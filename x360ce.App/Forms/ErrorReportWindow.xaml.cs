using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using x360ce.Engine;

namespace x360ce.App.Forms
{
	/// <summary>
	/// Interaction logic for ErrorReportWindow.xaml
	/// </summary>
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
	public partial class ErrorReportWindow : Window
	{
		public ErrorReportWindow()
		{
			InitHelper.InitTimer(this, InitializeComponent);
		}

		public void ErrorReportPanel_SendMessages(object sender, JocysCom.ClassLibrary.EventArgs<List<System.Net.Mail.MailMessage>> e)
		{
			var control = (ErrorReportControl)sender;
			// Create mail message.
			var win = (Forms.ErrorReportWindow)control.Parent;
			control.StatusLabel.Content = "Sending...";
			// Run cloud operation on a separate thread so that it won't freeze the app.
			Task.Run(new Action(() =>
			{
				var messages = e.Data.Select(x => new MailMessageSerializable(x)).ToArray();
				var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(messages.First());
				Global.CloudClient.Add(CloudAction.SendMailMessage, messages);
			}));
		}

	}
}
