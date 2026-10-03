<%@ Control Language="C#" AutoEventWireup="true" Inherits=" JocysCom.Web.Security.Controls.RoleEdit"
	Codebehind="RoleEdit.ascx.cs" %>
<table cellpadding="0" border="0" cellspacing="0">
	<tr>
		<td>
			<asp:Panel runat="server" ID="UserPanel"  CssClass="SWUI_Security_Header">
				Role
				<asp:Label ID="RoleNameLabel" runat="server"></asp:Label>
				Properties:</asp:Panel>
		</td>
	</tr>
	<tr>
		<td>
			<table border="0" cellpadding="0" cellspacing="4" class="SWUI_Security_UserEditTable"
				style="background-color: #e0e0e0;">
				<tr style="display: none;">
					<td>
						Provider Name</td>
					<td>
						<asp:TextBox ID="ProviderNameTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
				</tr>
				<tr>
					<td>
						Application Id</td>
					<td >
						<asp:TextBox ID="ApplicationIdTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
				</tr>
				<tr>
					<td>
						Role Id (GUID)</td>
					<td >
						<asp:TextBox ID="RoleIdTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
				</tr>
				<tr>
					<td>
						Role Name</td>
					<td >
						<asp:TextBox ID="RoleNameTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
				</tr>
				<tr>
					<td>
						Description</td>
					<td >
						<asp:TextBox ID="DescriptionTextBox" runat="server" Columns="43" CssClass="SWUI_Prg_TbxLeft" Rows="3" TextMode="MultiLine"></asp:TextBox></td>
				</tr>
			</table>
		</td>
	</tr>
	<tr>
		<td>
			<table cellpadding="0" cellspacing="0" border="0" style="width: 100%;">
				<tr>
					<td style="padding: 4px; text-align: left;">
						<span style="display: none;">
						<asp:Label ID="CreateStatusLabel" runat="server" Text="[CreateStatus]"></asp:Label><asp:TextBox
							ID="CreatedRoleTextBox" runat="server"></asp:TextBox></span></td>
					<td style="padding: 4px; text-align: right;" valign="top">
						<button class="SWUI_Prg SWUI_Btn" runat="server" id="CreateRoleButton" onserverclick="CreateRoleButton_Click"
							type="button" style="width: 82px;">
							<div>
								<img id="CreateRoleImage" runat="server" alt="Create" src="../Images/Icons/Role-Create-16x16.png" />Create</div>
						</button>
						<button class="SWUI_Prg SWUI_Btn" runat="server" id="UpdateRoleButton" onserverclick="UpdateRoleButton_Click"
							type="button" style="width: 82px;">
							<div>
								<img id="UpdateRoleImage" runat="server" alt="Update" src="../Images/Icons/Role-Update-16x16.png" />Update</div>
						</button>
					</td>
				</tr>
			</table>
		</td>
	</tr>
</table>
