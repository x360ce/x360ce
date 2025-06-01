using JocysCom.ClassLibrary.Controls;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>Hidden form that receives Windows messages about device insertion and removal.</summary>
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
			_Detector.DeviceChanged += new DeviceDetector.DeviceDetectorEventHandler(_Detector_DeviceChanged);
		}

		void _Detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				InfoLabel.Text = e.ChangeType.ToString();
			});
		}

		DeviceDetector _Detector = null;

		/// <summary>Forwards all window messages to the DeviceDetector so it can detect drive arrival and removal.</summary>
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
