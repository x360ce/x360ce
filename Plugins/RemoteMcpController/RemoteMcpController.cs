using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.Plugins;

namespace x360ce.Plugins.Samples
{
	/// <summary>A pretend controller whose controls are set from outside over MCP: by an AI assistant or a program, or by a person typing.</summary>
	/// <remarks>
	/// Its State setting takes the controls to change as JSON, named and valued as in x360ce's formulas. x360ce sets the
	/// settings on the thread that calls <see cref="Read"/>, before each read, so State is taken apart only when its text
	/// changes, and nothing needs a lock.
	/// </remarks>
	[Description("A pretend controller whose controls are set from outside, so a mapping, a formula or an AutoHotkey script "
		+ "can be tried without hardware and without hands. Its State setting takes the controls to change as JSON, named and "
		+ "valued as in formulas: {\"b1\":1,\"a1\":-0.5,\"s1\":0.25,\"p1\":90} presses button 1, holds axis 1 a quarter of the way "
		+ "left, slider 1 a quarter of the way along and hat 1 right. A control the text does not name keeps its value; "
		+ "{\"rest\":1} puts every control at rest first. A program or an AI assistant sets State over MCP with set_state, and "
		+ "reads Now back with read: on the controller's own MCP server, which its tab opens, or through x360ce's door with "
		+ "device_call. State is kept with the other settings, so after x360ce starts again the controller holds what it "
		+ "held: send {\"rest\":1} to let go.")]
	public class RemoteMcpController : IPluginDevice
	{
		static readonly Guid Product = new Guid("884e9137-a970-446b-962f-31ed8c2abb11");

		public string Name { get { return "Remote MCP Controller"; } }
		public Guid ProductGuid { get { return Product; } }
		public int AxisCount { get { return 6; } }
		public int SliderCount { get { return 8; } }
		public int PovCount { get { return 4; } }
		public int ButtonCount { get { return 128; } }

		/// <summary>The controller's own MCP server, on the port of the controller and place it is mapped to: 37141 on controller 4, first.</summary>
		public PluginMcpServer McpServer { get; } = new PluginMcpServer();

		/// <summary>Where the controls are, as State last left them, copied into x360ce's state on each read.</summary>
		readonly PluginState _held = new PluginState();

		string _state = "";

		[Category("Remote"), DisplayName("State")]
		[Description("The controls to change, as JSON named and valued as in formulas: b1 to b128 a button, 1 or 0; a1 to a6 an "
			+ "axis, -1 to 1; s1 to s8 a slider, 0 to 1; p1 to p4 a hat, degrees clockwise from up or -1 at rest. "
			+ "\"rest\":1 puts every control at rest before the others are set.")]
		public string State
		{
			get { return _state; }
			set
			{
				value = value ?? "";
				if (value == _state)
					return;
				_state = value;
				Error = Apply(value);
			}
		}

		[Category("Remote"), DisplayName("Now")]
		[Description("Every control that is not at rest, as JSON in the form State takes.")]
		public string Now { get { return Describe(); } }

		[Category("Remote"), DisplayName("Error")]
		[Description("What in the last State could not be set, or empty.")]
		public string Error { get; private set; } = "";

		public void Read(PluginState state)
		{
			Array.Copy(_held.Axes, state.Axes, state.Axes.Length);
			Array.Copy(_held.Sliders, state.Sliders, state.Sliders.Length);
			Array.Copy(_held.Povs, state.Povs, state.Povs.Length);
			Array.Copy(_held.Buttons, state.Buttons, state.Buttons.Length);
		}

		public RemoteMcpController()
		{
			Rest();
		}

		void Rest()
		{
			for (var i = 0; i < _held.Axes.Length; i++)
				_held.Axes[i] = PluginState.Middle;
			Array.Clear(_held.Sliders, 0, _held.Sliders.Length);
			for (var i = 0; i < _held.Povs.Length; i++)
				_held.Povs[i] = PluginState.PovReleased;
			Array.Clear(_held.Buttons, 0, _held.Buttons.Length);
		}

