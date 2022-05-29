using System.Windows.Controls;


namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Interaction logic for ProgressBarControl.xaml
	/// </summary>
	public partial class ProgressBarControl : UserControl
	{
		public ProgressBarControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			ControlsHelper.SetText(ProgressLevelTopBarTextBlock, "");
			ControlsHelper.SetText(ProgressLevelSubBarTextBlock, "");
		}

		public void UpdateProgress(ProgressEventArgs e)
		{
			if (e.TopCount > 0)
			{
				if (ProgressLevelTopBar.Maximum != e.TopCount)
					ProgressLevelTopBar.Maximum = e.TopCount;
				if (ProgressLevelTopBar.Value != e.TopIndex)
					ProgressLevelTopBar.Value = e.TopIndex;
			}
			else
			{
				if (ProgressLevelTopBar.Maximum != 100)
					ProgressLevelTopBar.Maximum = 100;
				if (ProgressLevelTopBar.Value != 0)
					ProgressLevelTopBar.Value = 0;
			}
			if (e.SubCount > 0)
			{
				if (ProgressLevelSubBar.Maximum != e.SubCount)
					ProgressLevelSubBar.Maximum = e.SubCount;
				if (ProgressLevelSubBar.Value != e.SubIndex)
					ProgressLevelSubBar.Value = e.SubIndex;
			}
			else
			{
				if (ProgressLevelSubBar.Maximum != 100)
					ProgressLevelSubBar.Maximum = 100;
				if (ProgressLevelSubBar.Value != 0)
					ProgressLevelSubBar.Value = 0;
			}
			// Create top message.
			var tc = e.TopProgressText;
			if (tc == null)
			{
				tc += $"{e.TopIndex}";
				if (e.TopCount > 0)
					tc += $"/{e.TopCount}";
			}
			ControlsHelper.SetText(ProgressLevelTopBarTextBlock, tc);
			// Create sub message.
			var sc = e.SubProgressText;
			if (sc == null)
			{
				sc += $"{e.SubIndex}";
				if (e.SubCount > 0)
					sc += $"/{e.SubCount}";
			}
			ControlsHelper.SetText(ProgressLevelSubBarTextBlock, sc);
			UpdateProgress(e.TopMessage, e.SubMessage);
		}

		public void UpdateProgress(string topText = "", string SubText = "", bool? resetBars = null)
		{
			ControlsHelper.SetText(ProgressLevelTopLabel, topText);
			ControlsHelper.SetText(ProgressLevelSubLabel, SubText);
			if (resetBars.GetValueOrDefault())
			{
				ProgressLevelTopBar.Maximum = 100;
				ProgressLevelTopBar.Value = 0;
				ProgressLevelSubBar.Maximum = 100;
				ProgressLevelSubBar.Value = 0;
			}
			ControlsHelper.SetVisible(ScanProgressPanel, !string.IsNullOrEmpty(topText));
		}




	}
}
