<%@ Page Title="" Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true"
	CodeBehind="Login.aspx.cs" Inherits="x360ce.Web.Security.Login" %>

<%@ Register Src="Controls/UserEdit.ascx" TagName="UserEditControl"
	TagPrefix="uc1" %>
<%@ Register Src="Controls/CreateUser.ascx" TagName="CreateUser" TagPrefix="uc" %>
<%@ Register Src="Controls/ResetPassword.ascx" TagName="ResetPassword" TagPrefix="uc" %>
<%@ Register Src="Controls/LoginUser.ascx" TagName="LoginUser" TagPrefix="uc" %>

<asp:Content ID="Header1" ContentPlaceHolderID="HeaderPlaceHolder" runat="server">
	<link type="text/css" rel="stylesheet" href="Common/JsClasses/System.Web.UI.Interface.css" />
	<link type="text/css" rel="stylesheet" href="Social/Styles/Facebook.css" />
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="PageContentPlaceHolder" runat="server">
	<asp:ScriptManagerProxy runat="server" ID="ScriptManagerProxy1">
		<Services>
			<asp:ServiceReference Path="~/WebServices/ServerInfo.asmx" />
		</Services>
	</asp:ScriptManagerProxy>

	<table class="MainTable" style="margin: auto;">
		<tr>
			<td>
				<img alt="" runat="server" id="LoginBanner" src='/Files/Website/Website_Management_System.gif' style="width: 240px; height: 48px; margin-bottom: 8px;" class="Login_Banner" /><%-- src='<%# string.Format("/App_Themes/Jocys/Images/{0}Banner.gif", AppContext.DomainBaseName) %>' --%>
				<div class="MainDiv">
					<asp:Panel runat="server" ID="AnonymousPlaceHolder" Visible='<%# !x360ce.Web.Security.SecurityContext.Current.IsAuthenticated %>'>
						<uc:LoginUser ID="LoginUser1" runat="server" AutoFocus="true" />
						<uc:ResetPassword ID="ResetPassword1" runat="server" />
						<div runat="server" id="RegisterPanel" visible='<%# x360ce.Web.Security.SecurityContext.Current.AllowUsersToRegister || HttpContext.Current.Request.IsLocal%>'>
							<uc:CreateUser ID="CreateUser1" runat="server" ShowBirthday="False" ShowGender="False"
								ShowTerms="false" ShowPromotions="false" ShowUsername="false" ShowNews="false" />
						</div>
					</asp:Panel>
					<asp:Panel runat="server" ID="LoggedInPlaceHolder" Visible='<%# x360ce.Web.Security.SecurityContext.Current.IsAuthenticated %>'>
						<div>
							<div class="Login_Title">Login</div>
							<div class="Login_Body" style="text-align: center">
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
