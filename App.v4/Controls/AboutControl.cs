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
			// What x360ce carries from other projects is shown under its own licence, as each of those licences asks.
			var license = AppHelper.ReadHelp("Documents.License.txt") + ThirdPartyLicences.Text(program.Assembly);
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
			ProductPictureBox.Size = new Size(side, side);
			ProductPictureBox.Image = ControlsHelper.ScaleImage(Properties.Resources.app_128x128, ProductPictureBox.Size);
		}

	}
}
