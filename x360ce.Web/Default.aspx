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
        body { margin: 0; background-color: #dddddd; }

        table { margin: auto; border: 0; padding: 0; border-collapse: collapse; border-spacing: 0; }

        td { color: #ffffff; font-family: Calibri, 'Trebuchet MS', Arial; font-size: 14px; border: 0; vertical-align: top; padding: 0; }

        .title { font-size: 18px; }

        img { vertical-align: middle; }

        a { text-decoration: none; color: #ffffff; }

            a:hover { background-color: #6ebcfb; }

                a:hover.ImageLink { background-color: #ffffff; }

            a.Blue { color: #003399; }
            a:hover.Blue { color: #003399; background-color: transparent; }

        .GitHub { background-color: #53a1f6; border-radius: 4px; padding-left: 8px; padding-right: 8px; padding-top: 0; padding-bottom: 1px; display: inline-block; }

        .GitHubBlack { display: block; border-radius: 0 0 0 0; background-color: #000000; padding-left: 3px; padding-right: 3px; padding-top: 1px; padding-bottom: 2px; color: #ffffff; font-size: 6pt; }

        .Controllers td, p { color: #84e68c; }

        .Games td, p { color: #ff7373; }

        .Description td, p { color: #80ceff; }
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
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce">Source (GitHub.com)</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce/blob/master/Wiki/CompatibilityList.md">Supported Games</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce/issues">Issues</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="http://code.google.com/p/x360ce/downloads/list?can=1&q=+%09XBOX+360+Controller+Emulator&colspec=Filename+Summary+Uploaded+ReleaseDate+Size+DownloadCount">Downloads</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce.zip">Download x360ce 3.1.7.58 (2015-07-07) • for 32-bit games</a></td>
					                    </tr>
                                        <tr>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce_x64.zip">Download x360ce 3.1.7.58 (2015-07-07) • for 64-bit games</a></td>
					                    </tr>
                                    </table>

<p style="margin-top: 0; text-align: left; font-size: 9pt; margin-left: 35px;"><i>• Latest version works with “Grand Theft Auto V” (64-bit only).</i></p>

                                    <p style="text-align: justify; color: #80ceff;">“Xbox 360 Controller Emulator” allows your controller (GamePad, Joystick, Wheel, ...) to function like “Xbox 360 Controller” and to play games, like “Grand Theft Auto”, “Mafia” or “Saints Row”, with Logitech Steering Wheel. Also, application allows you to edit and test “Xbox 360 Controller Emulator Library” settings.</p>

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
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/gaming-controllers" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 4px 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G29</a></td>
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/g27-racing-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G27</a></td>
						<td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/driving-force-gt-gaming-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 4px 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech GT</a></td>
					</tr>
                	<tr>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/s/?field-keywords=Logitech+G29"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 6px;" /></a></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/s/?field-keywords=Logitech+G27"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 0;" /></a></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/s/?field-keywords=Logitech+GT"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px; border-radius: 0 0 6px 0;" /></a></td>
				</tr>
				<tr>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20G29&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 6px;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20G27&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 0;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20GT&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px; border-radius: 0 0 6px 0;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
				</tr>
				<tr>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20G29&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 6px;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20G27&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px; border-radius: 0 0 0 0;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
					<td style="text-align: center; font-size: 8pt;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20GT&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px; border-radius: 0 0 6px 0;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
				</tr>
			</table>
		</td>
		</tr>
</table>

 <p id="AdvertisingDisclosureShort" style="text-align: center; font-size: 7.2pt; margin: 0; margin-top: 2px; color: #84e68c; cursor: pointer;" onclick="this.style.display='none'; document.getElementById('AdvertisingDisclosure').style.display='block'";>The owner of this website is a participant in the Amazon EU Associates Programme...</p>
 <p onclick="this.style.display='none'; document.getElementById('AdvertisingDisclosureShort').style.display='block'" id="AdvertisingDisclosure" style="cursor: pointer; text-align: justify; padding-left: 32px; padding-right: 32px; font-size: 7.2pt; margin: 0; margin-top: 2px; color: #84e68c; display: none;">The owner of this website is a participant in the Amazon EU Associates Programme, an affiliate advertising programme designed to provide a means for sites to earn advertising fees by advertising and linking to Amazon.co.uk / Amazon.de / Amazon.es / Amazon.it / Amazon.fr</p>

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
                                    <span class="title">Compatibility</span><br />
                                    <br />
                                    <a target="_blank" class="Blue" href="https://github.com/x360ce/x360ce/blob/master/Wiki/CompatibilityList.md">List of games</a> that work with x360ce.<br />
                                    <br />
                                    <span class="title">System Requirements</span><br />
                                    <br />
                                    • Windows Vista or newer.<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=21">.NET 3.5 (also installs 2.0 and 3.0)</a> (included in Windows 7)<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=30653">.NET 4.0 (link to 4.5, also installs 4.0)</a> (included with Windows 8)<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=35">DirectX End-User Runtimes (June 2010)</a> (Required regardless of OS)<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=40784">Visual C++ Redistributable for Visual Studio 2013</a> (For x64 systems install both x86 and x64 redistributables)<br />
                                    <br />
                                    <span class="title">Files</span><br />
                                    <br />
                                    • xinput1_3.dll (Library) - Translates XInput calls to DirectInput calls, for support old, no XInput compatible GamePads.<br />
                                    • x360ce.exe - (Application) - Allows edit and test Library settings.<br />
                                    • x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).<br />
                                    • x360ce.gdb - (Game Database) Includes required hookmasks for various games).<br />
                                    • Dinput8.dll - (DirectInput 8 spoof/wrapping file to improve x360ce compatibility in rare cases).<br />
                                    <br />
                                    <span class="title">Installation</span><br />
                                    <br />
                                    Run this program from the same directory as the game executable. Xinput library files exist with several different names and some games require a change in its name. Known names:<br />
                                    <br />
                                    • xinput1_4.dll<br />
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
                                    Some games will operate only, when controller is considered as GamePad, even if it is Steering Wheel. Try to:<br />
                                    <br />
                                    1. Run x360ce.exe<br />
                                    2. Select tab with your Wheel Controller.<br />
                                    3. Open [Advanced] tab page.<br />
                                    4. Set "Device Type" drop down list value to: GamePad<br />
                                    5. Click [Save] button.<br />
                                    6. Close x360ce Application, run game.<br />
                                    <br />
                                    Only one controller may work correctly in some games. If you have more than one controller, connected to your PC, you must set it as PAD1 (other controllers must be set as PAD2, PAD3 or PAD4) in x360ce.ini file. You must edit x360ce.ini file with Notepad each time, after closing x360ce.exe application:<br />
                                    <br />
                                    <span style="font-family: 'Courier New'">[Mappings]<br />
                                        PAD1=IG_{main controller number}<br />
                                        PAD2=IG_{other controller number}<br />
                                        PAD3=IG_{other controller number}<br />
                                        PAD4=<br />
                                    </span>
                                    <br />
                                    Some games have control issues, when Dead Zone is reduced to 0%.<br />
                                    <br />
                                    x360ce.exe application can be closed during the game - game doesn't need it and it uses computer resources. It is just a GUI for editing x360ce.ini and test controller. 
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
