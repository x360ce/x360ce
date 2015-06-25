<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="x360ce.Web.Default" %>

<%@ Register Src="~/Controls/ControllersControl.ascx" TagPrefix="uc1" TagName="ControllersControl" %>
<%@ Register Src="~/Controls/GamesControl.ascx" TagPrefix="uc1" TagName="GamesControl" %>

<%@ Register Src="Controls/ControllersChartControl.ascx" TagName="ControllersChartControl" TagPrefix="uc2" %>

<!DOCTYPE html>
<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta charset="utf-8" />
    <title>Xbox 360 Controller Emulator</title>
	<link rel="shortcut icon" type="image/x-icon" href="/favicon.ico" />

    <style type="text/css">
        body {
            margin: 0;
            background-color: #dddddd;
        }

        table {
            margin: auto;
            border: 0;
            padding: 0;
            border-collapse: collapse;
            border-spacing: 0;
        }

        td {
            color: #ffffff;
            font-family: Calibri, 'Trebuchet MS', Arial;
            font-size: 14px;
            border: 0;
            vertical-align: top;
            padding: 0;
        }

        .title {
            font-size: 18px;
        }

        img {
            vertical-align: middle;
        }

        a {
            text-decoration: none;
            color: #ffffff;
        }


            a:hover {
                background-color: #6ebcfb;
            }

        .GitHub {
            background-color: #53a1f6;
            border-radius: 4px;
            padding-left: 8px;
            padding-right: 8px;
            padding-top: 0;
            padding-bottom: 1px;
            display: inline-block;
        }

        .GitHubBlack {
            display: block;
            border-radius: 0 0 0 0;
            background-color: #000000;
            padding-left: 3px;
            padding-right: 3px;
            padding-top: 1px;
            padding-bottom: 2px;
            color: #ffffff;
            font-size: 6pt;
        }

        .Controllers td, p {
            color: #84e68c;
        }

        .Games td, p {
            color: #ff7373;
        }

        .Description td, p {
            color: #80ceff;
        }
    </style>

</head>
<body>

<table style="position: absolute; z-index: 1; margin-left:0; margin-right:auto;">
<tr>
<td>
<a href="http://www.jocys.com/Software" target="_blank" class="GitHubBlack">World of Warcraft Text to Speech Addon and Monitor</a>
</td>
</tr>
</table>

    <div style="position: relative;">

        <img src="/Images/Background.jpg" style="position: absolute; top: 0; left: 0; z-index: -2; width: 100%; height: 100%;" />
        <img src="/Images/Background_1.png" style="position: absolute; top: 0; left: 0; z-index: -1;" />
        <img src="/Images/Background_2.png" style="position: absolute; top: 0; right: 0; z-index: -1" />
        <img src="/Images/Background_3.png" style="position: absolute; bottom: 0; left: 0; z-index: -1" />
        <img src="/Images/Background_4.png" style="position: absolute; bottom: 0; right: 0; z-index: -1" />

        <form id="form1" runat="server">

            <table>
                <tr>
                    <td style="padding: 4px;">

                        <table style="width: 804px;">
                            <tr>
                                <td colspan="2" style="background-color: #2674ec; padding: 10px; text-align: center; border-radius: 14px;">

                                    <p style="font-size: 18pt; vertical-align: middle; color: #80ceff; margin-top: 0; margin-bottom: 12px;">TocaEdit Xbox 360 Controller Emulator</p>

                                    <table style="border-collapse: separate; border-spacing: 4px;">
                                        <tr>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="https://github.com/x360ce/x360ce">Source (GitHub.com)</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="https://github.com/x360ce/x360ce/wiki">Wiki</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="https://github.com/x360ce/x360ce/issues">Issues</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="http://code.google.com/p/x360ce/downloads/list?can=1&q=+%09XBOX+360+Controller+Emulator&colspec=Filename+Summary+Uploaded+ReleaseDate+Size+DownloadCount">Downloads</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce.zip">Download x360ce 3.0.7.57 (2015-06-25) 32-bit</a></td>
					                    </tr>
                                        <tr>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce_x64.zip">Download x360ce 3.0.7.57 (2015-06-25) 64-bit</a></td>
					                    </tr>
                                    </table>

<p style="margin-top: 0; text-align: left; font-size: 9pt; margin-left: 35px;"><i>• 32-bit version is for 32-bit games and 64-bit version is for 64-bit games.<br />
• Latest version works with “Grand Theft Auto V”.</i></p>

                                    <p style="text-align: justify; color: #80ceff;">“Xbox 360 Controller Emulator” allows your controller (GamePad, Joystick, Wheel, ...) to function like “Xbox 360 Controller” and... for example, to play games, like “Grand Theft Auto”, “Mafia” or “Saints Row”, with Logitech Steering Wheel. Application allows edit and test “Xbox 360 Controller Emulator Library” settings.</p>

                                </td>
                            </tr>
                        </table>

                        <table style="width: 804px; margin-top: 4px;">
                            <tr>
                                <td style="width: 404px;">

                                    <table style="width: 400px; margin-left: 0;">
                                        <tr>
                                            <td style="padding-bottom: 4px;">
                                                <img src="/Images/x360ce_General_400px.png" style="width: 400px; height: 367px;" /></td>
                                        </tr>
                                        <tr>
                                            <td style="width: 400px; background-color: #ae1919; padding-bottom: 20px; border-radius: 14px;" class="Games">
                                                <p style="text-align: center; margin-top: 20px; color: #ff7373;">TOP GAMES</p>
                                                <uc1:GamesControl runat="server" ID="GamesControl" />
                                            </td>
                                        </tr>
                                    </table>

                                </td>
                                <td style="background-color: #34963c; padding-bottom: 20px; border-radius: 14px; width: 400px;" class="Controllers">
                                    <p style="text-align: center; margin-top: 20px; color: #84e68c;">
                                        <img src="/Images/x360ce_Logo.png" />
                                    </p>

