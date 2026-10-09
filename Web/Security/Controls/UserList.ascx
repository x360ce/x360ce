<%@ Control Language="C#" AutoEventWireup="true" Inherits=" JocysCom.Web.Security.Controls.UserList"
	Codebehind="UserList.ascx.cs" %>
<asp:Panel runat="server" ID="MainPanel">
<table border="0" cellpadding="0" cellspacing="0" runat="server" id="RolesTable"
	style="width: 100%;">
	<tr>
		<td>
			<asp:Panel runat="server" ID="RolePanel"  CssClass="SWUI_Security_Header">
				<asp:Label ID="SearchFilterLabel" runat="server"></asp:Label> Members of '<asp:Label ID="ItemNameLabel" runat="server"></asp:Label>' Role:</asp:Panel>
			<asp:GridView ID="UsersGridView" runat="server" AutoGenerateColumns="False" DataSourceID="UsersEntityDataSource"
				AllowPaging="True" AllowSorting="True" Width="100%" CssClass="SWUI_GridViewTable"
				OnRowCommand="ItemsGridView_RowCommand">
				<Columns>
					<asp:TemplateField HeaderImageUrl="../Images/Icons/User-16x16.png">
						<ItemTemplate>
							<asp:ImageButton ID="ImageButton1" runat="server" ImageUrl="../Images/Icons/User-16x16.png" />
						</ItemTemplate>
						<ItemStyle Width="18px" />
					</asp:TemplateField>
					<asp:TemplateField HeaderImageUrl="../Images/Icons/Checks-16x16.png">
						<ItemTemplate>
							<asp:CheckBox ID="ItemIsSelected" runat="server" />
						</ItemTemplate>
						<ItemStyle Width="18px" />
					</asp:TemplateField>
					<asp:TemplateField HeaderText="Edit">
						<ItemTemplate>
							<asp:LinkButton ID="btnEditItem" runat="server" CommandArgument='<%# Eval("UserId") %>'
								CommandName="SelectItem">Edit</asp:LinkButton>
						</ItemTemplate>
					</asp:TemplateField>
					<asp:TemplateField HeaderText="Name">
						<ItemTemplate>
							<asp:Label ID="Label1" runat="server" Text='<%# GetUserName((Guid)Eval("UserId")) %>'></asp:Label>
						</ItemTemplate>
					</asp:TemplateField>
					<asp:CheckBoxField DataField="IsApproved" HeaderText="Approved" ReadOnly="True">
					<ItemStyle HorizontalAlign="Center" />
					</asp:CheckBoxField>
					<asp:BoundField DataField="CreateDate" 
						DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" HeaderText="Create Date" 
						HtmlEncode="False" SortExpression="CreateDate">
					<ItemStyle Wrap="False" />
					</asp:BoundField>
					<asp:BoundField DataField="LastLoginDate" 
						DataFormatString="{0:yyyy-MM-dd HH:mm:ss}" HeaderText="Last Login Date" 
						HtmlEncode="False" SortExpression="LastLoginDate">
					<ItemStyle ForeColor="DimGray" Wrap="False" />
					</asp:BoundField>
					<asp:BoundField DataField="Email" HeaderText="E-mail" 
						SortExpression="Email" />
					<asp:BoundField DataField="Comment" HeaderText="Comment" SortExpression="Comment">
						<ItemStyle ForeColor="DimGray" Width="100%" />
					</asp:BoundField>
				</Columns>
				<AlternatingRowStyle CssClass="SWUI_GridViewTableAltRow" />
			</asp:GridView>
		</td>
	</tr>
	<tr class="SWUI_Security_ItemsListRow">
		<td>
			<span>Enter the Object Names (Users) to Select:<br /></span>
			<asp:TextBox ID="ItemsTextBox" runat="server" TextMode="SingleLine"
				Style="width: 100%; margin-top: 4px;" CssClass="SWUI_Prg_TbxLeft"></asp:TextBox></td>
	</tr>
	<tr>
		<td id="NewDeleteCell" class="SWUI_Security_ButtonCell">
			<span style="display: none;">
				<asp:Label ID="MessageLabel" runat="server" Text="[MessageLabel]"></asp:Label>
				<asp:TextBox ID="ApplicationNameTextBox" runat="server" Columns="2">/</asp:TextBox>
				<asp:CheckBox ID="InvertCheckBox" runat="server" Checked="True" Text="Invert" />
				<asp:Button ID="RefreshListButton" runat="server" OnClick="RefreshListButton_Click"
					Text="Refresh List" />
				<asp:Button ID="DeleteSelectedButton" runat="server" OnClick="DeleteSelectedButton_Click"
					Text="Delete Selected" />
			</span>
			<button class="SWUI_Prg SWUI_Btn" runat="server" id="NewButton"
				type="button" style="width: 82px">
				<div>
					<img id="DeleteButtonImage" runat="server" alt="Create" src="../Images/Icons/User-Create-16x16.png" />New...</div>
			</button>
			<button class="SWUI_Prg SWUI_Btn" runat="server" id="DeleteButton"
				type="button" style="margin-left: 2px; width: 82px;">
				<div>
					<img id="CreateButtonImage" runat="server" alt="Create" src="../Images/Icons/User-Delete-16x16.png" />Delete</div>
			</button>
		</td>
	</tr>
	<tr>
		<td id="AddRemoveCell" class="SWUI_Security_ButtonCell" style="height: 27px">
			<span style="display: none;">
				<asp:Button ID="AddSelectedButton" runat="server" OnClick="AddSelectedButton_Click"
					Text="Add Selected" />
				<asp:Button ID="RemoveSelectedButton" runat="server" OnClick="RemoveSelectedButton_Click"
					Text="Remove Selected" />
			</span>
			<button class="SWUI_Prg SWUI_Btn" runat="server" id="AddButton"
				type="button" style="width: 82px">
				<div>
					<img id="AddUserImage" runat="server" alt="Add..." src="../Images/Icons/User-Add-16x16.png" />Add...</div>
			</button>
			<button class="SWUI_Prg SWUI_Btn" runat="server" id="RemoveButton"
				type="button" style="width: 82px;">
				<div>
					<img id="RemoveUserImage" runat="server" alt="Remove" src="../Images/Icons/User-Remove-16x16.png" />Remove</div>
			</button>
		</td>
	</tr>
</table>
</asp:Panel>
<asp:EntityDataSource ID="UsersEntityDataSource" runat="server" 
	ConnectionString="name=SecurityEntities" 
	DefaultContainerName="SecurityEntities" EnableFlattening="False" 
	EntitySetName="Memberships" EntityTypeFilter="Membership" 
	onselected="UsersEntityDataSource_Selected">
</asp:EntityDataSource>

