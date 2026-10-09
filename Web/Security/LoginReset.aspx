<%@ Page Title="" Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true"
	CodeBehind="LoginReset.aspx.cs" Inherits="JocysCom.Web.Security.LoginReset" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeaderPlaceHolder" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="PageContentPlaceHolder" runat="server">
	<table class="MainTable">
		<tr>
			<td>
				<img alt="Find Twins Banner" src="Social/Images/FindTwinsBanner.gif" style="width: 240px; height: 48px;" />
				<div class="MainDiv">
					<asp:Label ID="ErrorLabel" runat="server" Text="" Visible="false" />
					<asp:Label ID="ResetKeyLabel" runat="server" Visible="false" />
					<asp:Panel runat="server" ID="SuccessPanel" Visible="false">
						<div class="Login_Title">
							Password changed!
						</div>
						<div class="Login_Body">
							<div runat="server" id="RedirectionPanel" visible="false">
								Redirecting to homepage...

										<script type="text/javascript">
											window.setTimeout(function () {
												document.location = '<%=Page.ResolveClientUrl("~/Social") %>';
											}, 4000);
										</script>

							</div>
						</div>
					</asp:Panel>
					<div runat="server" id="ChangePasswordPanel" visible="false">
						<div class="SWUI_Panel_Title">
							Change Your Password
						</div>
						<div class="SWUI_Panel_Body">
							<table border="0">
								<tr>
									<td>
										<table border="0">
											<tr>
												<td>
													<asp:Label ID="NewPasswordLabel" runat="server" AssociatedControlID="NewPassword">New Password:</asp:Label>
												</td>
												<td>
													<asp:TextBox ID="NewPassword" CssClass="SWUI_Prg Login_TextBox" runat="server" TextMode="Password"></asp:TextBox>
												</td>
												<td>
													<asp:RequiredFieldValidator ID="NewPasswordRequired" runat="server" ControlToValidate="NewPassword"
														ErrorMessage="New Password is required." ToolTip="New Password is required."
														ValidationGroup="ChangePassword1">*</asp:RequiredFieldValidator>
												</td>
											</tr>
											<tr>
												<td>
													<asp:Label ID="ConfirmNewPasswordLabel" runat="server" AssociatedControlID="ConfirmNewPassword">Confirm New Password:</asp:Label>
												</td>
												<td>
													<asp:TextBox ID="ConfirmNewPassword" CssClass="SWUI_Prg Login_TextBox" runat="server"
														TextMode="Password"></asp:TextBox>
												</td>
												<td>
													<asp:RequiredFieldValidator ID="ConfirmNewPasswordRequired" runat="server" ControlToValidate="ConfirmNewPassword"
														ErrorMessage="Confirm New Password is required." ToolTip="Confirm New Password is required."
														ValidationGroup="ChangePassword1">*</asp:RequiredFieldValidator>
												</td>
											</tr>
											<tr>
												<td>
													<asp:CompareValidator ID="NewPasswordCompare" runat="server" ControlToCompare="NewPassword"
														ControlToValidate="ConfirmNewPassword" Display="Dynamic" ErrorMessage="The Confirm New Password must match the New Password entry."
														ValidationGroup="ChangePassword1"></asp:CompareValidator>
												</td>
											</tr>
											<tr>
												<td colspan="3">
													<asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
												</td>
											</tr>
											<tr>
												<td>
													<asp:Button ID="ChangePasswordPushButton" runat="server" CommandName="ChangePassword"
														Text="Change" CssClass="SWUI_Prg Login_Button" ValidationGroup="ChangePassword1"
														OnClick="ChangePasswordPushButton_Click" />
												</td>
												<td>
													<asp:Button ID="CancelPushButton" runat="server" CssClass="SWUI_Prg Login_Button"
														CausesValidation="False" CommandName="Cancel" Text="Cancel" />
												</td>
												<td></td>
											</tr>
										</table>
									</td>
								</tr>
							</table>
						</div>
					</div>
				</div>
			</td>
		</tr>
	</table>
</asp:Content>
