<%@ Page Title="" Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true"
	CodeBehind="Login.aspx.cs" Inherits="JocysCom.Web.Security.Login" %>

<%@ Register Src="Controls/LoginUser.ascx" TagName="LoginUser" TagPrefix="uc" %>
<%@ Register Src="Controls/ResetPassword.ascx" TagName="ResetPassword" TagPrefix="uc" %>
<%@ Register Src="Controls/CreateUser.ascx" TagName="CreateUser" TagPrefix="uc" %>

<asp:Content ID="Header1" ContentPlaceHolderID="HeaderPlaceHolder" runat="server">
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="PageContentPlaceHolder" runat="server">
	<table class="MainTable" style="margin: auto;">
		<tr>
			<td style="background-color: #3b5998; text-align: center;">
				<img alt="" runat="server" id="LoginBanner" src="Images/Website_Management_System.gif" style="width: 240px; height: 48px; margin-bottom: 8px;" class="Login_Banner" />
				<div class="MainDiv">
					<asp:Panel runat="server" ID="AnonymousPlaceHolder">
						<uc:LoginUser ID="LoginUser1" runat="server" AutoFocus="true" />
						<uc:ResetPassword ID="ResetPassword1" runat="server" />
						<div runat="server" id="RegisterPanel">
							<uc:CreateUser ID="CreateUser1" runat="server" ShowBirthday="False" ShowGender="False"
								ShowTerms="false" ShowPromotions="false" ShowUsername="True" ShowNews="false" />
						</div>
					</asp:Panel>
					<asp:Panel runat="server" ID="LoggedInPlaceHolder">
						<div>
							<div class="SWUI_Login_Title">Login</div>
							<div class="SWUI_Login_Body">
								<asp:LoginStatus ID="LoginStatus1" runat="server" />
								Logged in as
                                <asp:LoginName ID="LoginName1" runat="Server" />
							</div>
						</div>
					</asp:Panel>
				</div>
			</td>
		</tr>
	</table>

</asp:Content>
