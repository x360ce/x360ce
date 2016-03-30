<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ResetPassword.ascx.cs"
	Inherits="JocysCom.Web.Security.Controls.ResetPassword" %>
<div class="SWUI_Panel_Title">
	Reset Your Password</div>
<div class="SWUI_Panel_Body">
	<asp:PasswordRecovery ID="PasswordRecovery1" runat="server" Width="100%">
		<UserNameTemplate>
			<center>
				<table class="SWUI_Table" border="0" cellpadding="0" cellspacing="4">
					<tr>
						<td class="SWUI_Table_Label">
							<asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Your Email:</asp:Label>
						</td>
						<td class="SWUI_Table_Value">
							<asp:TextBox ID="UserName" runat="server" CssClass="SWUI_Prg Login_TextBox"></asp:TextBox>
						</td>
						<td class="SWUI_Table_Check">
							<div runat="server" id="LastNameStatus" class="SWUI_Table_Result0">
							</div>
						</td>
					</tr>
					<tr>
						<td></td>
						<td class="SWUI_Table_Value">
							<asp:Button ID="ResetButton" CssClass="SWUI_Table_Button SWUI_Btn" runat="server" CommandName="Reset"
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
								<div class="SWUI_Table_ErrorPanel"><b>Your Email</b> is required.</div>
			</asp:RequiredFieldValidator>
		</UserNameTemplate>
	</asp:PasswordRecovery>
	<asp:Panel runat="server" ID="ResetPasswordSuccessPanel" Visible="false" class="SWUI_Table_SuccessPanel">
		Password reset instructions were sent to your e-mail address. If the email doesn't
		show up in your inbox, check your SPAM folder.
	</asp:Panel>
	<asp:Label CssClass="SWUI_Table_ErrorPanel" runat="server" ID="ResetPasswordUserNotFoundLabel"
		Visible="false" />
</div>
