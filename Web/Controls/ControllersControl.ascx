<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ControllersControl.ascx.cs" Inherits="x360ce.Web.Controls.ControllersControl" %>
<asp:ListView ID="ControllersListView" runat="server">
    <LayoutTemplate>
        <table id="itemPlaceholderContainer" runat="server">
            <tr runat="server" id="itemPlaceholder"></tr>
        </table>
    </LayoutTemplate>
    <EmptyDataTemplate>
        <div>No data was returned.</div>
    </EmptyDataTemplate>
    <ItemTemplate>
        <tr>
            <td style="padding-right: 4px; text-align: right; white-space: nowrap"><%# ((int)Eval("InstanceCount")).ToString("0,00") %> - </td>
            <td><%# CropText(Eval("ProductName"), 48) %></td>
        </tr>
    </ItemTemplate>
</asp:ListView>
