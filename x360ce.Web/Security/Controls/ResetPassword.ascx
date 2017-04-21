<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ResetPassword.ascx.cs"
	Inherits="JocysCom.Web.Security.Controls.ResetPassword" %>
<div class="SWUI_Panel_Title">
	Reset Password
</div>
<div class="SWUI_Panel_Body">
	<div style="width: 100%;">
		<asp:PasswordRecovery ID="PasswordRecovery1" runat="server" Width="100%">
			<UserNameTemplate>
				<table class="SWUI_Table" style="width:100%;">
					<tr>
						<td class="SWUI_Table_Label">
							<asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Your Email:</asp:Label>
						</td>
						<td class="SWUI_Table_Value SWUI_100w">
							<asp:TextBox ID="UserName" runat="server" CssClass="SWUI_Prg Login_TextBox SWUI_100w" />
						</td>
						<td class="SWUI_Table_Check">
							<div runat="server" id="LastNameStatus" class="SWUI_Table_Result0">
							</div>
						</td>
					</tr>
					<tr>
						<td></td>
						<td class="SWUI_Table_Value">
							<asp:LinkButton ID="ResetButton" CssClass="SWUI_Table_Button SWUI_Btn" runat="server" CommandName="Reset"
								Text="Reset" ValidationGroup="PasswordRecovery1" OnClick="ResetButton_Click" />
						</td>
						<td></td>
					</tr>
				</table>
				<asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
					ErrorMessage="Your email is required." ValidationGroup="PasswordRecovery1" ForeColor="Black"
					Display="Dynamic">
								<div class="SWUI_Table_ErrorPanel"><b>Your Email</b> is required.</div>
				</asp:RequiredFieldValidator>
			</UserNameTemplate>
		</asp:PasswordRecovery>
	</div>
	<asp:Panel runat="server" ID="ErrorPanel" CssClass="SWUI_Table_ErrorPanel">
		<asp:Label runat="server" ID="ErrorLabel" />
	</asp:Panel>
	<asp:Panel runat="server" ID="ResetPasswordSuccessPanel" Visible="false" CssClass="SWUI_Table_SuccessPanel">
		Password reset instructions were sent to your e-mail address.<br />
		If the email doesn't show up in your inbox, check your SPAM folder.
	</asp:Panel>
	<pre runat="server" id="PasswordResetSubject" visible="false">Reset your {Host} password"</pre>
	<pre runat="server" id="PasswordResetBody" visible="false">
Hello, {UserName}:

We received your request to reset your password. To confirm your request and reset your password, please follow the instructions below. Confirming your request helps prevent unauthorized access to your account.

If you didn't request that your password be reset, please ignore this email.

CONFIRM REQUEST AND RESET PASSWORD

1. Copy the following web address:

{ResetKey}

IMPORTANT: Because fraudulent ("phishing") e-mail often uses misleading links, we recommend that you do not click links in e-mail, but instead copy and paste them into your browsers, as described above.

2. Open your web browser, paste the link in the address bar, and then press ENTER.

3. Follow the instructions on the web page that opens.

Thank you,
IT Helpdesk

NOTE: Please do not reply to this message, which was sent from an unmonitored e-mail address. Mail sent to this address cannot be answered.
</pre>
	<asp:Label CssClass="SWUI_Table_ErrorPanel" runat="server" ID="ResetPasswordUserNotFoundLabel"
		Visible="false" />
</div>
