<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LoginUser.ascx.cs" Inherits="x360ce.Web.Security.Controls.LoginUser" %>
<div runat="server" id="LoginPanel" visible='<%# x360ce.Web.Security.SecurityContext.Current.AllowUsersToLogin %>'>
	<div class="Login_Title">
		Login</div>
	<div class="Login_Body">
		<asp:Login ID="Login1" runat="server" Width="100%" OnLoggedIn="Login1_LoggedIn" 
			onloginerror="Login1_LoginError">
			<LayoutTemplate>
				<center>
					<table border="0" cellpadding="0" cellspacing="4" class="BA_FormTable">
						<tr>
							<td class="BA_FormTable_Label">
								<asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Your Email:</asp:Label>
							</td>
							<td class="BA_FormTable_Value">
								<asp:TextBox ID="UserName" runat="server" />
							</td>
							<td class="BA_FormTable_Check">
								<div runat="server" id="Div1" class="BA_FormTable_Result0">
								</div>
							</td>
						</tr>
						<tr>
							<td class="BA_FormTable_Label">
								<asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password">Password:</asp:Label>
							</td>
							<td class="BA_FormTable_Value">
								<asp:TextBox ID="Password" runat="server" TextMode="Password" />
							</td>
							<td class="BA_FormTable_Check">
								<div runat="server" id="LastNameStatus" class="BA_FormTable_Result0">
								</div>
							</td>
						</tr>
						<tr>
							<td class="BA_FormTable_Label">
								Remember:
							</td>
							<td>
								<table border="0" cellpadding="0" cellspacing="0" style="width: 100%;">
									<tr>
										<td align="left" valign="middle" style="padding-top: 3px;" class="BA_FormTable_Value">
											<input type="checkbox" id="RememberMe" runat="server" class="checkbox"/>
										</td>
										<td align="right">
											<asp:Button ID="LoginButton" runat="server" CommandName="Login" CssClass="SWUI_Prg Login_Button"
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
				<div class="BA_FormTable_ErrorPanel" runat="server" id="LoginErrorPanel" visible="false">
					<asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
				</div>
				<asp:ValidationSummary runat="server" ID="ValidationSummary1" ValidationGroup="ctl03$Login1"
					CssClass="BA_FormTable_ErrorPanel" />
				<div style="display: none;">
					<asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
						ErrorMessage="User Name is required" ValidationGroup="ctl03$Login1" Display="Static"><b>Username</b> is required.</asp:RequiredFieldValidator>
					<asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password"
						ErrorMessage="Password is required" ValidationGroup="ctl03$Login1" Display="Static"><b>Your Email</b> is required.</asp:RequiredFieldValidator>
				</div>
			</LayoutTemplate>
		</asp:Login>
	</div>
	<asp:PlaceHolder runat="server" ID="ScriptPlaceHolder" Visible='<%# AutoFocus && !IsPostBack %>'>
		<script type="text/javascript">
			function LoginUser_Load(source, e) {
				var control = document.getElementById('<%=FindControl("Login1$UserName").ClientID %>');
				control.focus();
			}
			Sys.UI.DomEvent.addHandler(window, 'load', window.setTimeout( function(){ LoginUser_Load(this, null); }, 100));
		</script>	
	</asp:PlaceHolder>
</div>
