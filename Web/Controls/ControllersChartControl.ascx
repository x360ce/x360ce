<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ControllersChartControl.ascx.cs" Inherits="x360ce.Web.Controls.ControllersChartControl" %>
<%-- One hour of output cache. Beyond the procedure's own 12-hour table there is no other cache,
     so without this every request runs a query and draws the chart. Measured with
     sys.dm_exec_procedure_stats: ten page loads add zero executions. --%>
<%@ OutputCache Duration="3600" VaryByParam="none" %>
<asp:Literal ID="ChartSvg" runat="server" Mode="PassThrough" />
