using JocysCom.ClassLibrary.Controls.UiTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Forms
{
	/// <summary>Tests one input device as a quality tester does: five steps that move on by themselves, then results graded in plain words.</summary>
	/// <remarks>
	/// The person presses only Start, which reads Stop while the test runs. While it runs the engine reads only this device,
	/// as fast as it can, and pauses everything else (<see cref="DInputHelper.Testing"/>); everything resumes as soon as the
	/// test ends, is stopped, or the window closes. Ten times a second the window hands what the engine recorded to
	/// <see cref="DeviceTestSteps"/>, which moves the steps on and grades the results (<see cref="DeviceTestGrades"/>), and
	/// shows where the test is.
	/// </remarks>
	public partial class TestDeviceForm : BaseFormWithHeader
	{
		/// <summary>The window as the designer and the layout tests make it, testing nothing.</summary>
		public TestDeviceForm() : this(null)
		{
		}

		/// <param name="device">The device to test, connected; null tests nothing.</param>
		public TestDeviceForm(UserDevice device)
		{
			InitializeComponent();
			if (IsDesignMode)
				return;
			Device = device;
			_buttonsView.Visible = false;
			// Made after the window was scaled, so their gap is given at the screen's scale.
			_stepsView.Margin = AppHelper.ScaledPadding(8, 8, 0, 0);
			_buttonsView.Margin = AppHelper.ScaledPadding(8, 8, 0, 0);
			ContentTableLayoutPanel.Controls.Add(_stepsView, 0, 0);
			ContentTableLayoutPanel.Controls.Add(_buttonsView, 0, 5);
			AddColumns();
			UiLanguage.Register(this);
			ShowWords();
			EventHandler languageChanged = (sender, args) => ShowWords();
			UiLanguage.Changed += languageChanged;
			Disposed += (sender, args) => UiLanguage.Changed -= languageChanged;
			ContentPanel.Resize += (s, e) => FitContent();
			// Measured again whenever the theme gives the grid or its headings another font or room.
			ResultsDataGridView.FontChanged += (s, e) => FitColumns();
			ResultsDataGridView.CellStyleContentChanged += (s, e) => FitColumns();
			ResultsDataGridView.ColumnHeadersDefaultCellStyleChanged += (s, e) => FitColumns();
			ResultsDataGridView.HandleCreated += (s, e) => FitColumns();
			ResultsDataGridView.ColumnHeadersHeightChanged += (s, e) => FitColumns();
		}

		void ShowWords()
		{
			if (Device != null)
				Text = string.Format(UiLanguage.T("TestDeviceForm.Title", "Test Device - {0}"),
					string.IsNullOrEmpty(Device.ProductName) ? Device.InstanceName : Device.ProductName);
			_shownResults = null;
			ShowSteps();
			_stepsView.Invalidate();
			foreach (var column in ColumnTexts)
				ResultsDataGridView.Columns[column.Name].HeaderText = column.Header();
			FitContent();
		}

		/// <summary>The device tested, or null.</summary>
		public UserDevice Device { get; }

		/// <summary>The test the engine runs, or null before the first Start.</summary>
		public DeviceTest Test { get; private set; }

		/// <summary>The steps of the test, and its results once done; null before the first Start.</summary>
		public DeviceTestSteps Steps { get; internal set; }

		readonly DeviceTestStepsView _stepsView = new DeviceTestStepsView();
		readonly DeviceTestButtonsView _buttonsView = new DeviceTestButtonsView();
		int _controlsVersion = -1;
		DeviceTestResults _shownResults;

		/// <summary>The test whose buttons the buttons view shows.</summary>
		DeviceTestSteps _buttonsShown;
		string _headerSubject, _headerBody;

		/// <summary>Whether a test is under way, between Start and its end.</summary>
		bool Running
		{
			get { return Steps != null && Steps.Step != DeviceTestStep.Idle && Steps.Step != DeviceTestStep.Done; }
		}

		/// <summary>The device as the test takes it: its input method, values, buttons and kind.</summary>
		internal static DeviceTestLayout LayoutOf(UserDevice ud)
		{
			var axes = ud.DiAxeMask;
			var sliders = ud.DiSliderMask;
			// Not read yet: the six axes and two sliders DirectInput names.
			if (axes == 0 && sliders == 0)
			{
				axes = 0x3F;
				sliders = 0x3;
			}
			var channels = new List<int>();
			for (var i = 0; i < SourceState.MaxAxis; i++)
				if ((axes & (1 << i)) != 0)
					channels.Add(i);
			for (var i = 0; i < SourceState.MaxSliders; i++)
				if ((sliders & (1 << i)) != 0)
					channels.Add(SourceState.MaxAxis + i);
			return new DeviceTestLayout
			{
				Method = ud.IsDirectInput ? InputSourceType.DirectInput : (InputSourceType)ud.InputSourceType,
				Channels = channels.ToArray(),
				ButtonCount = Math.Max(0, Math.Min(128, ud.CapButtonCount)),
				XInputCapable = XInputPlaces.IsXInputCapable(ud),
				Wheel = ud.CapType == (int)SharpDX.DirectInput.DeviceType.Driving,
				Names = NamesOf(ud),
			};
		}

		/// <summary>The device's own name for each of its values, as DirectInput gives its objects ("Throttle", "Rudder"); a name two share is numbered.</summary>
		static string[] NamesOf(UserDevice ud)
		{
			var names = new string[DeviceTestSteps.Values];
			var axes = new[] { SharpDX.DirectInput.ObjectGuid.XAxis, SharpDX.DirectInput.ObjectGuid.YAxis, SharpDX.DirectInput.ObjectGuid.ZAxis,
				SharpDX.DirectInput.ObjectGuid.RxAxis, SharpDX.DirectInput.ObjectGuid.RyAxis, SharpDX.DirectInput.ObjectGuid.RzAxis };
			var sliders = 0;
			foreach (var item in ud.DeviceObjects ?? new DeviceObjectItem[0])
			{
				var channel = item.Type == SharpDX.DirectInput.ObjectGuid.Slider ? SourceState.MaxAxis + sliders++ : Array.IndexOf(axes, item.Type);
				if (channel >= 0 && channel < names.Length && !string.IsNullOrWhiteSpace(item.Name))
					names[channel] = item.Name.Trim();
			}
			foreach (var shared in names.Where(x => x != null).GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList())
			{
				var number = 0;
				for (var i = 0; i < names.Length; i++)
					if (names[i] == shared)
						names[i] = shared + " " + ++number;
			}
			return names;
		}

		#region Engine

		private void StartButton_Click(object sender, EventArgs e)
		{
			if (Running)
			{
				Steps.Stop(Stopwatch.GetTimestamp());
				EndTest();
				return;
			}
			if (Device == null || Global.DHelper == null)
				return;
			Test = new DeviceTest(Device);
			Steps = new DeviceTestSteps(LayoutOf(Device), Stopwatch.Frequency);
			_controlsVersion = -1;
			_shownResults = null;
			Global.DHelper.Testing = Test;
			Steps.Start(Stopwatch.GetTimestamp());
			UpdateTimer.Start();
			ShowSteps();
		}

		private void UpdateTimer_Tick(object sender, EventArgs e)
		{
			if (Steps == null || Test == null)
				return;
			var now = Stopwatch.GetTimestamp();
			Test.TakeInto(Steps);
			Steps.SetReads(now, Test.Reads);
			if (Test.Offline)
				Steps.Stop(now);
			else
				Steps.Tick(now);
			if (Steps.Step == DeviceTestStep.Done)
				EndTest();
			else
				ShowSteps();
		}

		void EndTest()
		{
			UpdateTimer.Stop();
			Resume();
			ShowSteps();
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			Resume();
			base.OnFormClosed(e);
		}

		/// <summary>Takes the test off the engine, which resumes its normal work at once.</summary>
		void Resume()
		{
			UpdateTimer.Stop();
			var helper = Global.DHelper;
			if (Test != null && helper != null && helper.Testing == Test)
				helper.Testing = null;
		}

		#endregion

		#region Steps

		/// <summary>Shows where the test is: the step, what to do, the countdown, and the controls, buttons or results of the step.</summary>
		internal void ShowSteps()
		{
			var s = Steps;
			var step = s == null ? DeviceTestStep.Idle : s.Step;
			var unplugged = Test != null && Test.Offline;
			var results = step == DeviceTestStep.Done ? s.Results : null;
			// The header names the step and gives one fact; the large text is the one thing to do.
			if (step == DeviceTestStep.Idle)
				SetHeader(UiLanguage.T("TestDeviceForm.Ready", "Ready"), UiLanguage.T("TestDeviceForm.FiveSteps", "Five steps, about a minute: each moves on by itself."));
			else if (step == DeviceTestStep.Done)
				SetHeader(unplugged ? UiLanguage.T("TestDeviceForm.Unplugged", "Unplugged") : s.StepName, UiLanguage.T("TestDeviceForm.ControllersResumed", "The controllers resumed."));
			else
				SetHeader(string.Format(UiLanguage.T("TestDeviceForm.StepHeading", "Step {0} of {1}: {2}"), s.StepNumber, DeviceTestSteps.StepCount, s.StepName),
					UiLanguage.T("TestDeviceForm.ControllersPaused", "Controllers are paused until the test ends."));
			SetLine(InstructionLabel, unplugged ? UiLanguage.T("TestDeviceForm.ReconnectDevice", "Plug the device in again, then press Start.") : s == null ? UiLanguage.T("TestDeviceForm.PressStart", "Press Start to test the controller.") : s.Instruction);
			SetLine(HintLabel, unplugged ? "" : s == null ? "" : s.Hint);
			SetLine(CountdownLabel, s == null ? "" : s.Countdown);
			SetLine(CountdownCaptionLabel, s == null ? "" : s.CountdownCaption);
			SetText(StartButton, Running
				? UiLanguage.T("TestDeviceForm.StopButton", "S&top")
				: step == DeviceTestStep.Done ? UiLanguage.T("TestDeviceForm.TestAgainButton", "&Test again")
					: UiLanguage.T("TestDeviceForm.StartButton", "&Start"));
			// Named as it reads, for a screen reader and the door, which would otherwise take the catalog's name.
			StartButton.AccessibleName = Running ? "Stop" : step == DeviceTestStep.Done ? "Test again" : "Start";
			SetVisible(SkipButton, Running && s.CanSkip);
			SetVisible(_stepsView, step <= DeviceTestStep.PickUp);
			if (step <= DeviceTestStep.PickUp)
				_stepsView.ShowStep(s == null ? 0 : s.StepNumber);
			SetVisible(ControlsFlowLayoutPanel, step == DeviceTestStep.Sticks);
			SetVisible(_buttonsView, step == DeviceTestStep.Buttons || (results != null && s.Buttons.Length > 0));
			SetVisible(ButtonsHeadingLabel, results != null && s.Buttons.Length > 0);
			SetVisible(CardsFlowLayoutPanel, results != null);
			SetVisible(DetailsTableLayoutPanel, results != null);
			if (step == DeviceTestStep.Sticks)
				ShowControls();
			if (step == DeviceTestStep.Buttons)
			{
				if (_buttonsShown != s)
				{
					_buttonsShown = s;
					_buttonsView.SetButtons(s.Buttons);
					FitContent();
				}
				_buttonsView.Redraw();
			}
			if (results != null && results != _shownResults)
				ShowResults();
		}

		void SetHeader(string subject, string body)
		{
			if (_headerSubject != subject)
			{
				_headerSubject = subject;
				SetHeaderSubject(subject);
			}
			if (_headerBody != body)
			{
				_headerBody = body;
				SetHeaderBody(MessageBoxIcon.None, body);
			}
		}

		static void SetText(Control control, string text)
		{
			if (control.Text != text)
				control.Text = text;
		}

		/// <summary>A line of text, hidden while empty so it takes no room.</summary>
		static void SetLine(Label label, string text)
		{
			SetText(label, text);
			SetVisible(label, !string.IsNullOrEmpty(text));
		}

		/// <summary>Shows or hides a control. Set every time: before the window shows, every control reads as hidden, whatever it was told.</summary>
		static void SetVisible(Control control, bool visible)
		{
			control.Visible = visible;
		}

		/// <summary>A view for each stick and axis, made again when the reads pair the axes otherwise, and each drawn as it is now.</summary>
		void ShowControls()
		{
			var panel = ControlsFlowLayoutPanel;
			if (_controlsVersion != Steps.ControlsVersion)
			{
				_controlsVersion = Steps.ControlsVersion;
				panel.SuspendLayout();
				foreach (var old in panel.Controls.Cast<Control>().ToList())
					old.Dispose();
				foreach (var control in Steps.Controls)
				{
					var view = new DeviceTestControlView(control);
					panel.Controls.Add(view);
					view.Size = view.GetPreferredSize(Size.Empty);
				}
				panel.ResumeLayout();
			}
			foreach (var view in panel.Controls.OfType<DeviceTestControlView>())
				view.Redraw();
		}

		#endregion

		#region Results

		void ShowResults()
		{
			var results = Steps.Results;
			_shownResults = results;
			var panel = CardsFlowLayoutPanel;
			panel.SuspendLayout();
			foreach (var old in panel.Controls.Cast<Control>().ToList())
				old.Dispose();
			var overall = new DeviceTestCard("OverallCard") { Overall = true };
			panel.Controls.Add(overall);
			overall.Set(UiLanguage.T("TestDeviceForm.OverallGrade", "Overall grade"), results.Overall.HasValue ? results.Overall.Value.ToString() : UiLanguage.T("TestDeviceForm.NotGraded", "Not graded"), results.Overall, OverallText(results));
			foreach (var item in results.Items)
			{
				var card = new DeviceTestCard(new string(item.Criterion.Name.Where(char.IsLetterOrDigit).ToArray()) + "Card");
				panel.Controls.Add(card);
				card.Set(item.Criterion.Name, item.ValueText, item.Grade, Explanation(item), item.Detail);
			}
			panel.ResumeLayout();
			_buttonsView.SetButtons(Steps.Buttons);
			ButtonsHeadingLabel.Text = ButtonsText(Steps.Buttons);
			NotGradedLabel.Text = NotGradedText(Steps);
			SetVisible(NotGradedLabel, NotGradedLabel.Text.Length > 0);
			SummaryLabel.Text = SummaryText(Steps.Statistics.Summary(0), Test == null ? 0 : Test.Dropped + Test.DroppedEntries);
			UpdateRows();
			FitContent();
		}

		/// <summary>What the overall grade is made of, and the weakest result.</summary>
		static string OverallText(DeviceTestResults results)
		{
			if (!results.Overall.HasValue)
				return UiLanguage.T("TestDeviceForm.NothingGraded", "Nothing could be graded: no control was moved until its bar was full.");
			var count = results.Items.Count(x => x.Grade.HasValue);
			var text = string.Format(UiLanguage.T("TestDeviceForm.AverageOfResults", "The average of the {0} results graded, rounded toward the worse."), count);
			var weakest = results.Weakest;
			if (weakest == null)
				return text + " " + UiLanguage.T("TestDeviceForm.EveryResultBest", "Every one is Best.");
			return text + " " + (weakest.Control == null
				? string.Format(UiLanguage.T("TestDeviceForm.WeakestResult", "Weakest: {0}, {1}."), weakest.Criterion.Name, weakest.ValueText)
				: string.Format(UiLanguage.T("TestDeviceForm.WeakestControl", "Weakest: {0}, {1} on {2}."), weakest.Criterion.Name, weakest.ValueText, weakest.Control));
		}

		/// <summary>A card's line: what the result tells, the control its value came from, and what was left out or not measured.</summary>
		static string Explanation(DeviceTestResult item)
		{
			var text = item.Explanation ?? item.Criterion.Explanation;
			if (item.Control != null)
				text += " " + string.Format(UiLanguage.T("TestDeviceForm.WorstControl", "Worst: {0}."), item.Control);
			if (item.Limited != null && item.LimitedBy == null)
				text += " " + string.Format(UiLanguage.T("TestDeviceForm.LimitedResult", "{0}, not graded."), item.Limited);
			if (item.Value.HasValue || item.LimitedBy != null)
				return text;
			var c = item.Criterion;
			if (c == DeviceTestGrades.UpdatesPerSecond || c == DeviceTestGrades.SteadyTiming)
				return text + " " + UiLanguage.T("TestDeviceForm.TooFewUpdates", "Too few updates came while the controller moved.");
			if (c == DeviceTestGrades.CircleError)
				return text + " " + UiLanguage.T("TestDeviceForm.NoStickFinished", "No stick was finished.");
			if (c == DeviceTestGrades.DeadZone || c == DeviceTestGrades.JumpFromCentre)
				return text + " " + UiLanguage.T("TestDeviceForm.NoCentredControlFinished", "No stick, or axis resting in the middle, was finished.");
			return text + " " + UiLanguage.T("TestDeviceForm.NoControlFinished", "No control was finished.");
		}

		/// <summary>The controls left out of the grades, and why: never moved, skipped, or unstable, which is a finding of its own.</summary>
		static string NotGradedText(DeviceTestSteps steps)
		{
			var parts = new List<string>();
			foreach (var control in steps.Controls)
			{
				if (control.Unstable)
					parts.Add(string.Format(UiLanguage.T("TestDeviceForm.UnstableControl",
						"{0} unstable: it kept changing for {1} s without getting fuller"), control.Name, DeviceTestSteps.UnstableSeconds));
				else if (control.Skipped)
					parts.Add(string.Format(UiLanguage.T("TestDeviceForm.SkippedControl", "{0} skipped"), control.Name));
				else if (control.NotMoved)
					parts.Add(string.Format(UiLanguage.T("TestDeviceForm.UnmovedControl", "{0} not moved"), control.Name));
			}
			var flickering = steps.Buttons.Where(x => x.Unstable).Select(x => x.Number.ToString(CultureInfo.CurrentCulture)).ToArray();
			if (flickering.Length > 0)
				parts.Add(string.Format(flickering.Length == 1
					? UiLanguage.T("TestDeviceForm.UnstableButton", "button {0} unstable: pressed again and again by itself")
					: UiLanguage.T("TestDeviceForm.UnstableButtons", "buttons {0} unstable: pressed again and again by themselves"), string.Join(", ", flickering)));
			return parts.Count == 0 ? "" : string.Format(UiLanguage.T("TestDeviceForm.UngradedControls", "Not graded: {0}."), string.Join("; ", parts));
		}

		/// <summary>The buttons in words: how many were pressed, which never were, and which registered one push twice.</summary>
		static string ButtonsText(DeviceTestButton[] buttons)
		{
			var c = CultureInfo.CurrentCulture;
			var parts = new List<string>
			{
				string.Format(c, UiLanguage.T("TestDeviceForm.ButtonsPressed", "Buttons, shown and not graded: {0} of {1} pressed."), buttons.Count(x => x.Pressed), buttons.Length),
			};
			var never = buttons.Where(x => x.NotPressed).Select(x => x.Number.ToString(c)).ToArray();
			if (never.Length > 0)
				parts.Add(string.Format(UiLanguage.T("TestDeviceForm.ButtonsNotPressed", "Not pressed: {0}."), string.Join(", ", never)));
			var skipped = buttons.Where(x => x.Skipped).Select(x => x.Number.ToString(c)).ToArray();
			if (skipped.Length > 0)
				parts.Add(string.Format(UiLanguage.T("TestDeviceForm.ButtonsSkipped", "Skipped: {0}."), string.Join(", ", skipped)));
			var twice = buttons.Where(x => x.Twice).Select(x => string.Format(c, UiLanguage.T("TestDeviceForm.ButtonDoublePressInterval", "{0} ({1:0} ms apart)"), x.Number, x.TwiceAfter)).ToArray();
			if (twice.Length > 0)
				parts.Add(string.Format(twice.Length == 1
					? UiLanguage.T("TestDeviceForm.ButtonDoublePress", "One push registered twice on button {0}.")
					: UiLanguage.T("TestDeviceForm.ButtonsDoublePress", "One push registered twice on buttons {0}."), string.Join(", ", twice)));
			return string.Join(" ", parts);
		}

		/// <summary>The width the content is laid out in: the room of a scroll bar is kept whether it shows or not, so its coming and going changes nothing.</summary>
		int ContentWidth
		{
			get { return ContentPanel.ClientSize.Width - (ContentPanel.VerticalScroll.Visible ? 0 : SystemInformation.VerticalScrollBarWidth); }
		}

		/// <summary>
		/// Sizes the cards four to a row as room allows, the overall grade first, all as tall as the tallest; the step list
		/// and the buttons across the whole width; and the details table to the room.
		/// </summary>
		void FitContent()
		{
			var width = ContentWidth;
			if (width <= 0)
				return;
			FitColumns();
			var margin = _buttonsView.Margin.Horizontal;
			var columns = Math.Max(1, Math.Min(4, width / (Font.Height * 15)));
			var cardWidth = width / columns - margin;
			var cards = CardsFlowLayoutPanel.Controls.OfType<DeviceTestCard>().ToList();
			CardsFlowLayoutPanel.SuspendLayout();
			foreach (var card in cards)
				card.Width = cardWidth;
			var height = cards.Count == 0 ? 0 : cards.Max(x => x.GetPreferredSize(Size.Empty).Height);
			foreach (var card in cards)
				card.Height = height;
			CardsFlowLayoutPanel.ResumeLayout();
			var whole = width - margin;
			_stepsView.Size = new Size(whole, _stepsView.GetPreferredSize(new Size(whole, 0)).Height);
			_buttonsView.Size = new Size(whole, _buttonsView.GetPreferredSize(new Size(whole, 0)).Height);
		}

		/// <summary>The details line: the report rate and its intervals, the values a report carries, and the read rate.</summary>
		internal static string SummaryText(DeviceTestSummary s, long dropped)
		{
			var c = CultureInfo.CurrentCulture;
			var reports = new List<string>
			{
				s.ReportRate.HasValue
					? string.Format(c, UiLanguage.T("TestDeviceForm.ReportRate", "Report rate {0:N0} a second while moving."), s.ReportRate.Value)
					: UiLanguage.T("TestDeviceForm.ReportRateWhileMoving", "Report rate: measured while moving."),
			};
			if (s.ShortestInterval.HasValue)
				reports.Add(string.Format(c, UiLanguage.T("TestDeviceForm.ReportIntervals",
					"Between reports: {0:0.##} ms shortest, {1:0.##} ms median."), s.ShortestInterval.Value, s.MedianInterval.Value));
			if (s.OnTimePercent.HasValue && s.MedianInterval.HasValue)
				reports.Add(string.Format(c, UiLanguage.T("TestDeviceForm.ReportsNearMedianInterval",
					"{0:0.#} % of intervals were within a tenth of the median."), s.OnTimePercent.Value));
			if (s.ValuesPerReport.HasValue)
			{
				reports.Add(string.Format(c, UiLanguage.T("TestDeviceForm.ValuesPerReport", "{0:0.0} values a report."), s.ValuesPerReport.Value));
				if (s.TogetherPercent.HasValue)
					reports.Add(string.Format(c, UiLanguage.T("TestDeviceForm.ValuesChangedTogether",
						"{0:0} % of reports while two values moved changed both."), s.TogetherPercent.Value));
			}
			var reads = string.Format(c, UiLanguage.T("TestDeviceForm.ReadRate", "Read rate {0:N0} a second; {1:N0} reads; {2:N0} dropped."),
				s.ReadRate ?? 0, s.Reads, dropped);
			if (s.TooSlow)
				reads += " " + UiLanguage.T("TestDeviceForm.ReadRateTooSlow", "Reads come too slowly to see every report.");
			return string.Join(" ", reports) + Environment.NewLine + reads;
		}

		private void SkipButton_Click(object sender, EventArgs e)
		{
			if (!Running)
				return;
			Steps.Skip(Stopwatch.GetTimestamp());
			if (Steps.Step == DeviceTestStep.Done)
				EndTest();
			else
				ShowSteps();
		}

		#endregion

		#region Details

		struct ColumnText
		{
			public readonly string Name;
			public readonly Func<string> Header;

			public ColumnText(string name, Func<string> header)
			{
				Name = name;
				Header = header;
			}
		}

		/// <summary>The columns: what each row shows of its axis or slider.</summary>
		static readonly ColumnText[] ColumnTexts =
		{
			new ColumnText("ControlColumn", () => UiLanguage.T("TestDeviceForm.ControlColumn.HeaderText", "Control")),
			new ColumnText("MinColumn", () => UiLanguage.T("TestDeviceForm.MinColumn.HeaderText", "Min")),
			new ColumnText("MaxColumn", () => UiLanguage.T("TestDeviceForm.MaxColumn.HeaderText", "Max")),
			new ColumnText("RangeColumn", () => UiLanguage.T("TestDeviceForm.RangeColumn.HeaderText", "Range %")),
			new ColumnText("CentreColumn", () => UiLanguage.T("TestDeviceForm.CentreColumn.HeaderText", "Centre")),
			new ColumnText("OffsetColumn", () => UiLanguage.T("TestDeviceForm.OffsetColumn.HeaderText", "Offset %")),
			new ColumnText("SpreadColumn", () => UiLanguage.T("TestDeviceForm.SpreadColumn.HeaderText", "Noise")),
			new ColumnText("DeviationColumn", () => UiLanguage.T("TestDeviceForm.DeviationColumn.HeaderText", "Noise SD")),
			new ColumnText("DeadZoneColumn", () => UiLanguage.T("TestDeviceForm.DeadZoneColumn.HeaderText", "Dead Zone %")),
			new ColumnText("DistinctColumn", () => UiLanguage.T("TestDeviceForm.DistinctColumn.HeaderText", "Values")),
			new ColumnText("BitsColumn", () => UiLanguage.T("TestDeviceForm.BitsColumn.HeaderText", "Bits")),
			new ColumnText("SmallestColumn", () => UiLanguage.T("TestDeviceForm.SmallestColumn.HeaderText", "Smallest Step")),
			new ColumnText("FirstStepColumn", () => UiLanguage.T("TestDeviceForm.FirstStepColumn.HeaderText", "First Step %")),
			new ColumnText("LargestColumn", () => UiLanguage.T("TestDeviceForm.LargestColumn.HeaderText", "Largest Step")),
			new ColumnText("JumpsColumn", () => UiLanguage.T("TestDeviceForm.JumpsColumn.HeaderText", "Jumps")),
			new ColumnText("RateColumn", () => UiLanguage.T("TestDeviceForm.RateColumn.HeaderText", "Updates/s")),
		};

		void AddColumns()
		{
			foreach (var text in ColumnTexts)
			{
				var column = new DataGridViewTextBoxColumn { Name = text.Name, HeaderText = text.Header(), SortMode = DataGridViewColumnSortMode.NotSortable };
				// A number stands at the right, and its heading as its values do.
				if (text.Name != "ControlColumn")
					column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
				column.HeaderCell.Style.Alignment = column.DefaultCellStyle.Alignment;
				// A heading of two words or more takes two lines, so the table fits the window.
				column.HeaderCell.Style.WrapMode = DataGridViewTriState.True;
				UiLanguage.Keep(column);
				ResultsDataGridView.Columns.Add(column);
			}
			FitColumns();
		}

		bool _fittingColumns;

		/// <summary>
		/// Makes each column as wide as its widest value, or its heading on at most two lines, whichever is wider; a number's
		/// heading ends where its values end. The grid is as large as its columns and rows.
		/// </summary>
		void FitColumns()
		{
			// Setting a heading's room changes the style, which asks for this again.
			if (_fittingColumns)
				return;
			_fittingColumns = true;
			try
			{
				FitColumnWidths();
			}
			finally
			{
				_fittingColumns = false;
			}
		}

		void FitColumnWidths()
		{
			var grid = ResultsDataGridView;
			if (grid.Columns.Count == 0)
				return;
			var line = new Size(int.MaxValue, int.MaxValue);
			var font = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
			var header = grid.ColumnHeadersDefaultCellStyle.Padding;
			// Headings of two words or more are given two lines' height, as they are made narrow enough to need them.
			var lines = grid.Columns.Cast<DataGridViewColumn>().Any(x => x.HeaderText.Contains(" ")) ? 2 : 1;
			using (var bitmap = new Bitmap(1, 1))
			using (var g = Graphics.FromImage(bitmap))
			{
				Func<string, int> measure = text => TextRenderer.MeasureText(g, text, font, line, TextFormatFlags.NoPrefix).Width;
				// The room round a heading's text, from the first column's heading, a single word measured on one line.
				var first = grid.Columns[0];
				var around = first.GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true) - measure(first.HeaderText);
				foreach (DataGridViewColumn column in grid.Columns)
				{
					var style = column.InheritedStyle;
					if (column.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight)
					{
						var value = TextRenderer.MeasureText(g, "65535", style.Font, line, TextFormatFlags.Default).Width;
						var text = TextRenderer.MeasureText(g, "65535", style.Font, line, TextFormatFlags.NoPadding).Width;
						var padding = new Padding(header.Left, header.Top, (value - text) / 2, header.Bottom);
						if (column.HeaderCell.Style.Padding != padding)
							column.HeaderCell.Style.Padding = padding;
					}
					var own = column.HeaderCell.Style.Padding == Padding.Empty ? 0 : column.HeaderCell.Style.Padding.Horizontal - header.Horizontal;
					var heading = around + own + TwoLines(column.HeaderText, measure);
					var width = Math.Max(column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCellsExceptHeader, true), heading);
					if (column.Width != width)
						column.Width = width;

				}
			}
			// The theme holds the headings to one line's height; a heading on two lines needs two.
			var height = font.Height * lines + header.Vertical;
			if (grid.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize && grid.ColumnHeadersHeight < height)
				grid.ColumnHeadersHeight = height;
			// As wide as its columns, or as the room, scrolling sideways when that is less.
			var border = grid.BorderStyle == BorderStyle.None ? 0 : 2;
			var needed = grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) + border;
			var room = ContentWidth - grid.Margin.Horizontal;
			var scroll = room > 0 && needed > room;
			var size = new Size(scroll ? room : needed, grid.ColumnHeadersHeight + grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + border
				+ (scroll ? SystemInformation.HorizontalScrollBarHeight : 0));
			var bars = scroll ? ScrollBars.Horizontal : ScrollBars.None;
			if (grid.ScrollBars != bars)
				grid.ScrollBars = bars;
			if (grid.Size != size)
				grid.Size = size;
		}

		/// <summary>The width a heading needs on at most two lines: the narrowest of its splits between words.</summary>
		static int TwoLines(string text, Func<string, int> measure)
		{
			var words = text.Split(' ');
			var width = measure(text);
			for (var i = 1; i < words.Length; i++)
				width = Math.Min(width, Math.Max(measure(string.Join(" ", words, 0, i)), measure(string.Join(" ", words, i, words.Length - i))));
			return width;
		}

		/// <summary>One row for each axis and slider, with its raw figures.</summary>
		void UpdateRows()
		{
			var grid = ResultsDataGridView;
			var c = CultureInfo.CurrentCulture;
			grid.Rows.Clear();
			foreach (var channel in Steps.Layout.Channels)
			{
				var x = Steps.Statistics.Channels[channel];
				grid.Rows.Add(Steps.Layout.ChannelName(channel),
					Number(x.Min, c), Number(x.Max, c), Percent(x.RangePercent, c), Fixed(x.Centre, "N0", c),
					x.CentreOffsetPercent.HasValue ? Math.Round(x.CentreOffsetPercent.Value, 1).ToString("+0.0;-0.0;0.0", c) : "",
					Number(x.NoiseSpread, c), Fixed(x.NoiseDeviation, "N1", c), Percent(x.DeadZonePercent, c),
					x.Distinct == 0 ? "" : x.Distinct.ToString("N0", c), Fixed(x.EffectiveBits, "0.0", c), Number(x.SmallestStep, c),
					Percent(x.FirstStepPercent, c), Number(x.LargestStep, c), x.MedianStep.HasValue ? x.Jumps.ToString("N0", c) : "",
					Fixed(x.UpdateRate, "N0", c));
			}
			grid.ClearSelection();
			FitColumns();
		}

		static string Number(int? value, CultureInfo c)
		{
			return value.HasValue ? value.Value.ToString("N0", c) : "";
		}

		static string Fixed(double? value, string format, CultureInfo c)
		{
			return value.HasValue ? value.Value.ToString(format, c) : "";
		}

		static string Percent(double? value, CultureInfo c)
		{
			return value.HasValue ? value.Value.ToString("0.0", c) : "";
		}

		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Resume();
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