		/// <summary>One "name": value pair of State; the value is a number, true or false.</summary>
		static readonly Regex Pair = new Regex("\"\\s*([A-Za-z]+)(\\d*)\\s*\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?|true|false)", RegexOptions.IgnoreCase);

		/// <summary>Sets the controls a State names, and says what it could not set.</summary>
		string Apply(string json)
		{
			var errors = new StringBuilder();
			var matches = Pair.Matches(json);
			if (matches.Count == 0 && json.Trim().Trim('{', '}').Trim().Length > 0)
				return "Not JSON with controls in it, such as {\"b1\":1}.";
			// "rest" first, wherever it is, so it never undoes a control set beside it.
			foreach (Match m in matches)
			{
				if (string.Equals(m.Groups[1].Value, "rest", StringComparison.OrdinalIgnoreCase) && Number(m.Groups[3].Value) != 0)
					Rest();
			}
			foreach (Match m in matches)
			{
				var letter = m.Groups[1].Value.ToLowerInvariant();
				if (letter == "rest")
					continue;
				int index;
				int.TryParse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out index);
				var value = Number(m.Groups[3].Value);
				var name = letter + m.Groups[2].Value;
				switch (letter)
				{
					case "b":
						if (Within(index, _held.Buttons.Length, name, errors))
							_held.Buttons[index - 1] = value != 0;
						break;
					case "a":
						if (Within(index, _held.Axes.Length, name, errors))
							_held.Axes[index - 1] = Clamp(PluginState.Middle + value * PluginState.Middle);
						break;
					case "s":
						if (Within(index, _held.Sliders.Length, name, errors))
							_held.Sliders[index - 1] = Clamp(value * PluginState.Max);
						break;
					case "p":
						if (Within(index, _held.Povs.Length, name, errors))
							_held.Povs[index - 1] = value < 0 ? PluginState.PovReleased : (int)Math.Round((value % 360 + 360) % 360 * 100) % 36000;
						break;
					default:
						errors.Append("Not a control: ").Append(name).Append(". ");
						break;
				}
			}
			return errors.ToString().Trim();
		}

		static double Number(string text)
		{
			if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
				return 1;
			if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
				return 0;
			return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		static bool Within(int index, int count, string name, StringBuilder errors)
		{
			if (index >= 1 && index <= count)
				return true;
			errors.Append("No such control: ").Append(name).Append(". ");
			return false;
		}

		static int Clamp(double raw)
		{
			return (int)Math.Round(Math.Max(0, Math.Min(PluginState.Max, raw)));
		}

		/// <summary>Every control that is not at rest, in the form State takes.</summary>
		string Describe()
		{
			var text = new StringBuilder("{");
			for (var i = 0; i < _held.Buttons.Length; i++)
			{
				if (_held.Buttons[i])
					Add(text, "b" + (i + 1), "1");
			}
			for (var i = 0; i < _held.Axes.Length; i++)
			{
				if (_held.Axes[i] != PluginState.Middle)
					Add(text, "a" + (i + 1), ((_held.Axes[i] - PluginState.Middle) / (double)PluginState.Middle).ToString("0.###", CultureInfo.InvariantCulture));
			}
			for (var i = 0; i < _held.Sliders.Length; i++)
			{
				if (_held.Sliders[i] != 0)
					Add(text, "s" + (i + 1), (_held.Sliders[i] / (double)PluginState.Max).ToString("0.###", CultureInfo.InvariantCulture));
			}
			for (var i = 0; i < _held.Povs.Length; i++)
			{
				if (_held.Povs[i] != PluginState.PovReleased)
					Add(text, "p" + (i + 1), (_held.Povs[i] / 100.0).ToString("0.##", CultureInfo.InvariantCulture));
			}
			return text.Append("}").ToString();
		}

		static void Add(StringBuilder text, string name, string value)
		{
			if (text.Length > 1)
				text.Append(',');
			text.Append('"').Append(name).Append("\":").Append(value);
		}
	}
}
