using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace x360ce.TestGame
{
	/// <summary>
	/// Interactive Virtual Car tab inside x360ce Test Game: drive a 2D car with any XInput controller
	/// (including x360ce's Remote MCP Controller or Test Controller), keyboard WASD/Arrows, or Auto-Drive,
	/// while transmitting authentic standard UDP telemetry (Forza, EA F1, Codemasters, OutSim, Project CARS 2)
	/// directly to x360ce for live end-to-end wheel telemetry testing.
	/// Uses system colours (so x360ce's shared Light/Dark theme applies) and strict 4 px font-scaled spacing.
	/// </summary>
	public sealed class VirtualCarPanel : UserControl
	{
		public readonly VirtualCarSimulation Car = new VirtualCarSimulation();

		readonly List<Action<int>> _spacing = new List<Action<int>>();
		readonly TableLayoutPanel _root;
		readonly ComboBox _controllerCombo;
		readonly ComboBox _surfaceCombo;
		readonly NumericUpDown _gradeUpDown;
		readonly CheckBox _autoDriveCheck;
		readonly Button _curbLeftButton;
		readonly Button _curbRightButton;
		readonly Button _resetButton;
		readonly CheckBox _sendTelemetryCheck;
		readonly ComboBox _protocolCombo;
		readonly TextBox _ipTextBox;
		readonly NumericUpDown _portUpDown;
		readonly CheckBox _sendRumbleCheck;
		readonly Label _txStatusLabel;
		readonly TrackCanvas _canvas;
		readonly TextBox _hudTextBox;
		Font _monoFont;

		Socket _udp;
		IPEndPoint _target;
		string _targetIp = "127.0.0.1";
		int _targetPort = 37150;
		long _packetsSent;
		int _sentThisSecond;
		int _sendHz;
		long _rateWindowMs;
		string _lastTxError;
		readonly Stopwatch _clock = Stopwatch.StartNew();
		long _lastStepMs;

		// Keyboard driving state when the Virtual Car tab is active.
		bool _keyLeft, _keyRight, _keyUp, _keyDown, _keySpace;

		public VirtualCarPanel()
		{
			Name = nameof(VirtualCarPanel);
			AccessibleName = "Virtual Car";
			AccessibleDescription = "Drivable virtual car simulator that emits standard racing telemetry UDP packets to test x360ce's Wheel Telemetry plugin.";
			Dock = DockStyle.Fill;

			SuspendLayout();

			_root = new TableLayoutPanel
			{
				Name = "VirtualCarRootPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 3,
				Margin = Padding.Empty,
			};
			_root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			_root.RowStyles.Add(new RowStyle());
			_root.RowStyles.Add(new RowStyle());
			_root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			_spacing.Add(g => _root.Padding = new Padding(0, 0, g, g));

			// Row 0: Driving & Surface controls.
			var driveRow = new FlowLayoutPanel
			{
				Name = "DriveControlsPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
				WrapContents = true,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			_controllerCombo = new ComboBox
			{
				Name = "CarControllerComboBox",
				AccessibleName = "Controller",
				AccessibleDescription = "XInput controller driving the virtual car (Left Stick X steers, RT accelerates, LT brakes, B handbrakes, X hits a curb, A toggles Auto-Drive).",
				DropDownStyle = ComboBoxStyle.DropDownList,
			};
			_controllerCombo.Items.AddRange(new object[] { "XInput 1", "XInput 2", "XInput 3", "XInput 4" });
			_controllerCombo.SelectedIndex = 0;

			_surfaceCombo = new ComboBox
			{
				Name = "CarSurfaceComboBox",
				AccessibleName = "Track surface",
				AccessibleDescription = "Road surface grip and roughness (Dry Tarmac, Wet Tarmac, Gravel, Ice).",
				DropDownStyle = ComboBoxStyle.DropDownList,
			};
			_surfaceCombo.Items.AddRange(new object[] { "Dry Tarmac", "Wet Tarmac", "Gravel", "Ice" });
			_surfaceCombo.SelectedIndex = 0;
			_surfaceCombo.SelectedIndexChanged += (s, e) => Car.Surface = (VirtualCarSurface)_surfaceCombo.SelectedIndex;

			_gradeUpDown = new NumericUpDown
			{
				Name = "RoadGradeUpDown",
				AccessibleName = "Road grade percent",
				AccessibleDescription = "Simulated uphill/downhill road grade in percent (-20 to +20 %) sent as pitch in telemetry packets.",
				Minimum = -20,
				Maximum = 20,
				DecimalPlaces = 1,
				Increment = 1,
				Value = 0,
			};
			_gradeUpDown.ValueChanged += (s, e) => Car.RoadGradePercent = (float)_gradeUpDown.Value;

			_autoDriveCheck = new CheckBox
			{
				Name = "AutoDriveCheckBox",
				Text = "Auto-Drive",
				AccessibleName = "Auto-Drive",
				AccessibleDescription = "Drives the virtual car automatically around the track so you can test telemetry hands-free.",
				AutoSize = true,
				UseVisualStyleBackColor = true,
			};
			_autoDriveCheck.CheckedChanged += (s, e) => Car.AutoDrive = _autoDriveCheck.Checked;

			_curbLeftButton = new Button
			{
				Name = "HitCurbLeftButton",
				Text = "Hit Curb L",
				AccessibleName = "Hit left curb",
				AccessibleDescription = "Triggers a left-side curb strike impulse.",
				AutoSize = true,
				UseVisualStyleBackColor = true,
			};
			_curbLeftButton.Click += (s, e) => Car.TriggerCurb(-1f);

			_curbRightButton = new Button
			{
				Name = "HitCurbRightButton",
				Text = "Hit Curb R",
				AccessibleName = "Hit right curb",
				AccessibleDescription = "Triggers a right-side curb strike impulse.",
				AutoSize = true,
				UseVisualStyleBackColor = true,
			};
			_curbRightButton.Click += (s, e) => Car.TriggerCurb(1f);

			_resetButton = new Button
			{
				Name = "ResetCarButton",
				Text = "Reset Car",
				AccessibleName = "Reset car",
				AccessibleDescription = "Returns the virtual car to the starting straight.",
				AutoSize = true,
				UseVisualStyleBackColor = true,
			};
			_resetButton.Click += (s, e) => { Car.Reset(); _canvas.Invalidate(); };

			var hintLabel = Caption("(WASD / Arrows + Space also drive)");
			hintLabel.ForeColor = SystemColors.GrayText;

			driveRow.Controls.AddRange(new Control[]
			{
				Caption("Drive:"), _controllerCombo,
				Caption("Surface:"), _surfaceCombo,
				Caption("Grade %:"), _gradeUpDown,
				_autoDriveCheck, _curbLeftButton, _curbRightButton, _resetButton,
				hintLabel,
			});

			// Row 1: Telemetry UDP transmitter controls.
			var txRow = new FlowLayoutPanel
			{
				Name = "TelemetryControlsPanel",
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Dock = DockStyle.Fill,
				WrapContents = true,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			_sendTelemetryCheck = new CheckBox
			{
				Name = "SendTelemetryCheckBox",
				Text = "Send telemetry",
				AccessibleName = "Send UDP telemetry",
				AccessibleDescription = "Streams standard racing telemetry packets to x360ce (default 127.0.0.1:37150).",
				AutoSize = true,
				Checked = true,
				UseVisualStyleBackColor = true,
			};

			_protocolCombo = new ComboBox
			{
				Name = "TelemetryProtocolComboBox",
				AccessibleName = "Telemetry protocol",
				AccessibleDescription = "Standard game telemetry wire format to transmit.",
				DropDownStyle = ComboBoxStyle.DropDownList,
			};
			_protocolCombo.Items.AddRange(new object[]
			{
				"Forza Data Out (323 B)",
				"EA F1 2024 UDP",
				"Codemasters UDP (264 B)",
				"OutSim UDP (64 B)",
				"Project CARS 2 UDP",
			});
			_protocolCombo.SelectedIndex = 0;

			_ipTextBox = new TextBox
			{
				Name = "TelemetryIpTextBox",
				AccessibleName = "Target IP",
				AccessibleDescription = "Destination IP address for UDP telemetry packets (default 127.0.0.1).",
				Text = "127.0.0.1",
			};
			_ipTextBox.TextChanged += (s, e) => UpdateTargetEndpoint();

			_portUpDown = new NumericUpDown
			{
				Name = "TelemetryPortUpDown",
				AccessibleName = "Target UDP port",
				AccessibleDescription = "Destination UDP port (37150 is x360ce's conflict-free default).",
				Minimum = 1024,
				Maximum = 65535,
				Value = 37150,
			};
			_portUpDown.ValueChanged += (s, e) => UpdateTargetEndpoint();

			_sendRumbleCheck = new CheckBox
			{
				Name = "SendRumbleCheckBox",
				Text = "Send XInput rumble",
				AccessibleName = "Send XInput rumble",
				AccessibleDescription = "Sends simulated tire-scrub and curb vibration back to the selected XInput controller.",
				AutoSize = true,
				Checked = true,
				UseVisualStyleBackColor = true,
			};

			_txStatusLabel = new Label
			{
				Name = "TelemetryTxStatusLabel",
				AccessibleName = "Telemetry transmit status",
				AccessibleDescription = "Count and rate of UDP telemetry packets sent.",
				AutoSize = true,
				ForeColor = SystemColors.GrayText,
				Text = "Idle",
			};

			txRow.Controls.AddRange(new Control[]
			{
				_sendTelemetryCheck,
				Caption("Protocol:"), _protocolCombo,
				Caption("IP:"), _ipTextBox,
				Caption("Port:"), _portUpDown,
				_sendRumbleCheck,
				_txStatusLabel,
			});

			// Row 2: 2D Track Canvas (left 68%) + Live Telemetry HUD (right 32%).
			var content = new TableLayoutPanel
			{
				Name = "VirtualCarContentPanel",
				Dock = DockStyle.Fill,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty,
			};
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
			content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
			content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			_canvas = new TrackCanvas(Car)
			{
				Name = "TrackCanvas",
				AccessibleName = "Virtual track",
				AccessibleDescription = "Top-down view of the virtual car, track curbs and tire skid trail.",
				Dock = DockStyle.Fill,
			};

			_hudTextBox = new TextBox
			{
				Name = "CarHudTextBox",
				AccessibleName = "Virtual car telemetry",
				AccessibleDescription = "Live speed, gear, RPM, steering, grip, slip angle, lateral G, road grade and curb impulse.",
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				TabStop = false,
				ScrollBars = ScrollBars.Vertical,
				BackColor = SystemColors.Window,
			};

			_spacing.Add(g =>
			{
				var itemMargin = new Padding(g, g, 0, 0);
				_controllerCombo.Width = g * 12;
				_controllerCombo.Margin = itemMargin;
				_surfaceCombo.Width = g * 13;
				_surfaceCombo.Margin = itemMargin;
				_gradeUpDown.Width = g * 8;
				_gradeUpDown.Margin = itemMargin;
				_autoDriveCheck.Margin = itemMargin;
				_curbLeftButton.Margin = itemMargin;
				_curbRightButton.Margin = itemMargin;
				_resetButton.Margin = itemMargin;
				_sendTelemetryCheck.Margin = itemMargin;
				_protocolCombo.Width = g * 21;
				_protocolCombo.Margin = itemMargin;
				_ipTextBox.Width = g * 12;
				_ipTextBox.Margin = itemMargin;
				_portUpDown.Width = g * 9;
				_portUpDown.Margin = itemMargin;
				_sendRumbleCheck.Margin = itemMargin;
				_txStatusLabel.Margin = itemMargin;
				_canvas.Margin = itemMargin;
				_hudTextBox.Margin = itemMargin;
			});

			content.Controls.Add(_canvas, 0, 0);
			content.Controls.Add(_hudTextBox, 1, 0);

			_root.Controls.Add(driveRow, 0, 0);
			_root.Controls.Add(txRow, 0, 1);
			_root.Controls.Add(content, 0, 2);
			Controls.Add(_root);

			ApplyFont();
			ResumeLayout(false);
			PerformLayout();

			UpdateTargetEndpoint();
			_hudTextBox.Text = FormatHud();
		}

		void ApplyFont()
		{
			var old = _monoFont;
			_monoFont = new Font(FontFamily.GenericMonospace, Font.Size);
			_hudTextBox.Font = _monoFont;
			if (old != null)
				old.Dispose();
			var dpiScale = 1.0;
			try
			{
				using (var g = Graphics.FromHwnd(IntPtr.Zero))
					dpiScale = g.DpiX / 96.0;
			}
			catch
			{
				dpiScale = 1.0;
			}
			var gap = Math.Max(4, (int)Math.Round(8.0 * dpiScale));
			for (var i = 0; i < _spacing.Count; i++)
				_spacing[i](gap);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (_root != null)
				ApplyFont();
		}

		Label Caption(string text)
		{
			var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
			_spacing.Add(g => label.Margin = new Padding(g, g, 0, 0));
			return label;
		}

		public VirtualCarProtocol Protocol
		{
			get { return (VirtualCarProtocol)Math.Max(0, _protocolCombo.SelectedIndex); }
			set { _protocolCombo.SelectedIndex = (int)value; }
		}

		public bool SendTelemetry
		{
			get { return _sendTelemetryCheck.Checked; }
			set { _sendTelemetryCheck.Checked = value; }
		}

		public int TargetPort
		{
			get { return (int)_portUpDown.Value; }
			set { _portUpDown.Value = Math.Max(1024, Math.Min(65535, value)); }
		}

		public bool AutoDrive
		{
			get { return _autoDriveCheck.Checked; }
			set { _autoDriveCheck.Checked = value; }
		}

		public int ControllerIndex
		{
			get { return Math.Max(0, _controllerCombo.SelectedIndex); }
			set { _controllerCombo.SelectedIndex = Math.Max(0, Math.Min(3, value)); }
		}

		public long PacketsSent { get { return _packetsSent; } }
		public int SendRateHz { get { return _sendHz; } }

		public void SetKeyState(Keys key, bool down)
		{
			if (key == Keys.A || key == Keys.Left) _keyLeft = down;
			else if (key == Keys.D || key == Keys.Right) _keyRight = down;
			else if (key == Keys.W || key == Keys.Up) _keyUp = down;
			else if (key == Keys.S || key == Keys.Down) _keyDown = down;
			else if (key == Keys.Space) _keySpace = down;
		}

		public void ClearKeys()
		{
			_keyLeft = _keyRight = _keyUp = _keyDown = _keySpace = false;
		}

		bool _lastButtonA;
		bool _lastButtonX;
		bool _lastButtonY;

		/// <summary>Called on MainForm's poll timer (~60-100 Hz); advances physics, sends UDP telemetry and updates HUD.</summary>
		public void Tick(XInputReader xinput, bool activeTab)
		{
			var nowMs = _clock.ElapsedMilliseconds;
			var dtMs = _lastStepMs == 0 ? 16 : nowMs - _lastStepMs;
			if (dtMs < 14)
				return;
			_lastStepMs = nowMs;

			var idx = ControllerIndex;
			float steer = (_keyRight ? 1f : 0f) - (_keyLeft ? 1f : 0f);
			float throttle = _keyUp ? 1f : 0f;
			float brake = _keyDown ? 1f : 0f;
			bool handbrake = _keySpace;

			if (xinput != null && idx >= 0 && idx < 4 && xinput.Connected[idx])
			{
				var gp = xinput.States[idx].Gamepad;
				var xiSteer = gp.ThumbLX / 32767f;
				if (Math.Abs(xiSteer) < 0.04f) xiSteer = 0f;
				if (Math.Abs(xiSteer) > Math.Abs(steer))
					steer = xiSteer;
				throttle = Math.Max(throttle, gp.RightTrigger / 255f);
				brake = Math.Max(brake, gp.LeftTrigger / 255f);
				// Standard XInput button masks: LeftShoulder=0x0100, A=0x1000, B=0x2000, X=0x4000, Y=0x8000.
				if ((gp.Buttons & (0x2000 | 0x0100)) != 0)
					handbrake = true;

				var btnA = (gp.Buttons & 0x1000) != 0;
				if (btnA && !_lastButtonA && activeTab)
					_autoDriveCheck.Checked = !_autoDriveCheck.Checked;
				_lastButtonA = btnA;

				var btnX = (gp.Buttons & 0x4000) != 0;
				if (btnX && !_lastButtonX && activeTab)
					Car.TriggerCurb(steer < 0 ? -1f : 1f);
				_lastButtonX = btnX;

				var btnY = (gp.Buttons & 0x8000) != 0;
				if (btnY && !_lastButtonY && activeTab)
					Car.Reset();
				_lastButtonY = btnY;
			}

			Car.SteeringInput = steer;
			Car.ThrottleInput = throttle;
			Car.BrakeInput = brake;
			Car.Handbrake = handbrake;

			// Only step physics & emit UDP while the Virtual Car tab is active or Auto-Drive is running.
			var running = activeTab || Car.AutoDrive;
			if (!running)
				return;

			Car.Step(dtMs / 1000.0);

			if (_sendTelemetryCheck.Checked)
				TransmitTelemetry((uint)nowMs);
			else
				_txStatusLabel.Text = "Off";

			if (_sendRumbleCheck.Checked && xinput != null && idx >= 0 && idx < 4 && xinput.Connected[idx])
			{
				XInputReader.Rumble(idx + 1,
					(int)Math.Round(Car.RumbleLeft * 65535f),
					(int)Math.Round(Car.RumbleRight * 65535f));
			}

			if (activeTab)
			{
				_canvas.Invalidate();
				_hudTextBox.Text = FormatHud();
			}
		}

		void UpdateTargetEndpoint()
		{
			_targetIp = _ipTextBox.Text.Trim();
			_targetPort = (int)_portUpDown.Value;
			IPAddress ip;
			if (IPAddress.TryParse(_targetIp, out ip))
			{
				_target = new IPEndPoint(ip, _targetPort);
				_lastTxError = null;
			}
			else
			{
				_target = null;
				_lastTxError = "Invalid IP";
			}
		}

		void TransmitTelemetry(uint timestampMs)
		{
			if (_target == null)
			{
				_txStatusLabel.Text = _lastTxError ?? "Invalid target";
				return;
			}
			try
			{
				if (_udp == null)
					_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				var packets = Car.BuildPackets(Protocol, timestampMs);
				for (var i = 0; i < packets.Length; i++)
				{
					_udp.SendTo(packets[i], _target);
					_packetsSent++;
					_sentThisSecond++;
				}
				var now = _clock.ElapsedMilliseconds;
				if (_rateWindowMs == 0)
					_rateWindowMs = now;
				else if (now - _rateWindowMs >= 1000)
				{
					_sendHz = _sentThisSecond;
					_sentThisSecond = 0;
					_rateWindowMs = now;
				}
				_txStatusLabel.Text = string.Format(CultureInfo.InvariantCulture, "Sent {0} ({1} Hz) -> {2}:{3}",
					_packetsSent, _sendHz, _targetIp, _targetPort);
			}
			catch (SocketException ex)
			{
				_txStatusLabel.Text = "UDP error: " + ex.SocketErrorCode;
			}
		}

		string FormatHud()
		{
			return string.Format(CultureInfo.InvariantCulture,
				"=== VIRTUAL CAR ===\r\n" +
				"Speed:     {0,6:0.0} km/h\r\n" +
				"Gear / RPM:  {1} / {2,4:0}\r\n" +
				"Surface:   {3}\r\n" +
				"RoadGrade: {17,5:+0.0;-0.0;0.0} %\r\n" +
				"AutoDrive: {4}\r\n" +
				"\r\n=== INPUTS ===\r\n" +
				"Steer:     {5,6:+0.00;-0.00;0.00}\r\n" +
				"Throttle:  {6,5:0} %\r\n" +
				"Brake:     {7,5:0} %\r\n" +
				"Handbrake: {8}\r\n" +
				"\r\n=== LIVE TELEMETRY ===\r\n" +
				"FrontGrip: {9,5:0} %\r\n" +
				"Aligning:  {10,6:+0.1;-0.1;0.0} deg\r\n" +
				"BodySlip:  {11,6:+0.1;-0.1;0.0} deg\r\n" +
				"LateralG:  {12,6:+0.2;-0.2;0.00} G\r\n" +
				"CurbKick:  {13,5:+0;-0;0} %\r\n" +
				"OnCurb:    {14}\r\n" +
				"Rumble L/R:{15,3:0}% / {16,3:0}%\r\n",
				Car.SpeedKmh,
				Car.Gear, Car.Rpm,
				Car.Surface,
				Car.AutoDrive ? "ON" : "OFF",
				Car.SteeringInput,
				Car.ThrottleInput * 100f,
				Car.BrakeInput * 100f,
				Car.Handbrake ? "ON" : "OFF",
				Car.FrontGrip * 100f,
				Car.AlignAngleDeg,
				Car.BodySlipDeg,
				Car.LateralG,
				Car.CurbImpulse * 100f,
				Car.OnCurb ? "YES" : "no",
				Car.RumbleLeft * 100f, Car.RumbleRight * 100f,
				Car.RoadGradePercent);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_udp != null)
				{
					_udp.Dispose();
					_udp = null;
				}
				if (_monoFont != null)
				{
					_monoFont.Dispose();
					_monoFont = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>Double-buffered top-down 2D circuit and car renderer.</summary>
		sealed class TrackCanvas : Control
		{
			readonly VirtualCarSimulation _car;

			public TrackCanvas(VirtualCarSimulation car)
			{
				_car = car;
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
				TabStop = false;
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				var rect = ClientRectangle;
				if (rect.Width < 20 || rect.Height < 20)
					return;

				var bgColor = _car.Surface == VirtualCarSurface.Ice
					? Color.FromArgb(205, 224, 236)
					: _car.Surface == VirtualCarSurface.Gravel
						? Color.FromArgb(148, 128, 98)
						: Color.FromArgb(52, 96, 56);
				using (var bg = new SolidBrush(bgColor))
					g.FillRectangle(bg, rect);

				var worldW = (float)((VirtualCarSimulation.TrackStraightHalf + VirtualCarSimulation.TrackCornerRadius + VirtualCarSimulation.TrackHalfWidth + 16.0) * 2.0);
				var worldH = (float)((VirtualCarSimulation.TrackCornerRadius + VirtualCarSimulation.TrackHalfWidth + 16.0) * 2.0);
				var scale = Math.Min((rect.Width - 16f) / worldW, (rect.Height - 16f) / worldH);
				var cx = rect.Width * 0.5f;
				var cy = rect.Height * 0.5f;

				g.TranslateTransform(cx, cy);
				g.ScaleTransform(scale, scale);

				var halfS = (float)VirtualCarSimulation.TrackStraightHalf;
				var rad = (float)VirtualCarSimulation.TrackCornerRadius;
				var roadW = (float)(VirtualCarSimulation.TrackHalfWidth * 2.0);

				using (var trackPath = BuildStadiumPath(halfS, rad))
				{
					// Outer red/white curb bands.
					using (var curbRed = new Pen(Color.FromArgb(205, 48, 48), roadW + (float)(VirtualCarSimulation.CurbOuterWidth * 2.0)))
					using (var curbWhite = new Pen(Color.FromArgb(240, 240, 240), roadW + (float)(VirtualCarSimulation.CurbOuterWidth * 2.0)) { DashPattern = new[] { 1.4f, 1.4f } })
					{
						g.DrawPath(curbRed, trackPath);
						g.DrawPath(curbWhite, trackPath);
					}

					// Road surface.
					var asphalt = _car.Surface == VirtualCarSurface.WetTarmac
						? Color.FromArgb(58, 64, 72)
						: _car.Surface == VirtualCarSurface.Gravel
							? Color.FromArgb(132, 108, 78)
							: _car.Surface == VirtualCarSurface.Ice
								? Color.FromArgb(175, 206, 224)
								: Color.FromArgb(68, 70, 76);
					using (var roadPen = new Pen(asphalt, roadW))
						g.DrawPath(roadPen, trackPath);

					// Centre dashed line.
					using (var centerPen = new Pen(Color.FromArgb(165, 255, 255, 255), 0.7f) { DashPattern = new[] { 6f, 6f } })
						g.DrawPath(centerPen, trackPath);
				}

				// Start/finish line on bottom straight.
				using (var sfPen = new Pen(Color.White, 1.4f))
					g.DrawLine(sfPen, 0f, rad - (float)VirtualCarSimulation.TrackHalfWidth, 0f, rad + (float)VirtualCarSimulation.TrackHalfWidth);

				// Tire trail (red/orange when slipping, soft gray when gripping).
				for (var i = 1; i < _car.Trail.Count; i++)
				{
					var c = _car.TrailSlipping[i] ? Color.FromArgb(180, 220, 75, 45) : Color.FromArgb(85, 230, 230, 230);
					using (var p = new Pen(c, _car.TrailSlipping[i] ? 1.5f : 0.9f))
						g.DrawLine(p, _car.Trail[i - 1], _car.Trail[i]);
				}

				// Draw the car.
				g.TranslateTransform((float)_car.X, (float)_car.Y);
				g.RotateTransform((float)(_car.HeadingRad * 180.0 / Math.PI));

				// Car chassis colour shifts with front grip (blue = gripped, orange/red = understeering).
				var bodyColor = _car.FrontGrip < 0.55f
					? Color.FromArgb(225, 65, 50)
					: _car.FrontGrip < 0.85f
						? Color.FromArgb(230, 150, 35)
						: Color.FromArgb(40, 145, 235);

				// Four wheels (front wheels visibly turn with SteeringInput).
				var frontAngle = _car.SteeringInput * 32f;
				DrawWheel(g, -3.4f, -2.5f, 0f);
				DrawWheel(g, -3.4f, 2.5f, 0f);
				DrawWheel(g, 3.4f, -2.5f, frontAngle);
				DrawWheel(g, 3.4f, 2.5f, frontAngle);

				using (var body = new SolidBrush(bodyColor))
				using (var outline = new Pen(Color.White, 0.55f))
				{
					g.FillRectangle(body, -5.2f, -2.4f, 10.4f, 4.8f);
					g.DrawRectangle(outline, -5.2f, -2.4f, 10.4f, 4.8f);
				}
				// Windshield pointing forward (+X).
				using (var glass = new SolidBrush(Color.FromArgb(215, 235, 250)))
					g.FillRectangle(glass, 0.8f, -1.8f, 2.4f, 3.6f);

				g.ResetTransform();
				using (var border = new Pen(SystemColors.ControlDark))
					g.DrawRectangle(border, 0, 0, rect.Width - 1, rect.Height - 1);
			}

			static void DrawWheel(Graphics g, float x, float y, float angleDeg)
			{
				var state = g.Save();
				g.TranslateTransform(x, y);
				g.RotateTransform(angleDeg);
				using (var br = new SolidBrush(Color.FromArgb(25, 25, 28)))
					g.FillRectangle(br, -1.4f, -0.65f, 2.8f, 1.3f);
				g.Restore(state);
			}

			static GraphicsPath BuildStadiumPath(float halfStraight, float radius)
			{
				var p = new GraphicsPath();
				p.AddArc(halfStraight - radius, -radius, radius * 2f, radius * 2f, -90f, 180f);
				p.AddArc(-halfStraight - radius, -radius, radius * 2f, radius * 2f, 90f, 180f);
				p.CloseFigure();
				return p;
			}
		}
	}
}
