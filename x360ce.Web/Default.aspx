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

        td { font-family: Calibri, 'Trebuchet MS', Arial; font-size: 14px; border: 0; vertical-align: top; padding: 0; }

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

        .Description td, p { color: #111111; }
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

                        <table style="width: 800px;">
                            <tr>
                                <td colspan="2" style="background-color: #2674ec; padding: 10px; text-align: center; border-radius: 14px;">

                                    <p style="font-size: 18pt; vertical-align: middle; margin-top: 0; margin-bottom: 12px; color: #80ceff;">TocaEdit Xbox 360 Controller Emulator</p>

                                    <table style="border-collapse: separate; border-spacing: 4px;">
                                        <tr>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce">Source (GitHub.com)</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce/blob/master/Wiki/CompatibilityList.md">Supported Games</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="https://github.com/x360ce/x360ce/issues">Issues</a></td>
                                            <td style="vertical-align: bottom;"><a target="_blank" class="GitHub" href="http://ngemu.com/forums/x360ce.140">Forum</a></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce.zip">Download x360ce 3.2.9.81 (2015-10-04) • for 32-bit games</a></td>
                                        </tr>
                                        <tr>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td></td>
                                            <td style="vertical-align: bottom;"><a class="GitHub" href="Files/x360ce_x64.zip">Download x360ce 3.2.9.81 (2015-10-04) • for 64-bit games</a></td>
                                        </tr>
                                    </table>

                                    <p style="margin-top: 0; text-align: left; font-size: 9pt; margin-left: 35px; color: #80ceff;"><i>• Latest version works with “Grand Theft Auto V” (64-bit only).</i></p>

                                    <p style="text-align: justify; color: #80ceff;">“Xbox 360 Controller Emulator” allows your controller (gamepad, joystick, wheel, etc.) to function as an Xbox 360 controller. For example, it lets you play games such as “Grand Theft Auto”, “Mafia” or “Saints Row” using a Logitech Steering Wheel.</p>

                                </td>
                            </tr>
                        </table>

                        <table style="width: 800px; margin-top: 4px;">
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
                                <td style="background-color: #34963c; padding-bottom: 20px; border-radius: 14px; width: 396px;" class="Controllers">
                                    <p style="text-align: center; margin-top: 20px; color: #84e68c;">
                                        <img src="/Images/x360ce_Logo.png" />
                                    </p>

                                    <table style="background-color: #ffffff; margin-left: auto; margin: auto; border-radius: 20px 20px 20px 20px;">
                                        <tr>
                                            <td>
                                                <table style="margin: auto; border-spacing: 5px; border-collapse: separate; width: 100%;">
                                                    <tr>
                                                        <td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/gaming-controllers" target="_blank" title="Logitech G29 Driving Force Wheel and Shifter">
                                                            <img src="http://www.jocys.com/files/hardware/Logitech_G29_small.jpg" style="width: 127px; height: 100px; border-radius: 16px;" /></a></td>
                                                        <td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/product/g27-racing-wheel" target="_blank" title="Logitech G27 Racing Wheel">
                                                            <img src="http://www.jocys.com/files/hardware/Logitech_G27_small.jpg" style="width: 127px; height: 100px; border-radius: 16px;" /></a></td>
                                                        <td style="text-align: center;"><a href="http://gaming.logitech.com/en-gb/product/driving-force-gt-gaming-wheel" target="_blank" title="Logitech Driving Force GT">
                                                            <img src="http://www.jocys.com/files/hardware/Logitech_GT_small.jpg" style="width: 97px; height: 100px; border-radius: 16px;" /></a></td>
                                                    </tr>
                                                    <tr>
                                                        <td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/gaming-controllers" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 4px 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G29</a></td>
                                                        <td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/g27-racing-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech G27</a></td>
                                                        <td style="text-align: center; font-size: 8pt;"><a href="http://gaming.logitech.com/en-gb/product/driving-force-gt-gaming-wheel" style="width: 100%; background-color: #bfbfbf; color: #ffffff; border-radius: 4px 4px 4px 4px; padding-top: 0; padding-bottom: 0px; display: table;">Logitech GT</a></td>
                                                    </tr>
                                                    <tr>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/gp/search?ie=UTF8&camp=1789&creative=9325&index=aps&keywords=Logitech%20G29&linkCode=ur2&tag=x360cecom-20&linkId=3NLUP4FBHQ7EXHSH"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-na.amazon-adsystem.com/e/ir?t=x360cecom-20&l=ur2&o=1" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/gp/search?ie=UTF8&camp=1789&creative=9325&index=aps&keywords=Logitech%20G27&linkCode=ur2&tag=x360cecom-20&linkId=KTZ4CTSVPVUK44RL"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-na.amazon-adsystem.com/e/ir?t=x360cecom-20&l=ur2&o=1" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.com/gp/search?ie=UTF8&camp=1789&creative=9325&index=aps&keywords=Logitech%20GT&linkCode=ur2&tag=x360cecom-20&linkId=7IRDTCJ2IHR6VAHZ"><img src="/Images/Amazon.com.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-na.amazon-adsystem.com/e/ir?t=x360cecom-20&l=ur2&o=1" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20G29&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20G27&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.co.uk/gp/search?ie=UTF8&camp=1634&creative=6738&index=aps&keywords=Logitech%20GT&linkCode=ur2&tag=x360cecom-21"><img src="/Images/Amazon.co.uk.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-uk.amazon-adsystem.com/e/ir?t=x360cecom-21&l=ur2&o=2" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                    </tr>
                                                    <tr>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20G29&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20G27&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                        <td style="text-align: center;"><a class="ImageLink" target="_blank" href="http://www.amazon.de/gp/search?ie=UTF8&camp=1638&creative=6742&index=aps&keywords=Logitech%20GT&linkCode=ur2&tag=x360cecom0c-21"><img src="/Images/Amazon.de.jpg" style="width: 70px; height: 15px;" /></a><img src="http://ir-de.amazon-adsystem.com/e/ir?t=x360cecom0c-21&l=ur2&o=3" width="1" height="1" border="0" alt="" style="border: none !important; margin: 0px !important;" /></td>
                                                    </tr>
                                                </table>
                                            </td>
                                        </tr>
                                    </table>

                                    <p id="AdvertisingDisclosureShort" style="text-align: center; font-size: 7.2pt; margin: 0; margin-top: 2px; color: #84e68c; cursor: pointer;" onclick="this.style.display='none'; document.getElementById('AdvertisingDisclosure').style.display='block'";>The owner of this website is a participant in the Amazon EU Associates Programme...</p>
                                    <p onclick="this.style.display='none'; document.getElementById('AdvertisingDisclosureShort').style.display='block'" id="AdvertisingDisclosure" style="cursor: pointer; text-align: justify; padding-left: 32px; padding-right: 32px; font-size: 7.2pt; margin: 0; margin-top: 2px; color: #84e68c; display: none;">The owner of this website is a participant in the Amazon EU Associates and Amazon Services LLC Associates Program, an affiliate advertising program designed to provide a means for sites to earn advertising fees by advertising and linking to Amazon.com / Amazon.co.uk / Amazon.de / Amazon.es / Amazon.it / Amazon.fr</p>

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
                                <td class="Description" colspan="2" style="background-color: #ffbf00; padding: 10px; color: #111111; text-align: justify; border-radius: 14px; width: 804px;">

                                    <p><span class="title">System Requirements</span></p>

                                    <p style="margin-left: 20px;">• Windows Vista or newer.<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=22">.NET 3.5 (includes 2.0 and 3.0)</a> - included in Windows 7.<br /><span style="margin-left: 14px; font-size: 9pt;"><i>In Windows 8 and 10: <a  class="Blue" target="_blank" href="http://msdn.microsoft.com/en-us/library/hh506443.aspx">Control Panel &gt; Programs and Features &gt; Turn Windows features on or off > enable “.NET Framework 3.5 (includes 2.0 and 3.0)”</a>.</i></span><br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=48130">.NET 4.6 (includes 4.0)</a> - included in Windows 8 and 10.<br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=8109">DirectX End-User Runtime (June 2010)</a> - Required regardless of OS; <span style="font-size: 9pt;"><i>.NET MUST be installed prior to the DirectX update.</i></span><br />
                                    • <a target="_blank" class="Blue" href="http://www.microsoft.com/en-us/download/details.aspx?id=40784">Visual C++ Redistributable for Visual Studio 2013</a> - For x64 systems install both x86 and x64 redistributables.</p>
                                   
                                    <p><span class="title">Files</span></p>

                                    <p style="margin-left: 20px;">• xinput1_3.dll (Library) - Translates XInput calls to DirectInput calls - supports old, non-XInput compatible GamePads.<br />
                                    • x360ce.exe - (Application) - Allows for editing and testing of Library settings.<br />
                                    • x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).<br />
                                    • x360ce.gdb - (Game Database) Includes required hookmasks for various games).<br />
                                    • Dinput8.dll - (DirectInput 8 spoof/wrapping file to improve x360ce compatibility in rare cases).</p>

                                    <p><span class="title">Installation</span></p>

                                    <p>Run this program from the same directory as the game executable. XInput library files exist with several different names and some games require a change in its name. Known names:</p>

                                    <p style="margin-left: 20px;">• xinput1_4.dll<br />
                                    • xinput1_3.dll<br />
                                    • xinput1_2.dll<br />
                                    • xinput1_1.dll<br />
                                    • xinput9_1_0.dll</p>
                                    
                                    <p><span class="title">Uninstallation</span></p>

                                    <p>Delete x360ce.exe, x360ce.ini and all XInput DLLs from the game's executable directory.</p>

                                    <p><span class="title">Troubleshooting</span></p>
                                    
                                    <p>Some games have control issues, when Dead Zone is reduced to 0%.</p>

                                    <p>You may need to increase the Anti-Dead Zone value, if there is gap between the moment, when you start to push the axis related button, and the reaction in game.</p>

                                    <p>Some controllers will only operate in game, if they are set as “GamePad”. Try to:</p>

                                    <p style="margin-left: 20px;">1. Run x360ce.exe<br />
                                    2. Select [Controller #] tab page with your controller.<br />
                                    3. Open [Advanced] tab page.<br />
                                    4. Set "Device Type" drop down list value to: GamePad.<br />
                                    5. Click [Save] button.<br />
                                    6. Close x360ce Application, run game.</p>

                                    <p>Only one controller, mapped to PAD1, may work correctly in some games. Try to:</p>
                                    
                                    <p style="margin-left: 20px;">1. Run x360ce.exe<br />
                                    2. Select the [Controller #] tab page corresponding to your controller.<br />
                                    3. Open the [Direct Input Device] tab page (visible when the controller is connected).<br />
                                    4. Set "Map To" drop down list value to: 1.<br />
                                    5. Set "Map To" drop down list values <i>(repeat steps 2. to 4.)</i> for other controllers, if you have them, to: 2, 3 or 4.<br />
                                    6. Click [Save] button.<br />
                                    7. Close x360ce Application, run game.</p>

                                    <p>To use more than one controller in game, you may need to combine them. Try to:</p>
                                    
                                    <p style="margin-left: 20px;">1. Run x360ce.exe<br />
                                    2. Select the [Controller #] tab page corresponding to your additional controller.<br />
                                    3. Open the [Advanced] tab page.<br />
                                    4. Set "Combine Into" drop down list value to: One.<br />
                                    5. Select [Options] tab page.<br />
                                    6. Check "Enable Combining" check-box. <span style="font-size: 9pt;"><i>(Note: Uncheck "Enable Combining" check-box, when you want to configure the controller.)</i></span><br />
                                    7. Click [Save] button.<br />
                                    8. Close x360ce Application, run game.</p>

                                   <p>The x360ce.exe application can be closed before launching the game; the game doesn't need it and it uses your computer's resources. The x360ce.exe application is just a GUI for editing x360ce.ini and testing your controller.</p>

                                   <p>If [Controller #] tab page light won't turn green / Red light on [Controller #] tab page:</p>

                                    <p style="margin-left: 20px;">• The controller profile loaded may match the name of your controller, but not actually be for the controller you own.<br />
                                    • There just might not be a profile for your control at all. The light should turn green once the 2 sticks, triggers and D-pad are assigned. Sometimes x360ce.exe application needs to be restarted, after assigning these, for the light to turn green.<br />
                                    • The controller profile might have PassThrough (check-box) enabled.<br />
                                    • The DInput state of the controller might be incorrect due to an application crashing previously and not unloading the controller or some other reason. Opening up Joy.cpl (Set Up USB Game Controllers) and clicking the [Advanced] button, and then Okaying out of the window, that appears, can fix it.<br />
                                    </p>
                                    
                                    <p>If you have questions about installation or configuration, please go to our <a target="_blank" class="Blue" href="http://ngemu.com/forums/x360ce.140">NGemu x360ce forum</a></p>
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
