using JocysCom.ClassLibrary.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Controls
{
	public partial class AboutControl : UserControl
	{
		public AboutControl()
		{
			InitializeComponent();
		}

		void AboutControl_Load(object sender, EventArgs e)
		{
			AppHelper.LoadHelp(ProgramRichTextBox, AppHelper.ProgramResource);
			var stream = EngineHelper.GetResourceStream("ChangeLog.txt");
			var sr = new StreamReader(stream);
			ChangeLogTextBox.Text = ControlsHelper.NormalizeLineBreaks(sr.ReadToEnd());
			// The program's own name and version: wherever it is hosted, as in a test, the process's product can be another's.
			var program = new JocysCom.ClassLibrary.Configuration.AssemblyInfo(typeof(AboutControl).Assembly);
			var version = program.Version;
			AboutProductLabel.Text = string.Format(AboutProductLabel.Text, version);
			stream = EngineHelper.GetResourceStream("License.txt");
			sr = new StreamReader(stream);
			var license = sr.ReadToEnd();
			// What x360ce carries from other projects is shown under its own licence, as each of those licences asks.
			license += CarriedLicence("SDL controller database", SdlDatabase.ProjectUrl, "zlib licence", SdlDatabase.ReadLicence());
			license += CarriedLicence("Selawik font", "https://github.com/microsoft/Selawik", "SIL Open Font License 1.1", AppHelper.ReadHelp("Fonts.Selawik.OFL.txt"));
			license += CarriedLicence("Inconsolata font", "https://github.com/googlefonts/Inconsolata", "SIL Open Font License 1.1", AppHelper.ReadHelp("Fonts.Inconsolata.OFL.txt"));
			LicenseTextBox.Text = ControlsHelper.NormalizeLineBreaks(license);
			LicenseTabPage.Text = string.Format("{0} {1} License", program.Product, version.ToString(2));
			SizeProductPicture();
		}

		/// <summary>
		/// Makes the program's picture as tall as the title and the subtitle beside it, from the top of the one to the bottom of
		/// the other, made from the version of the picture drawn nearest above that size, so it is never enlarged.
		/// </summary>
		/// <remarks>
		/// Shown zoomed, the picture is left alone when the window enlarges its images to the screen's scale: the text it is
		/// measured by is at that scale already.
		/// </remarks>
		void SizeProductPicture()
		{
			var side = AboutProductLabel.PreferredHeight + AboutProductLabel.Margin.Bottom
				+ AboutDescriptionLabel.Margin.Top + AboutDescriptionLabel.PreferredHeight;
			var drawn = new Image[] { Properties.Resources.app_16x16, Properties.Resources.app_48x48, Properties.Resources.app_128x128 }
				.Select(ControlsHelper.GetOriginal).ToArray();
			var source = drawn.FirstOrDefault(x => x.Height >= side) ?? drawn.Last();
			ProductPictureBox.Size = new Size(side, side);
			ProductPictureBox.Image = ControlsHelper.ScaleImage(source, ProductPictureBox.Size);
		}

		/// <summary>The licence of a project x360ce carries a part of, to put under x360ce's own; empty when the program does not carry its text.</summary>
		/// <param name="name">What is carried, such as "Selawik font".</param>
		/// <param name="url">Where the project lives.</param>
		/// <param name="licence">The licence's name.</param>
		/// <param name="text">The licence as the project gives it.</param>
		static string CarriedLicence(string name, string url, string licence, string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";
			return "\r\n\r\n" + new string('-', 72) + "\r\n\r\n" + name + " (" + url + "), " + licence + ":\r\n\r\n" + text;
		}

	}
}
