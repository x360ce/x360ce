using JocysCom.ClassLibrary.Controls;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Hidden Form which will receive Windows messages about device insertion and removal.
	/// </summary>
	public partial class DeviceDetectorForm : Form
	{
		public DeviceDetectorForm()
		{
			ControlsHelper.InitInvokeContext();
			InitializeComponent();
		}

		public DeviceDetectorForm(DeviceDetector detector)
		{
			ControlsHelper.InitInvokeContext();
			InitializeComponent();
			_Detector = detector;
			// This form is never shown and is owned by whichever thread built it, which for
			// device polling is a worker thread with no message loop. Nothing here may touch
			// its controls: the form exists only to own a window handle for WndProc.
		}

		DeviceDetector _Detector = null;

		/// <summary>
		/// This function receives all the windows messages for this window (form).
		/// We call the DeviceDetector from here so that is can pick up the messages about
		/// drives arrived and removed.
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (_Detector != null)
			{
				_Detector.WndProc(ref m);
			}
		}

	}
}
