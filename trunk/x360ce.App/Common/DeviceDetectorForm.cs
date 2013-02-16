using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>
	/// Hidden Form which will receive Windows messages about device insertion and removal.
	/// </summary>
	public partial class DeviceDetectorForm : Form
	{
		public DeviceDetectorForm()
		{
			InitializeComponent();
		}

		public DeviceDetectorForm(DeviceDetector detector)
		{
			InitializeComponent();
			_Detector = detector;
			_Detector.DeviceChanged += new DeviceDetector.DeviceDetectorEventHandler(_Detector_DeviceChanged);
		}

		void _Detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			InfoLabel.Text = e.ChangeType.ToString();
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
