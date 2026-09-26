using JocysCom.ClassLibrary.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	/// <summary>Switches devices on and off through one copy of this program running as Administrator.</summary>
	/// <remarks>
	/// Windows will not let an ordinary program switch a device off. Putting controllers in order
	/// switches a real one off and later on again, and starting a copy for each switch asked for
	/// Administrator each time. One copy is started at the first switch instead and makes every switch
	/// after it, as this copy asks, until it is told to go or this copy goes. It answers over an
	/// <see cref="InstanceChannel"/>, so nothing here waits for a process to end.
	///
	/// Rather than asking somebody to start the whole program again as Administrator - which loses
	/// whatever they were doing, and leaves it running with more power than it needs for anything
	/// else - only the copy has it, and only while it is needed. Running as Administrator already, the
	/// switches are made here and no copy is started.
	/// </remarks>
	public sealed class ElevatedDevices : IDisposable
	{
		/// <summary>Asks for devices to be switched. Values: Id, Devices (comma separated), On ("true" or "false").</summary>
		public const string SetDevices = "SetDevices";

		/// <summary>The answer to <see cref="SetDevices"/>. Values: its Id, and how many were switched (Done).</summary>
		public const string Result = "Result";

		/// <summary>Words for a person while a request is being carried out.</summary>
		public const string Progress = "Progress";

		/// <summary>Tells the Administrator copy it is no longer needed.</summary>
		public const string Exit = "Exit";

		/// <summary>How long the copy has to connect once Windows has started it.</summary>
		static readonly TimeSpan ConnectLimit = TimeSpan.FromSeconds(60);

		/// <summary>How long one request may take.</summary>
		static readonly TimeSpan AnswerLimit = TimeSpan.FromSeconds(60);

		/// <summary>How long the Administrator copy waits for a request before it decides it was forgotten.</summary>
		/// <remarks>Longer than a reorder spends between switching a controller off and on again.</remarks>
		static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(5);

		static int _pipes;

		readonly Func<string, bool> _start;
		readonly bool _here;
		InstanceChannel _channel;
		Process _copy;
		string _failure;
		int _requests;

		/// <summary>Told what the Administrator copy says while it works.</summary>
		public Action<string> Said;

		/// <summary>Switches here when this program is Administrator, and through a copy started as Administrator when not.</summary>
		public ElevatedDevices()
		{
			_here = JocysCom.ClassLibrary.Security.PermissionHelper.IsElevated;
			_start = StartCopy;
		}

		/// <summary>Switches through whatever <paramref name="start"/> starts.</summary>
		/// <param name="start">Given the pipe name to connect to; false when nothing could be started.</param>
		public ElevatedDevices(Func<string, bool> start)
		{
			_start = start;
		}

		/// <summary>Switches the devices on or off, in the order given.</summary>
		/// <returns>True when every one was switched.</returns>
		public bool Switch(bool on, string[] deviceIds, out string error)
		{
			if (_here)
				return Answered(SwitchHere(on, deviceIds, DeviceDetector.SetDeviceState), deviceIds.Length, out error);
			if (!Open(out error))
				return false;
			var id = (++_requests).ToString(CultureInfo.InvariantCulture);
			var request = new InstanceMessage { Type = SetDevices };
			request.Values["Id"] = id;
			request.Values["Devices"] = string.Join(",", deviceIds);
			request.Values["On"] = on ? "true" : "false";
			var until = DateTime.UtcNow + AnswerLimit;
			if (_channel.Send(request))
			{
				for (var left = AnswerLimit; left > TimeSpan.Zero; left = until - DateTime.UtcNow)
				{
					var message = _channel.Receive(left);
					if (message == null)
						break;
					var said = Said;
					if (message.Type == Progress && said != null && !string.IsNullOrEmpty(message.Text))
						said(message.Text);
					else if (message.Type == Result && message.Value("Id") == id)
						return Answered(message, deviceIds.Length, out error);
					// Anything else, including what a newer copy sends that this one has never heard of, is passed over.
				}
			}
			error = _channel.IsConnected
				? "The copy of this program running as Administrator did not answer in time."
				: "The copy of this program running as Administrator stopped before it answered.";
			return false;
		}

		/// <summary>Starts the Administrator copy at the first switch, and waits for it to connect.</summary>
		bool Open(out string error)
		{
			error = _failure;
			if (_failure != null)
				return false;
			if (_channel != null)
			{
				if (_channel.IsConnected)
					return true;
				error = _failure = "The copy of this program running as Administrator has stopped.";
				return false;
			}
			var name = InstanceChannel.NameFor("devices" + Interlocked.Increment(ref _pipes));
			_channel = InstanceChannel.Listen(name);
			if (!_start(name))
			{
				error = _failure = "Switching a controller off needs Administrator, and Windows did not start "
					+ "a copy of this program as Administrator.";
				return false;
			}
			var until = DateTime.UtcNow + ConnectLimit;
			while (!_channel.WaitForConnection(TimeSpan.FromMilliseconds(250)))
			{
				if (DateTime.UtcNow > until || _copy != null && _copy.HasExited)
				{
					error = _failure = "The copy of this program started as Administrator did not connect.";
					return false;
				}
			}
			return true;
		}

		/// <summary>Starts this program again as Administrator, to serve the pipe of that name.</summary>
		/// <remarks>Windows asks at this point, and a refusal arrives as an exception from Start.</remarks>
		bool StartCopy(string pipeName)
		{
			var copy = JocysCom.ClassLibrary.Win32.UacHelper.CreateElevatedProcess(Application.ExecutablePath,
				string.Format("{0}=\"{1}\"", AdminCommand.DeviceHelper, pipeName));
			copy.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			try
			{
				copy.Start();
			}
			catch (Win32Exception)
			{
				copy.Dispose();
				return false;
			}
			_copy = copy;
			return true;
		}

		/// <summary>How the answer reads: true when every device asked for was switched.</summary>
		static bool Answered(InstanceMessage result, int asked, out string error)
		{
			int done;
			var ok = int.TryParse(result.Value("Done"), NumberStyles.Integer, CultureInfo.InvariantCulture, out done)
				&& done == asked;
			error = ok ? null : result.Text ?? string.Format("Switched {0} of {1}.", done, asked);
			return ok;
		}

		/// <summary>Switches the devices in this process, and says how it went as an answer.</summary>
		static InstanceMessage SwitchHere(bool on, string[] deviceIds, Func<string, bool, bool> setState)
		{
			var done = 0;
			string why = null;
			foreach (var deviceId in deviceIds)
			{
				try
				{
					if (setState(deviceId, on))
						done++;
				}
				catch (Exception ex)
				{
					why = ex.GetBaseException().Message;
				}
			}
			var result = new InstanceMessage
			{
				Type = Result,
				Text = string.Format("Switched {0} {1} of {2}.{3}", on ? "on" : "off", done, deviceIds.Length,
					why == null ? "" : " " + why),
			};
			result.Values["Done"] = done.ToString(CultureInfo.InvariantCulture);
			return result;
		}

		/// <summary>The device identifiers in a request.</summary>
		/// <remarks>Separated by commas, which no device identifier contains.</remarks>
		static string[] SplitIds(string value)
		{
			return string.IsNullOrEmpty(value)
				? new string[0]
				: value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
		}

		#region Administrator copy

		/// <summary>Connects to the copy that started this one, and serves it.</summary>
		public static void Serve(string pipeName)
		{
			using (var channel = InstanceChannel.Connect(pipeName, ConnectLimit))
				Serve(channel, DeviceDetector.SetDeviceState, IdleLimit);
		}

		/// <summary>Carries out requests until told to stop, until the other copy goes, or until none come for a while.</summary>
		/// <remarks>
		/// A request of a type this copy does not know is passed over, so a newer copy can ask for more
		/// without an older one stopping.
		/// </remarks>
		public static void Serve(InstanceChannel channel, Func<string, bool, bool> setState, TimeSpan idle)
		{
			if (channel == null)
				return;
			InstanceMessage message;
			while ((message = channel.Receive(idle)) != null && message.Type != Exit)
			{
				if (message.Type != SetDevices)
					continue;
				var result = SwitchHere(message.Value("On") == "true", SplitIds(message.Value("Devices")), setState);
				result.Values["Id"] = message.Value("Id");
				if (!channel.Send(result))
					return;
			}
		}

		#endregion

		/// <summary>Tells the Administrator copy it is no longer needed.</summary>
		public void Dispose()
		{
			if (_channel != null)
			{
				_channel.Send(new InstanceMessage { Type = Exit });
				_channel.Dispose();
				_channel = null;
			}
			if (_copy != null)
			{
				_copy.Dispose();
				_copy = null;
			}
		}
	}
}
