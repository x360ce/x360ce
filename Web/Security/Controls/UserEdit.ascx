<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserEdit.ascx.cs" Inherits="JocysCom.Web.Security.Controls.UserEdit" %>
<table cellpadding="0" border="0" cellspacing="0">
	<tr>
		<td align="left">
			<asp:Panel runat="server" ID="UserPanel" CssClass="SWUI_Security_Header">
				User
				<asp:Label ID="UserNameLabel" runat="server"></asp:Label>
				Properties</asp:Panel>
		</td>
	</tr>
	<tr>
		<td>
			<table border="0" cellpadding="0" cellspacing="4" class="SWUI_Security_UserEditTable"
				style="background-color: #e0e0e0;">
				<tr style="display: none;">
					<td align="left">Provider Name </td>
					<td colspan="2" style="width: 100%;">
						<asp:TextBox ID="ProviderNameTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft" />
					</td>
				</tr>
				<tr runat="server" id="UserIdRow">
					<td align="left">User Id (GUID) </td>
					<td colspan="2">
						<asp:TextBox ID="UserIdTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox>
					</td>
				</tr>
				<tr>
					<td align="left">Login Username </td>
					<td>
						<asp:TextBox ID="UserNameTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox>
					</td>
					<td>&nbsp;</td>
				</tr>
				<tr>
					<td align="left">Login Password </td>
					<td>
						<asp:TextBox ID="PasswordTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft" TextMode="Password"></asp:TextBox>
					</td>
					<td style="text-align: left">
						<input id="GenerateButton" class="SWUI_Prg_TbxCenter" type="button" value="<" tabindex="100" />
						<input id="AdvancedButton" class="SWUI_Prg_TbxCenter" type="button" value="Advanced..."
							tabindex="100" />
					</td>
				</tr>
				<tr>
					<td align="left">Confirm Password </td>
					<td>
						<asp:TextBox ID="PasswordConfirmTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"
							TextMode="Password"></asp:TextBox>
					</td>
					<td style="text-align: left">
						<asp:CheckBox ID="ShowPasswordCheckBox" runat="server" Text="Show Password" CssClass="SWUI_CheckBox SWUI_Prg_Cbx13"
							AutoPostBack="True" OnCheckedChanged="ShowPasswordCheckBox_CheckedChanged" TabIndex="100" />
					</td>
				</tr>
				<tr>
					<td align="left">E-mail </td>
					<td colspan="2">
						<asp:TextBox ID="EmailTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft" />
					</td>
				</tr>
				<tr>
					<td align="left">Confirm E-mail </td>
					<td colspan="2">
						<asp:TextBox ID="ConfirmEmailTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft" />
					</td>
				</tr>
				<tr runat="server" id="PasswordQuestionRow">
					<td align="left">Password Question </td>
					<td>
						<asp:TextBox ID="PasswordQuestionTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox>
					</td>
					<td style="text-align: left;">
						<asp:CheckBox ID="IsLockedOutCheckBox" runat="server" Text="Locked Out" CssClass="SWUI_CheckBox" />
					</td>
				</tr>
				<tr runat="server" id="PasswordAnswerRow">
					<td align="left">Password Answer </td>
					<td>
						<asp:TextBox ID="PasswordAnswerTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox>
					</td>
					<td style="text-align: left">
						<asp:CheckBox ID="IsApprovedCheckBox" runat="server" Text="Approved" CssClass="SWUI_CheckBox" />
					</td>
				</tr>
				<tr runat="server" id="CommentRow">
					<td align="left">Comment </td>
					<td>
						<asp:TextBox ID="CommentTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox>
					</td>
					<td style="text-align: left">
						<asp:CheckBox ID="IsOnlineCheckBox" runat="server" Text="Online" CssClass="SWUI_CheckBox" />
					</td>
				</tr>
				<tr runat="server" id="CreateDateRow">
					<td align="left">Create Date </td>
					<td colspan="2">
						<asp:Label ID="CreateDateLabel" runat="server"></asp:Label>
					</td>
				</tr>
				<tr runat="server" id="LastLoginDateDateRow">
					<td align="left">Login Date </td>
					<td colspan="2">
						<asp:Label ID="LastLoginDateLabel" runat="server"></asp:Label>
					</td>
				</tr>
				<tr runat="server" id="LastActivityDateRow">
					<td align="left">Activity Date </td>
					<td colspan="2">
						<asp:Label ID="LastActivityDateLabel" runat="server"></asp:Label>
					</td>
				</tr>
				<tr runat="server" id="LastPasswordChangeDateRow">
					<td align="left">Password Date </td>
					<td colspan="2">
						<asp:Label ID="LastPasswordChangeLablel" runat="server"></asp:Label>
					</td>
				</tr>
				<tr runat="server" id="LastLockoutDateDateRow">
					<td align="left">Lockout Date </td>
					<td colspan="2">
						<asp:Label ID="LastLockoutDateLabel" runat="server"></asp:Label>
					</td>
				</tr>
			</table>
		</td>
	</tr>
	<tr>
		<td>
			<table cellpadding="0" cellspacing="0" border="0" style="width: 100%;">
				<tr>
					<td style="padding: 4px; text-align: left;">
						<asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="UsernameTextBox"
							ErrorMessage="FirstNameValidator" Display="Dynamic" ValidationGroup="UserValidationGroup">Username can&#39;t be empty; </asp:RequiredFieldValidator>
						<asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="PasswordTextBox"
							ErrorMessage="FirstNameValidator" Display="Dynamic" ValidationGroup="UserValidationGroup">Password can&#39;t be empty; </asp:RequiredFieldValidator>
						<asp:CompareValidator ID="CompareValidator1" runat="server" ControlToCompare="PasswordTextBox"
							ControlToValidate="PasswordConfirmTextBox" ErrorMessage="CompareValidator" Display="Dynamic"
							ValidationGroup="UserValidationGroup">Passwords doesn't match; </asp:CompareValidator>
						<br />
						<asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ControlToValidate="EmailTextBox"
							ErrorMessage="E-mail must be valid; " ValidationExpression="^[a-zA-Z0-9_%-]+(|([\.][a-zA-Z0-9_%-]+)+)@[a-zA-Z0-9_%-]+(|([\.][a-zA-Z0-9_%-]+)+)[\.](([0-9]{1,3})|([a-zA-Z]{2,3})|(aero|coop|info|museum|name))$"
							Display="Dynamic" ValidationGroup="UserValidationGroup"></asp:RegularExpressionValidator>
						<asp:CompareValidator ID="CompareValidator2" runat="server" ControlToCompare="EmailTextBox"
							ControlToValidate="ConfirmEmailTextBox" ErrorMessage="CompareValidator" Display="Dynamic"
							ValidationGroup="UserValidationGroup">E-mails doesn&#39;t match; </asp:CompareValidator>
						<asp:Label ID="CreateStatusLabel" runat="server" Text="[CreateStatus]"></asp:Label>
						<span style="display: none;">
							<asp:TextBox ID="CreatedUserTextBox" runat="server"></asp:TextBox></span>
					</td>
					<td style="padding: 4px; text-align: right;" valign="top">
						<asp:Button ID="CreateUserButton" runat="server" Text="Create" 
							class="SWUI_Prg SWUI_Btn"
							onclick="CreateUserButton_Click"  />
						<button class="SWUI_Prg SWUI_Btn" runat="server" id="UpdateUserButton" onserverclick="UpdateUserButton_Click"
							type="button" style="width: 82px;">
							<div>
								<img id="UpdateUserImage" runat="server" alt="Update" src="../Images/Icons/User-Update-16x16.png" />Update</div>
						</button>
					</td>
				</tr>
			</table>
		</td>
	</tr>
</table>
