using System.Collections.Generic;
using static x360ce.Engine.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The INPUT column of the General tab: the device's own controls, lit as they are used.</summary>
		static void AddInputPanel(Dictionary<string, Text> d)
		{
			d["InputUserControl"] = new Text("Input",
				"Every control of the selected device, lit while it is used, before anything is mapped.");
			d["InputUserControl.SourceLabel"] = Live("Input source",
				"The device the chips below belong to, or that none is selected.");
			d["InputUserControl.ButtonsGroupBox"] = new Text("Buttons",
				"One chip per button, lit while it is held. Click or drag a chip into a mapping box.");
			d["InputUserControl.ButtonChips"] = new Text("Button chips",
				"The device's buttons, numbered as the mapping list numbers them.");
			d["InputUserControl.AxesGroupBox"] = new Text("Axes",
				"One chip per axis with its reading, lit while it is away from the centre.");
			d["InputUserControl.AxisChips"] = new Text("Axis chips",
				"The device's axes, numbered by the slot each answers to, as the mapping list numbers them.");
			d["InputUserControl.SlidersGroupBox"] = new Text("Sliders",
				"One chip per slider with its reading, lit while it is moved from zero.");
			d["InputUserControl.SliderChips"] = new Text("Slider chips",
				"The device's sliders, numbered as the mapping list numbers them.");
			d["InputUserControl.PovsGroupBox"] = new Text("POVs",
				"One chip per POV with its reading in degrees, then its four directions, lit while pressed.");
			d["InputUserControl.PovChips"] = new Text("POV chips",
				"Each POV followed by U, R, D and L: its up, right, down and left as mappable buttons.");
		}
	}
}
