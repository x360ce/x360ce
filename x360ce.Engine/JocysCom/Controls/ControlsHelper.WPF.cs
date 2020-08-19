using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class ControlsHelper
	{
		public static void EnableWithDelay(UIElement control)
		{
			Task.Run(async () =>
			{
				await Task.Delay(500).ConfigureAwait(true);
				control.Dispatcher.Invoke(() => control.IsEnabled = true);
			});
		}

		/// <summary>
		/// Set form TopMost if one of the application forms is top most.
		/// </summary>
		/// <param name="win"></param>
		public static void CheckTopMost(Window win)
		{
			// If this form is not set as TopMost but one of the application forms is on TopMost then...
			// Make this dialog form TopMost too or user won't be able to access it.
			if (!win.Topmost && System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(x => x.TopMost))
				win.Topmost = true;
		}

		public static void AutoSizeByOpenForms(Window win, int  addSize = -64)
		{
			var form = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().First();
			win.Width = form.Width  + addSize;
			win.Height = form.Height + addSize;
			win.Top = form.Top - addSize / 2;
			win.Left = form.Left - addSize / 2;
		}

		public static bool IsDesignMode(UIElement component)
		{
			return DesignerProperties.GetIsInDesignMode(component);
		}

	}
}
