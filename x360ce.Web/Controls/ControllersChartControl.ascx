<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ControllersChartControl.ascx.cs" Inherits="x360ce.Web.Controls.ControllersChartControl" %>
<asp:chart id="Chart1" runat="server" Height="296px" Width="774px" Palette="BrightPastel" imagetype="Png" BorderDashStyle="Solid" BackSecondaryColor="255, 191, 0" BackGradientStyle="TopBottom" BorderWidth="2" backcolor="255, 191, 0" BorderColor="26, 59, 105" BackImageTransparentColor="255, 191, 0" BorderlineColor="255, 191, 0">
	<Titles>
		<asp:Title Text="Controllers in Database (Monthly)" />
	</Titles>
	<legends>
		<asp:Legend IsTextAutoFit="False" Name="Default" BackColor="Transparent" Font="Trebuchet MS, 8.25pt, style=Bold"></asp:Legend>
	</legends>
	<borderskin PageColor="Transparent"></borderskin>
	<series>
		<asp:Series Name="Controllers" BorderColor="180, 26, 59, 105">
		</asp:Series>
	</series>
	<chartareas>
		<asp:ChartArea Name="ChartArea1" BorderColor="64, 64, 64, 64" BorderDashStyle="Solid" BackSecondaryColor="White" BackColor="64, 165, 191, 228" ShadowColor="Transparent" BackGradientStyle="TopBottom">
			<area3dstyle Rotation="10" perspective="10" Inclination="15" IsRightAngleAxes="False" wallwidth="0" IsClustered="False"></area3dstyle>
			<axisy linecolor="64, 64, 64, 64">
				<labelstyle font="Trebuchet MS, 8.25pt, style=Bold" />
				<majorgrid linecolor="64, 64, 64, 64" />
			</axisy>
			<axisx linecolor="64, 64, 64, 64">
				<labelstyle font="Trebuchet MS, 8.25pt, style=Bold" />
				<majorgrid linecolor="64, 64, 64, 64" />
			</axisx>
		</asp:ChartArea>
	</chartareas>
</asp:chart>