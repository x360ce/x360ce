<%@ Page Language="C#" AutoEventWireup="true" Inherits="JocysCom.Web.Security.Admin.Roles"
	CodeBehind="Roles.aspx.cs" %>

<%@ Register Src="../Controls/UserList.ascx" TagName="UserList" TagPrefix="uc3" %>
<%@ Register Src="../Controls/RoleEdit.ascx" TagName="RoleEdit" TagPrefix="uc2" %>
<%@ Register Src="../Controls/RoleList.ascx" TagName="RoleList" TagPrefix="uc1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title>Roles</title>
	<link type="text/css" rel="stylesheet" href="Styles/Security.css" />
	<!--[if IE]>
		<link type="text/css" rel="stylesheet" href="Styles/Security.IE.css" />
	<![endif]-->
	<link type="text/css" rel="stylesheet" href="../../Common/JsClasses/System.Web.UI.Interface.css" />
	<style type="text/css">
		body { font-family: Verdana; font-size: 8pt; }
		table tr td { font-family: Verdana; font-size: 8pt; }
		table tr th { font-family: Verdana; font-size: 8pt; }
	</style>
</head>
<body style="height: 100%; padding: 0; margin: 0;">
	<form id="form1" runat="server" style="height: 100%">
	<uc1:RoleList ID="RoleList1" runat="server" />
	<div style="border-top: solid 1px #95b7f3;">
		<table cellpadding="0" cellspacing="0" border="0" style="width: 100%;">
			<tr>
				<td valign="top">
					<uc2:RoleEdit ID="RoleEdit1" runat="server" />
				</td>
				<td valign="top" style="width: 100%;">
					<uc3:UserList ID="UserList1" runat="server" />
				</td>
			</tr>
		</table>
	</div>
	<span style="display: none;">
		<asp:TextBox ID="SelectRoleTextBox" runat="server"></asp:TextBox>
		<asp:Button ID="SelectRoleButton" runat="server" OnClick="SelectRoleButton_Click"
			Text="[SelectRoleButton]" />
	</span>
	</form>
</body>
</html>
