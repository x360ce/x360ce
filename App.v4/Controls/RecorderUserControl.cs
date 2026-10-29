using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using x360ce.App.Recording;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	/// <summary>The Issues page's recorder: choose what to record, start, make the problem happen, stop, and post the file.</summary>
	/// <remarks>The recording itself is <see cref="ReportRecorder"/>; this control starts and stops it and tells it, a few times a second, that the window answers.</remarks>
	public partial class RecorderUserControl : UserControl
	{
		public RecorderUserControl()
		{
			InitializeComponent();
		}

		/// <summary>Fits the panel's height to where its content ends, whatever the font and the display scale.</summary>
		/// <remarks>A group box that sizes itself counts its caption twice, once in its content's place and once in its own border, and leaves a line's worth of space below.</remarks>
		void LayoutPanel_SizeChanged(object sender, EventArgs e)
		{
			var below = RecordGroupBox.Height - RecordGroupBox.DisplayRectangle.Bottom;
			Height = LayoutPanel.Bottom + below;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			LayoutPanel_SizeChanged(this, e);
		}

		/// <summary>Binds the check boxes to the options, once they are read.</summary>
		public void InitOptions()
		{
			SettingsManager.LoadAndMonitor(x => x.RecordSettings, SettingsCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RecordInput, InputCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RecordOutput, OutputCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RecordForces, ForcesCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RecordEngine, EngineCheckBox);
			SettingsManager.LoadAndMonitor(x => x.RecordWindow, WindowCheckBox);
			UpdateControls();
		}

		ReportRecorder _recorder;
		string _error;

		/// <summary>True while a recording runs.</summary>
		public bool IsRecording { get { return _recorder != null && !_recorder.IsStopped; } }

		/// <summary>What the check boxes choose.</summary>
		public static RecordParts PartsOf(Options o)
		{
			var parts = RecordParts.None;
			if (o.RecordSettings) parts |= RecordParts.Settings;
			if (o.RecordInput) parts |= RecordParts.Input;
			if (o.RecordOutput) parts |= RecordParts.Output;
			if (o.RecordForces) parts |= RecordParts.Forces;
			if (o.RecordEngine) parts |= RecordParts.Engine;
			if (o.RecordWindow) parts |= RecordParts.Window;
			return parts;
		}

		/// <summary>Starts a recording into a new file in <see cref="ReportRecorder.Folder"/>.</summary>
		public void StartRecording()
		{
			if (IsRecording)
				return;
			_error = null;
			var path = Path.Combine(ReportRecorder.Folder, "x360ce-recording-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt");
			int scale;
			using (var g = CreateGraphics())
				scale = (int)Math.Round(g.DpiX * 100 / 96);
			var recorder = new ReportRecorder(PartsOf(SettingsManager.Options), path, scale);
			try
			{
				recorder.Start(Global.DHelper);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				recorder.Dispose();
				_error = ShareSafe.Clean(ex.Message);
				UpdateControls();
				return;
			}
			_recorder = recorder;
			BeatTimer.Start();
			UpdateControls();
		}

		/// <summary>Ends the recording that runs, if any, and keeps its file.</summary>
		public void StopRecording()
		{
			var recorder = _recorder;
			if (recorder == null || recorder.IsStopped)
				return;
			recorder.Stop();
			UpdateControls();
		}

		void RecordButton_Click(object sender, EventArgs e)
		{
			if (IsRecording)
				StopRecording();
			else
				StartRecording();
		}

		void OpenFolderButton_Click(object sender, EventArgs e)
		{
			var file = _recorder == null ? null : _recorder.FilePath;
			if (file != null && File.Exists(file))
			{
				EngineHelper.BrowsePath(file);
				return;
			}
			Directory.CreateDirectory(ReportRecorder.Folder);
			EngineHelper.BrowsePath(ReportRecorder.Folder);
		}

		/// <summary>Tells the recording the window answers and how it is shown, and keeps the status line up to date.</summary>
		void BeatTimer_Tick(object sender, EventArgs e)
		{
			var recorder = _recorder;
			if (recorder == null)
			{
				BeatTimer.Stop();
				return;
			}
			recorder.Beat(WindowState());
			if (recorder.IsStopped)
				BeatTimer.Stop();
			UpdateControls();
		}

		/// <summary>How the main window is shown, in the words a recording writes.</summary>
		static string WindowState()
		{
			var form = MainForm.Current;
			if (form == null)
				return "closed";
			if (!form.Visible)
				return "hidden in the tray";
			if (form.WindowState == FormWindowState.Minimized)
				return "minimized";
			return Form.ActiveForm == form ? "in front" : "behind another window";
		}

		void UpdateControls()
		{
			var recording = IsRecording;
			foreach (var box in new[] { SettingsCheckBox, InputCheckBox, OutputCheckBox, ForcesCheckBox, EngineCheckBox, WindowCheckBox })
				box.Enabled = !recording;
			RecordButton.Text = recording ? "Stop Recording" : "Start Recording";
			StatusLabel.Text = StatusText();
		}

		string StatusText()
		{
			if (_error != null)
				return "The recording could not start: " + _error;
			var recorder = _recorder;
			if (recorder == null)
				return "Press Start Recording, make the problem happen, then press Stop Recording. It stops by itself after "
					+ (int)ReportRecorder.MaxDuration.TotalMinutes + " minutes.";
			var size = SizeText(recorder.Bytes);
			if (!recorder.IsStopped)
				return "Recording " + recorder.Elapsed.ToString(@"m\:ss", CultureInfo.InvariantCulture) + ", " + size + ". Make the problem happen now, then press Stop Recording.";
			return "Saved " + Path.GetFileName(recorder.FilePath) + ", " + size
				+ (recorder.StopReason == null ? "" : ", stopped because " + recorder.StopReason)
				+ ". Read it, then attach it to your issue on GitHub.";
		}

		static string SizeText(long bytes)
		{
			return bytes < 1024 * 1024
				? (bytes / 1024).ToString("N0", CultureInfo.InvariantCulture) + " KB"
				: (bytes / 1024.0 / 1024).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
		}
	}
}
