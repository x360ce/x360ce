<%@ Control Language="C#" AutoEventWireup="true" Codebehind="UserAdvancedEdit.ascx.cs"
	Inherits="JocysCom.Web.Security.Controls.UserAdvancedEdit" %>
<asp:PlaceHolder runat="server" ID="MainPlaceHolder" Visible="false">
<asp:Panel runat="server" ID="UserPanel" Style="padding: 4px;
				background: url(../../Images/Interface/Office2003/FadeLightBlue-1x23.gif);">
				Employee <asp:Label ID="UserNameLabel" runat="server"></asp:Label> Properties:</asp:Panel>
<table border="0" cellpadding="0" cellspacing="4" width="100%" class="SWUI_Security_UserEditTable"
	style="background-color: #e0e0e0;">
	<tr>
		<td>
			Employee Id</td>
		<td>
			<asp:TextBox runat="server" ID="EmployeeIdTextBox" CssClass="SWUI_Prg_TbxLeft" ReadOnly="True" />
		</td>
		<td style="text-align: left; width: 100%;">
			<asp:CheckBox ID="RecordEnabledCheckBox" runat="server" CssClass="SWUI_CheckBox"
				Text="Enabled" /></td>
	</tr>
	<tr>
		<td>
			First Name</td>
		<td>
			<asp:TextBox ID="FirstNameTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
		<td>
		</td>
	</tr>
	<tr style="color: #000000">
		<td>
			Last Name</td>
		<td>
			<asp:TextBox ID="LastNameTextBox" runat="server" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
		<td>
		</td>
	</tr>
	<tr style="color: #000000">
		<td>
			Department /
			Level</td>
		<td colspan="2">
			<asp:DropDownList ID="DepartmentDropDownList" runat="server" CssClass="SWUI_Prg_TbxLeft"
				OnLoad="DepartmentDropDownList_Load">
			</asp:DropDownList>&nbsp;<asp:DropDownList ID="EmployeeLevelDropDownList" runat="server" CssClass="SWUI_Prg_TbxLeft">
				<asp:ListItem Value="1">Default</asp:ListItem>
				<asp:ListItem Value="2">Advanced</asp:ListItem>
				<asp:ListItem Value="3">Hi-Tech</asp:ListItem>
			</asp:DropDownList></td>
	</tr>
</table>
<asp:Label runat="server" ID="MessageLabel" ForeColor="red" Visible="false">[MessageLabel]</asp:Label>
</asp:PlaceHolder>