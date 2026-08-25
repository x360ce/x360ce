using System.Windows.Forms;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Dialog wrapper around the shared error report control.
	/// </summary>
	public partial class ErrorReportForm : Form
	{
		public ErrorReportForm()
		{
			InitializeComponent();
			ErrorReportPanel.SupportEmail = "support@x360ce.com";
		}
	}
}
