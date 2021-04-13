using System;
using System.Windows;

namespace x360ce.App
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// This overrides the windows messaging processing. Be careful with this method,
		/// because this method is responsible for all the windows messages that are coming to the form.
		/// </summary>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
			source.AddHook(StartHelper.CustomWndProc);
		}

	}
}
