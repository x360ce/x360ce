<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ControllersChartControl.ascx.cs" Inherits="x360ce.Web.Controls.ControllersChartControl" %>
<asp:chart id="Chart1" runat="server" Height="296px" Width="774px"  Palette="SemiTransparent" imagetype="Png" BorderDashStyle="Solid" BackColor="255, 191, 0">
	<Titles>
		<asp:Title Text="Controllers in Database (Monthly)" Font="Trebuchet MS, 10.45pt" />
	</Titles>
	<legends>
		<asp:Legend IsTextAutoFit="False" Name="Default" BackColor="Transparent" Font="Trebuchet MS, 8.25pt"></asp:Legend>
	</legends>
	<borderskin PageColor="Transparent"></borderskin>
	<series>
		<asp:Series Name="Controllers" BorderColor="0, 0, 0, 0" Color="200, 38, 116, 236" IsVisibleInLegend="false" >
		</asp:Series>
	</series>
	<chartareas>
		<asp:ChartArea Name="ChartArea1" BorderDashStyle="Solid" BackColor="0, 255, 255, 255">
			<area3dstyle IsRightAngleAxes="True" IsClustered="False" LightStyle="Simplistic"></area3dstyle>
			<axisy>
				<labelstyle font="Trebuchet MS, 8.25pt" />
				<majorgrid linecolor="60, 0, 0, 0" />
			</axisy>
			<axisx TextOrientation="Auto" IntervalType="Months" IntervalOffsetType="Years">
				<labelstyle font="Trebuchet MS, 8.25pt" />
				<majorgrid />
			</axisx>
		</asp:ChartArea>
	</chartareas>
</asp:chart>