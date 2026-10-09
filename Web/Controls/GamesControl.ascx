<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="GamesControl.ascx.cs" Inherits="x360ce.Web.Controls.GamesControl" %>
<asp:ListView ID="GamesListView" runat="server">
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
            <td><%# Eval("FileProductName") %></td>
        </tr>
    </ItemTemplate>
</asp:ListView>


