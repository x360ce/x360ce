using System.Windows;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadFootControl.xaml
	/// </summary>
	public partial class PadFootControl : UserControl
	{
		public PadFootControl()
		{
			InitializeComponent();
		}

		PadSetting _padSetting;
		MapTo _MappedTo;

		public void SetBinding(MapTo mappedTo, PadSetting padSetting)
		{
			_MappedTo = mappedTo;
			_padSetting = padSetting;
		}

		private void GameControllersButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void DxTweakButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void PasteButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void AutoButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
