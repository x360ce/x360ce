<%@ Control Language="C#" AutoEventWireup="true" Inherits=" JocysCom.Web.Security.Controls.RoleList"
	CodeBehind="RoleList.ascx.cs" %>
<%@ Register Src="RoleEdit.ascx" TagName="RoleEdit" TagPrefix="uc1" %>
<asp:Panel runat="server" ID="MainPanel">
	<table border="0" cellpadding="0" cellspacing="0" runat="server" id="RolesTable"
		style="width: 100%;">
		<tr>
			<td>
				<asp:Panel runat="server" ID="UserPanel" CssClass="SWUI_Security_Header">
					User '<asp:Label ID="ItemNameLabel" runat="server"></asp:Label>' is
					
					<asp:Label ID="SearchFilterLabel" runat="server"></asp:Label>
					Member Of:</asp:Panel>
				<asp:Label ID="SearchUserId" runat="server" Visible="false"></asp:Label>
				<asp:GridView ID="RolesGridView" runat="server" AutoGenerateColumns="False" DataKeyNames="ApplicationId,LoweredRoleName"
					DataSourceID="RolesDataSource" OnRowCommand="RolesGridView_RowCommand" AllowPaging="True"
					CssClass="SWUI_GridViewTable" Width="100%">
					<Columns>
						<asp:TemplateField HeaderImageUrl="../Images/Icons/Role-16x16.png">
							<ItemTemplate>
								<asp:Image ID="Image1" runat="server" ImageUrl="../Images/Icons/Role-16x16.png" />
							</ItemTemplate>
						</asp:TemplateField>
						<asp:TemplateField HeaderImageUrl="../Images/Icons/Checks-16x16.png">
							<ItemTemplate>
								<asp:CheckBox ID="CheckBox1" runat="server" />
							</ItemTemplate>
							<ItemStyle Wrap="False" />
						</asp:TemplateField>
						<asp:TemplateField HeaderText="Edit">
							<ItemTemplate>
								<asp:LinkButton ID="btnEditItem" runat="server" CommandArgument='<%# Eval("RoleId") %>'
									CommandName="SelectItem">Edit</asp:LinkButton>
							</ItemTemplate>
						</asp:TemplateField>
						<asp:BoundField DataField="RoleName" HeaderText="Role" SortExpression="RoleName" />
						<asp:BoundField DataField="Description" HeaderText="Description" SortExpression="Description">
							<ItemStyle ForeColor="DimGray" Width="100%" />
						</asp:BoundField>
					</Columns>
					<AlternatingRowStyle CssClass="SWUI_GridViewTableAltRow" />
				</asp:GridView>
			</td>
		</tr>
		<tr class="SWUI_Security_ItemsListRow">
			<td><span>Enter the Object Names (Roles) to Select:<br />
			</span>
				<asp:TextBox ID="ItemsTextBox" runat="server" TextMode="SingleLine" Style="width: 100%;
					margin-top: 4px;" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
		</tr>
		<tr>
			<td id="NewDeleteCell" class="SWUI_Security_ButtonCell"><span style="display: none;">
				<asp:Label ID="MessageLabel" runat="server" Text="[MessageLabel]"></asp:Label>
				<asp:TextBox ID="ApplicationNameTextBox" runat="server" Columns="2">/</asp:TextBox>
				<asp:CheckBox ID="InvertCheckBox" runat="server" Checked="True" Text="Invert" />
			</span></td>
		</tr>
		<tr>
			<td id="AddRemoveCell" class="SWUI_Security_ButtonCell">
				<button class="SWUI_Prg SWUI_Btn" runat="server" id="AddButton" onserverclick="AddRoleButton_Click"
					type="button" style="width: 82px">
					<div>
						<img id="AddRoleImage" runat="server" alt="Add..." src="../Images/Icons/Role-Add-16x16.png" />Add...</div>
				</button>
			</td>
		</tr>
	</table>
</asp:Panel>
<asp:EntityDataSource ID="RolesDataSource" runat="server" ConnectionString="name=SecurityEntities"
	DefaultContainerName="SecurityEntities" EnableFlattening="False" EntitySetName="Roles"
	EntityTypeFilter="Role" OnQueryCreated="RolesDataSource_QueryCreated">
</asp:EntityDataSource>
