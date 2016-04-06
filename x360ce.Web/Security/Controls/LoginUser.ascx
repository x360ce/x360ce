<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LoginUser.ascx.cs" Inherits="JocysCom.Web.Security.Controls.LoginUser" %>
<div runat="server" id="LoginPanel">
	<div class="SWUI_Panel_Title">
		Login
	</div>
	<div class="SWUI_Panel_Body">
		<asp:Login ID="Login1" runat="server" Width="100%" OnLoggedIn="Login1_LoggedIn"
			OnLoginError="Login1_LoginError">
			<LayoutTemplate>
				<center>
					<table border="0" class="SWUI_Table">
						<tr>
							<td class="SWUI_Table_Label">
								<asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Your Email:</asp:Label>
							</td>
							<td class="SWUI_Table_Value">
								<asp:TextBox ID="UserName" runat="server" />
							</td>
							<td class="SWUI_Table_Check">
								<div runat="server" id="Div1" class="SWUI_Table_Result0">
								</div>
							</td>
						</tr>
						<tr>
							<td class="SWUI_Table_Label">
								<asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password">Password:</asp:Label>
							</td>
							<td class="SWUI_Table_Value">
								<asp:TextBox ID="Password" runat="server" TextMode="Password" />
							</td>
							<td class="SWUI_Table_Check">
								<div runat="server" id="LastNameStatus" class="SWUI_Table_Result0">
								</div>
							</td>
						</tr>
						<tr>
							<td class="SWUI_Table_Label">
								Remember:
							</td>
							<td>
								<table border="0" style="width: 100%;" cellpaddin="0" cellspacing ="0">
									<tr>
										<td style="padding-top: 3px;" class="SWUI_Table_Value">
											<asp:CheckBox ID="RememberMeCheckBox" runat="server" CssClass="checkbox"/>
										</td>
										<td>
											<asp:Button ID="LoginButton" runat="server" CommandName="Login" CssClass="SWUI_Table_Button SWUI_Btn"
												Text="Log In" ValidationGroup="ctl03$Login1" />
										</td>
									</tr>
								</table>
							</td>
							<td>
							</td>
						</tr>
					</table>
				</center>
				<asp:Panel CssClass="SWUI_Table_ErrorPanel" runat="server" ID="ErrorPanel">
					<asp:Label runat="server" ID="ErrorLabel" />
				</asp:Panel>
				<asp:ValidationSummary runat="server" ID="ValidationSummary1" ValidationGroup="ctl03$Login1"
					CssClass="SWUI_Table_ErrorPanel" />
				<div style="display: none;">
					<asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
						ErrorMessage="User Name is required" ValidationGroup="ctl03$Login1" Display="Static"><b>User name</b> is required.</asp:RequiredFieldValidator>
					<asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password"
						ErrorMessage="Password is required" ValidationGroup="ctl03$Login1" Display="Static"><b>Your Email</b> is required.</asp:RequiredFieldValidator>
				</div>
			</LayoutTemplate>
		</asp:Login>
	</div>
	<asp:PlaceHolder runat="server" ID="ScriptPlaceHolder">
		<script type="text/javascript">
			function LoginUser_Load(source, e) {
				var control = document.getElementById('<%=FindControl("Login1$UserName").ClientID %>');
				control.focus();
			}
			Sys.UI.DomEvent.addHandler(window, 'load', window.setTimeout(function () { LoginUser_Load(this, null); }, 100));
		</script>
	</asp:PlaceHolder>
</div>
