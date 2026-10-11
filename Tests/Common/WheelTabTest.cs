// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/Controls/PadTabPages/WheelChartUserControl.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The Wheel tab: the Car list that fills the centering spring's curve, the chart of the force at every angle, and
	/// a layout that keeps every control clear of the others.
	/// </summary>
	[TestClass]
	public class WheelTabTest
	{
		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The Car list offers the five cars in order, each naming what it is based on, and the first is the settings' defaults")]
		public void The_car_list_offers_five_cars_and_the_first_is_the_default()
		{
			var cars = PadControl.SpringCars;
			Assert.AreEqual(5, cars.Length);
			AssertCar(cars[0], "Family car, 100 km/h (ISO 13674-1 weave test, measured)", 12, 60, 40, 5, 100);
			AssertCar(cars[1], "Family car in town, 40 km/h (same car, slower)", 12, 60, 40, 5, 40);
			AssertCar(cars[2], "Parking (same car, 10 km/h)", 12, 60, 40, 0, 10);
			AssertCar(cars[3], "Sports car, 100 km/h (estimate, firm end of the ISO 13674-1 range)", 8, 40, 30, 5, 100);
			AssertCar(cars[4], "Heavy van, 80 km/h (estimate, slow end of the ISO 13674-1 range)", 30, 120, 50, 10, 80);
			var defaults = new PadSetting();
			Assert.AreEqual(0, PadControl.SpringCarIndex(defaults.GetForceSpringCentre(), defaults.GetForceSpringPeak(),
				defaults.GetForceSpringLightening(), defaults.GetForceSpringDamping(), defaults.GetForceSpringSpeed()),
				"Empty settings are not the family car at 100 km/h.");
			Assert.AreEqual(-1, PadControl.SpringCarIndex(12, 60, 40, 5, 99), "Values no car has matched a car.");
		}

		static void AssertCar(PadControl.SpringCar car, string name, int centre, int peak, int lightening, int damping, int speed)
		{
			Assert.AreEqual(name, car.Name);
			Assert.AreEqual(name, car.ToString(), "The list shows another text than the car's name.");
			Assert.AreEqual(centre, car.Centre, name);
			Assert.AreEqual(peak, car.Peak, name);
			Assert.AreEqual(lightening, car.Lightening, name);
			Assert.AreEqual(damping, car.Damping, name);
			Assert.AreEqual(speed, car.Speed, name);
		}

		[TestMethod, TestCategory("ui"), TestCategory("force-feedback")]
		[Description("The Car list shows the car the loaded values match; choosing a car sets the five sliders and leaves Strength; a slider moved off it shows Custom")]
		public void Choosing_a_car_sets_the_curve_and_a_moved_slider_shows_custom()
		{
			var preset = new PadSetting
			{
				ForceSpringStrength = "44",
				ForceSpringCentre = "8",
				ForceSpringPeak = "40",
				ForceSpringLightening = "30",
				ForceSpringDamping = "5",
				ForceSpringSpeed = "100",
			};
			PresetLoadTest.OnLoadedPage(preset, pad =>
			{
				var car = (ComboBox)Find(pad, "ForceSpringCarComboBox");
				Assert.AreEqual(PadControl.SpringCars[3].Name, car.GetItemText(car.SelectedItem), "The loaded sports car is not shown.");
				car.SelectedIndex = 4;
				var van = PadControl.SpringCars[4];
				Assert.AreEqual(van.Centre, Slider(pad, "ForceSpringCentreTrackBar"));
				Assert.AreEqual(van.Peak, Slider(pad, "ForceSpringPeakTrackBar"));
				Assert.AreEqual(van.Lightening, Slider(pad, "ForceSpringLighteningTrackBar"));
				Assert.AreEqual(van.Damping, Slider(pad, "ForceSpringDampingTrackBar"));
				Assert.AreEqual(van.Speed, Slider(pad, "ForceSpringSpeedTrackBar"));
				Assert.AreEqual(44, Slider(pad, "ForceSpringStrengthTrackBar"), "Choosing a car changed Strength.");
				var saved = pad.CloneCurrentPadSetting();
				Assert.AreEqual("30", saved.ForceSpringCentre, "The car's values do not reach the settings.");
				Assert.AreEqual("80", saved.ForceSpringSpeed, "The car's values do not reach the settings.");
				Assert.AreEqual(van.Name, car.GetItemText(car.SelectedItem), "The list left the car it set.");
				((TrackBar)Find(pad, "ForceSpringDampingTrackBar")).Value = 11;
				Assert.AreEqual(PadControl.CustomSpringCar, car.GetItemText(car.SelectedItem), "Values that match no car are not shown as Custom.");
				((TrackBar)Find(pad, "ForceSpringDampingTrackBar")).Value = van.Damping;
				Assert.AreEqual(van.Name, car.GetItemText(car.SelectedItem), "Values moved back onto a car do not show it.");
			});
		}

		static Control Find(Control root, string name)
		{
			return root.Controls.Find(name, true).Single();
		}

		static int Slider(Control root, string name)
		{
			return ((TrackBar)Find(root, name)).Value;
		}

		[TestMethod, TestCategory("ui"), TestCategory("force-feedback")]
		[Description("The chart draws the curve the engine plays, the friction band only when Auto measured one, and the live speed when one is set")]
		public void The_chart_draws_the_curve_the_engine_plays()
		{
			Ui.OnUiThread(() =>
			{
				using (var chart = new WheelChartUserControl { Size = new Size(640, 520) })
				{
					chart.CreateControl();
					chart.PerformLayout();
					chart.SetCurve(60, 12, 60, 40, 900, 100);
					var picture = (PictureBox)Find(chart, "MainPictureBox");
					var w = picture.ClientSize.Width;
					var h = picture.ClientSize.Height;
					var curve = new SpringCurve();
					curve.Set(60, 12, 60, 40, 900, 100);
					// A quarter of the way from the left stop: the force pushes right, above the middle.
					var x = w / 4;
					var force = curve.Force(Axis(x, w));
					Assert.IsTrue(force > 20, "The family car at 60 % pushes with only " + force + " % at a quarter of the way.");
					using (var plain = Draw(picture))
					{
						Assert.IsTrue(IsRedNear(plain, picture, x, YOf(force, h)), "The red curve is not where the engine's force is.");
						Assert.IsTrue(IsWhite(plain, picture, x, YOf(10, h)), "A band is drawn before Auto measured any friction.");
					}
					chart.UpdateWheel(null, null, 20);
					using (var banded = Draw(picture))
						Assert.IsFalse(IsWhite(banded, picture, x, YOf(10, h)), "The friction Auto measured is not drawn as a band.");
					// The wheel 32 degrees to the left: the header says so, and the force there pushes right.
					var position = (int)Math.Round(SpringCalibration.Center - 32.0 / 450 * SpringCalibration.Center);
					chart.UpdateWheel(position, null, 20);
					Assert.AreEqual("-32°", Find(chart, "AngleValueLabel").Text);
					Assert.AreEqual(curve.Force(position) + " % to the right", Find(chart, "ForceValueLabel").Text);
					// A live speed replaces the Speed setting, as it does in the engine.
					chart.UpdateWheel(position, 40, 20);
					var slower = new SpringCurve();
					slower.Set(60, 12, 60, 40, 900, 40);
					Assert.AreNotEqual(curve.Force(position), slower.Force(position), "The test angle cannot tell the two speeds apart.");
					Assert.AreEqual(slower.Force(position) + " % to the right", Find(chart, "ForceValueLabel").Text,
						"The chart ignores the live speed.");
				}
			});
		}

		static int Axis(int x, int width)
		{
			return (int)Math.Round((double)x / (width - 1) * SpringCalibration.AxisMax);
		}

		static int YOf(int forcePercent, int height)
		{
			var middle = (height - 1) / 2f;
			return (int)Math.Round(middle - forcePercent / 100f * middle);
		}

		/// <summary>The picture as it is painted, its border included.</summary>
		static Bitmap Draw(PictureBox picture)
		{
			var bitmap = new Bitmap(picture.Width, picture.Height);
			picture.DrawToBitmap(bitmap, new Rectangle(0, 0, picture.Width, picture.Height));
			return bitmap;
		}

		static Point ToBitmap(PictureBox picture, int x, int y)
		{
			var border = (picture.Width - picture.ClientSize.Width) / 2;
			return new Point(x + border, y + border);
		}

		/// <summary>Whether a red pixel is a few pixels above or below the point: the line goes through the middle of each whole percent.</summary>
		static bool IsRedNear(Bitmap bitmap, PictureBox picture, int x, int y)
		{
			for (var dy = -4; dy <= 4; dy++)
			{
				var p = ToBitmap(picture, x, y + dy);
				var c = bitmap.GetPixel(p.X, p.Y);
				if (c.R - c.G > 80 && c.R - c.B > 80)
					return true;
			}
			return false;
		}

		static bool IsWhite(Bitmap bitmap, PictureBox picture, int x, int y)
		{
			var p = ToBitmap(picture, x, y);
			var c = bitmap.GetPixel(p.X, p.Y);
			var window = SystemColors.Window;
			return c.R == window.R && c.G == window.G && c.B == window.B;
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("No two controls on the Wheel tab cover each other, at any width")]
		public void Wheel_tab_keeps_every_control_clear_of_the_others()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var pad = new PadControl(MapTo.Controller1))
				{
					pad.Dock = DockStyle.Fill;
					form.ClientSize = pad.Size;
					form.Controls.Add(pad);
					var tabs = (TabControl)Find(pad, "PadTabControl");
					var page = (TabPage)Find(pad, "WheelTabPage");
					Assert.AreEqual(tabs.TabPages.IndexOf((TabPage)Find(pad, "ForceFeedbackTabPage")) + 1, tabs.TabPages.IndexOf(page),
						"The Wheel tab is not right after Force Feedback.");
					tabs.SelectedTab = page;
					form.Show();
					Application.DoEvents();
					var drawn = form.ClientSize.Width;
					foreach (var width in new[] { drawn, (int)(drawn * 0.75), (int)(drawn * 1.5), drawn * 2 })
					{
						form.ClientSize = new Size(width, form.ClientSize.Height);
						Application.DoEvents();
						var covered = ControlOverlapTest.Covered(page).ToList();
						Assert.AreEqual(0, covered.Count, string.Format(
							"At {0} pixels wide, {1} control(s) are covered by another:{2}{3}",
							width, covered.Count, Environment.NewLine, string.Join(Environment.NewLine, covered)));
						var outside = page.Controls.Find("WheelSettingsTableLayoutPanel", true).Single().Controls.Cast<Control>()
							.Where(x => x.Visible && x.Right > x.Parent.ClientSize.Width).Select(x => x.Name).ToList();
						Assert.AreEqual(0, outside.Count, "At " + width + " pixels wide, past the edge: " + string.Join(", ", outside));
					}
				}
			});
		}
	}
}