<table style="background-color: #ffffff; margin-left: auto; margin: auto; border-radius: 20px 20px 20px 20px;">
		<tr>
			<td>
			<table style="margin: auto; border-spacing: 5px; border-collapse: separate; width: 100%;">
					<tr>
						<td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/gaming-controllers" target="_blank" title="Logitech G29 Driving Force Wheel and Shifter"><img src="http://www.jocys.com/files/hardware/Logitech_G29_small.jpg" style="width: 127px; height: 100px; border-radius: 16px;" /></a></td>
						<td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/product/g27-racing-wheel" target="_blank" title="Logitech G27 Racing Wheel"><img src="http://www.jocys.com/files/hardware/Logitech_G27_small.jpg" style="width: 127px; height: 100px; border-radius: 16px;" /></a></td>
						<td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/product/driving-force-gt-gaming-wheel" target="_blank" title="Logitech Driving Force GT"><img src="http://www.jocys.com/files/hardware/Logitech_GT_small.jpg" style="width: 97px; height: 100px; border-radius: 16px;" /></a></td>
					</tr>
					<tr>
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/gaming-controllers" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 4px 16px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G29 • €460</a></td>
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/g27-racing-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G27 • €200</a></td>
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/driving-force-gt-gaming-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 16px 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech GT • €120</a></td>
					</tr>
			</table>
			</td>
		</tr>
</table>










                                    <p style="text-align: center; margin-top: 20px; color: #84e68c;">TOP CONTROLLERS</p>
                                    <uc1:ControllersControl runat="server" ID="ControllersControl" />
                                </td>
                            </tr>
                        </table>
                        <table style="width: 804px; margin-top: 4px;">
                            <tr>
                                <td colspan="2" style="background-color: #ffbf00; padding-top: 15px; padding-bottom: 15px; padding-left: 10px; padding-right: 10px; color: #111111; text-align: center; border-radius: 14px; width: 804px;">
					<uc2:ControllersChartControl ID="ControllersChartControl1" runat="server" />
                                </td>
                            </tr>
                        </table>
                        <table style="width: 804px; margin-top: 4px;">
                            <tr>
                                <td colspan="2" style="background-color: #ffbf00; padding: 10px; color: #111111; text-align: justify; border-radius: 14px; width: 804px;">
                                    <span class="title">System Requirements</span><br />
                                    <br />
                                    • Windows XP SP3 and newer.<br />
                                    • DirectX End-User Runtimes (June 2010) (Required regardless of OS)<br />
                                    • Visual C++ Redistributable for Visual Studio 2012 Update 1 (Most titles use the x86 runtime whether your OS is 64bit or not)<br />
                                    • .NET 4.0 (link to 4.5, also installs 4.0)<br />
                                    <br />
                                    <span class="title">Files</span><br />
                                    <br />
                                    • xinput1_3.dll (Library) - Translates XInput calls to DirectInput calls, for support old, no XInput compatible GamePads.<br />
                                    • x360ce.exe - (Application) - Allows edit and test Library settings.<br />
                                    • x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).<br />
                                    <br />
                                    <span class="title">Installation</span><br />
                                    <br />
                                    Run this program from game executable directory. Xinput file occurs in several versions and some games require a change in its name. Known names:<br />
                                    <br />
                                    • xinput1_3.dll<br />
                                    • xinput1_2.dll<br />
                                    • xinput1_1.dll<br />
                                    • xinput9_1_0.dll<br />
                                    <br />
                                    <span class="title">Uninstallation</span><br />
                                    <br />
                                    Delete x360ce.exe, x360ce.ini and all xinput dll from game executable directory.<br />
                                    <br />
                                    <span class="title">Troubleshooting</span><br />
                                    <br />
                                    Wheel doesn't work in the game, but it works inside x360ce Application.<br />
                                    <br />
                                    Some games will only operate when the controller is considered to be the gamepad, even if it is the steering wheel. Try to:<br />
                                    <br />
                                    1. Run x360ce.exe<br />
                                    2. Select tab with your Wheel Controller.<br />
                                    3. Open [Advanced] tab page.<br />
                                    4. Set "Device Type" drop down list value to: GamePad<br />
                                    5. Click [Save] button.<br />
                                    6. Close x360ce Application, run game.<br />
                                    <br />
                                    How to reduce wheel dead zone (GTA, Mafia II, ...)?<br />
                                    <br />
                                    1. Run x360ce.exe<br />
                                    2. Select tab with your Wheel Controller.<br />
                                    3. Open [Advanced] tab page.<br />
                                    4. Select "Enabled (XInput, 80%)" from "AntiDeadZone" drop down in order to reduce dead zone by 80%.<br />
                                    5. Click [Save] button.<br />
                                    6. Close x360ce Application, run game.<br />
                                    <br />
                                    Note: Some games have control issues when the dead zone is reduced by 100%.<br />
                                    <br />
                                    Do I need to run x360ce Application during the game?<br />
                                    <br />
                                    No, You do not need. Close x360ce during the game, because the game does not need it, and the application uses computer resources. The application is just a GUI for editing the x360ce.ini and test controller. 
                                </td>
                            </tr>
                        </table>

                    </td>
                </tr>
            </table>
        </form>

    </div>
</body>
</html>
