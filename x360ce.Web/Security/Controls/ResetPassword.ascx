<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ResetPassword.ascx.cs"
	Inherits="x360ce.Web.Security.Controls.ResetPassword" %>
<div class="Login_Title">
	Reset Your Password</div>
<div class="Login_Body">
	<asp:PasswordRecovery ID="PasswordRecovery1" runat="server" Width="100%">
		<UserNameTemplate>
			<center>
				<table class="BA_FormTable" border="0" cellpadding="0" cellspacing="4">
					<tr>
						<td class="BA_FormTable_Label">
							<asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Your Email:</asp:Label>
						</td>
						<td class="BA_FormTable_Value">
							<asp:TextBox ID="UserName" runat="server" CssClass="SWUI_Prg Login_TextBox"></asp:TextBox>
						</td>
						<td class="BA_FormTable_Check">
							<div runat="server" id="LastNameStatus" class="BA_FormTable_Result0">
							</div>
						</td>
					</tr>
					<tr>
						<td align="right" colspan="2">
							<asp:Button ID="ResetButton" CssClass="SWUI_Prg Login_Button" runat="server" CommandName="Reset"
								Text="Reset" ValidationGroup="PasswordRecovery1" OnClick="ResetButton_Click" />
						</td>
						<td>
						</td>
					</tr>
				</table>
			</center>
			<asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
				ErrorMessage="Your email is required." ValidationGroup="PasswordRecovery1" ForeColor="Black"
				Display="Dynamic">
								<div class="BA_FormTable_ErrorPanel"><b>Your Email</b> is required.</div>
			</asp:RequiredFieldValidator>
		</UserNameTemplate>
	</asp:PasswordRecovery>
	<asp:Panel runat="server" ID="ResetPasswordSuccessPanel" Visible="false" class="BA_FormTable_SuccessPanel">
		Password reset instructions were sent to your e-mail address. If the email doesn't
		show up in your inbox, check your SPAM folder.
	</asp:Panel>
	<asp:Label CssClass="BA_FormTable_ErrorPanel" runat="server" ID="ResetPasswordUserNotFoundLabel"
		Visible="false" />
</div>
