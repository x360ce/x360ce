using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace x360ce.Engine
{
	/// <summary>One message between two copies of this program.</summary>
	/// <remarks>
	/// <see cref="Type"/> says what the message is, and what a type carries goes in <see cref="Values"/>
	/// as text. A new type needs no new class, and a copy that has never heard of it can still read it
	/// and move on.
	/// </remarks>
	public class InstanceMessage
	{
		/// <summary>What the message is, such as Ready, Progress or Result.</summary>
		public string Type { get; set; }

		/// <summary>Words for a person, when there are any.</summary>
		public string Text { get; set; }

		/// <summary>What the type carries, by name.</summary>
		public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

		/// <summary>The value of that name, or null when the message does not carry it.</summary>
		public string Value(string name)
		{
			return Values != null && Values.TryGetValue(name, out var value) ? value : null;
		}
	}

	/// <summary>A connection between two copies of this program, carrying one JSON message to a line.</summary>
	/// <remarks>
	/// One copy listens on a named pipe and the other connects; after that either may send. The copy
	/// that is not Administrator listens, because Windows lets a copy running as Administrator reach
	/// one that is not, and not the other way round.
	///
	/// The two copies may be different versions, so messages are read tolerantly: fields a message does
	/// not declare are passed over, a value of a shape this copy does not know arrives as its JSON text,
	/// and a line that is no message at all is skipped.
	/// </remarks>
	public sealed class InstanceChannel : IDisposable
	{
		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		/// <summary>No byte order mark: it would arrive at the start of the first line and make it no message.</summary>
		static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

		/// <summary>How long one message may take to go.</summary>
		/// <remarks>
		/// A copy that has stopped reading fills the pipe, and a write to a full pipe waits for as long as
		/// it stays full. Given up on, the channel is closed and both copies see the other has gone.
		/// </remarks>
		static readonly TimeSpan SendLimit = TimeSpan.FromSeconds(10);

		readonly PipeStream _pipe;
		readonly object _writeLock = new object();
		readonly BlockingCollection<InstanceMessage> _received = new BlockingCollection<InstanceMessage>();
		Task _connecting;
		volatile bool _reading;
		volatile bool _closed;

		InstanceChannel(PipeStream pipe)
		{
			_pipe = pipe;
		}

		/// <summary>A pipe name no other running copy uses: this process and what the pipe is for.</summary>
		public static string NameFor(string purpose)
		{
			return string.Format(CultureInfo.InvariantCulture, "x360ce.{0}.{1}",
				System.Diagnostics.Process.GetCurrentProcess().Id, purpose);
		}

		/// <summary>Opens a pipe for one other copy to connect to.</summary>
		public static InstanceChannel Listen(string name)
		{
			return new InstanceChannel(new NamedPipeServerStream(name, PipeDirection.InOut, 1,
				PipeTransmissionMode.Byte, PipeOptions.Asynchronous));
		}

		/// <summary>Waits for the other copy to connect, and starts reading what it sends.</summary>
		/// <returns>False when nobody connected in the time given.</returns>
		public bool WaitForConnection(TimeSpan timeout)
		{
			var server = (NamedPipeServerStream)_pipe;
			if (!server.IsConnected)
			{
				try
				{
					// Kept, so waiting again goes on with the same wait. A pipe allows only one at a time.
					if (_connecting == null)
						_connecting = server.WaitForConnectionAsync();
					if (!_connecting.Wait(timeout))
						return false;
				}
				catch (AggregateException ex) when (ex.InnerException is IOException || ex.InnerException is ObjectDisposedException)
				{
					return false;
				}
			}
			StartReading();
			return true;
		}

		/// <summary>Connects to a copy listening under that name.</summary>
		/// <returns>Null when nobody is listening there in the time given.</returns>
		public static InstanceChannel Connect(string name, TimeSpan timeout)
		{
			var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
			try
			{
				client.Connect((int)timeout.TotalMilliseconds);
			}
			catch (TimeoutException)
			{
				client.Dispose();
				return null;
			}
			catch (IOException)
			{
				client.Dispose();
				return null;
			}
			var channel = new InstanceChannel(client);
			channel.StartReading();
			return channel;
		}

		/// <summary>Whether the other copy is still there.</summary>
		public bool IsConnected => !_closed && _pipe.IsConnected;

		/// <summary>Sends a message.</summary>
		/// <returns>False when the other copy has gone.</returns>
		public bool Send(InstanceMessage message)
		{
			if (_closed || message == null || !_reading)
				return false;
			var line = Json.Serialize(new Dictionary<string, object>
			{
				{ "Type", message.Type },
				{ "Text", message.Text },
				{ "Values", message.Values },
			});
			var bytes = Utf8.GetBytes(line + "\n");
			if (!Monitor.TryEnter(_writeLock, SendLimit))
				return false;
			try
			{
				if (_pipe.WriteAsync(bytes, 0, bytes.Length).Wait(SendLimit))
					return true;
				Close();
				return false;
			}
			catch (AggregateException ex) when (ex.InnerException is IOException || ex.InnerException is ObjectDisposedException)
			{
				Close();
				return false;
			}
			catch (IOException)
			{
				Close();
				return false;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
			finally
			{
				Monitor.Exit(_writeLock);
			}
		}

		/// <summary>The next message, or null when none came in the time given or the other copy has gone.</summary>
		public InstanceMessage Receive(TimeSpan timeout)
		{
			try
			{
				return _received.TryTake(out var message, timeout) ? message : null;
			}
			catch (ObjectDisposedException)
			{
				return null;
			}
		}

		/// <summary>Starts writing and reading, once the other copy is connected.</summary>
		void StartReading()
		{
			if (_reading)
				return;
			_reading = true;
			new Thread(ReadLines) { IsBackground = true, Name = "InstanceChannel" }.Start();
		}

		void ReadLines()
		{
			try
			{
				using (var reader = new StreamReader(_pipe, Utf8, false, 4096, true))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						var message = Parse(line);
						if (message != null)
							_received.Add(message);
					}
				}
			}
			catch (IOException) { }
			catch (ObjectDisposedException) { }
			// Added after the channel was closed from the other side of this copy.
			catch (InvalidOperationException) { }
			finally
			{
				Close();
			}
		}

		/// <summary>Reads one line as a message, or null when it is none.</summary>
		static InstanceMessage Parse(string line)
		{
			Dictionary<string, object> map;
			try
			{
				map = Json.DeserializeObject(line.TrimStart('﻿')) as Dictionary<string, object>;
			}
			catch (ArgumentException) { return null; }
			catch (InvalidOperationException) { return null; }
			if (map == null || !map.TryGetValue("Type", out var type) || !(type is string) || ((string)type).Length == 0)
				return null;
			var message = new InstanceMessage
			{
				Type = (string)type,
				Text = map.TryGetValue("Text", out var text) ? text as string : null,
			};
			if (map.TryGetValue("Values", out var values) && values is Dictionary<string, object> named)
				foreach (var pair in named)
					message.Values[pair.Key] = AsText(pair.Value);
			return message;
		}

		/// <summary>A value as text: numbers and yes-or-no as written, anything larger as its JSON.</summary>
		static string AsText(object value)
		{
			if (value == null)
				return null;
			if (value is string text)
				return text;
			if (value is bool yes)
				return yes ? "true" : "false";
			if (value is IFormattable number)
				return number.ToString(null, CultureInfo.InvariantCulture);
			return Json.Serialize(value);
		}

		void Close()
		{
			if (_closed)
				return;
			_closed = true;
			try { _received.CompleteAdding(); }
			catch (ObjectDisposedException) { }
		}

		public void Dispose()
		{
			Close();
			try { _pipe.Dispose(); }
			catch (IOException) { }
		}
	}
}
