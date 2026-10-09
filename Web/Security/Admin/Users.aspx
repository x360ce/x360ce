<%@ Page Language="C#" AutoEventWireup="true" Inherits="JocysCom.Web.Security.Admin.Users"
	Codebehind="Users.aspx.cs" %>

<%@ Register Src="../Controls/UserAdvancedEdit.ascx" TagName="UserAdvancedEdit" TagPrefix="uc5" %>
<%@ Register Src="../Controls/UserEdit.ascx" TagName="UserEdit" TagPrefix="uc4" %>
<%@ Register Src="../Controls/UserList.ascx" TagName="UserList" TagPrefix="uc3" %>
<%@ Register Src="../Controls/RoleEdit.ascx" TagName="RoleEdit" TagPrefix="uc2" %>
<%@ Register Src="../Controls/RoleList.ascx" TagName="RoleList" TagPrefix="uc1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" style="height: 100%">
<head runat="server">
	<title>Untitled Page</title>
	<meta name="GENERATOR" content="Microsoft Visual Studio .NET 7.1" />
	<meta name="CODE_LANGUAGE" content="C#" />
	<meta name="vs_defaultClientScript" content="JavaScript" />
	<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5" />
	<link type="text/css" rel="stylesheet" href="Styles/Security.css" />
	<!--[if IE]>
		<link type="text/css" rel="stylesheet" href="Styles/Security.IE.css" />
	<![endif]-->

	<link type="text/css" rel="stylesheet" href="../../Common/JsClasses/System.Web.UI.Interface.css" />

	<script type="text/javascript" language="javascript" src="../../Common/JsClasses/System.js"></script>

	<script type="text/javascript" language="javascript" src="../../Common/JsClasses/System.Web.js"></script>

	<script type="text/javascript" language="javascript" src="../../Common/JsClasses/System.Web.UI.HtmlControls.js"></script>

	<script type="text/javascript" language="javascript" src="../../Common/JsClasses/System.Web.UI.Interface.js"></script>

	<script type="text/javascript" language="javascript" src="../../Common/JsClasses/System.Security.Password.js"></script>

	<script type="text/javascript" language="javascript" src="Scripts/Security.PrincipalsSearch.js"></script>

	<style type="text/css">
		body { font-family: Verdana; font-size: 8pt; }
		table tr td { font-family: Verdana; font-size: 8pt;	}
		table tr th { font-family: Verdana; font-size: 8pt;	}
	</style>
</head>
<body style="height: 100%; padding: 0; margin: 0;">
	<form id="form1" runat="server" style="height: 100%">
		<uc3:UserList ID="UserList1" runat="server" />
			<div style="border-top: solid 1px #95b7f3;">
				<table cellpadding="0" cellspacing="0" border="0" style="width: 100%;">
					<tr>
						<td valign="top">
							<uc4:UserEdit ID="UserEdit1" runat="server" />
						</td>
						<td valign="top" style="width: 100%;">
							<uc1:RoleList ID="RoleList1" runat="server" />
						</td>
					</tr>
				</table>
			</div>
		<span style="display: none;">
			<asp:TextBox ID="SelectUserTextBox" runat="server"></asp:TextBox>
			<asp:Button ID="SelectUserButton" runat="server" OnClick="SelectUserButton_Click"
				Text="[SelectUserButton]" />
		</span>
	</form>
</body>
</html>
