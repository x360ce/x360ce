<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="x360ce.Web.Default" %>

<%@ Register Src="~/Controls/ControllersControl.ascx" TagPrefix="uc1" TagName="ControllersControl" %>
<%@ Register Src="~/Controls/GamesControl.ascx" TagPrefix="uc1" TagName="GamesControl" %>

<!DOCTYPE html>
<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta charset="utf-8" />
    <title></title>
    <style type="text/css">
        body {
            margin: 0;
            background-color: #dddddd;
        }

        table {
            margin: auto;
            border: 0;
            border-spacing: 0;
            padding: 0;
        }

        td {
            color: #ffffff;
            font-family: Calibri, 'Trebuchet MS', Arial;
            font-size: 11pt;
            border: 0;
            vertical-align: top;
        }

        .title {
            font-size: 14pt;
        }

        img {
            vertical-align: middle;
        }

        a {
            text-decoration: none;
            color: #ffffff;
        }

            a:hover {
                color: #80ceff;
            }
        .Controllers td, p { color: #84e68c;
        }
        .Games td, p { color: #ff7373;
        }
        .Description td, p {
            color: #80ceff;
        }

    </style>

</head>
<body>

<div style="position: relative;">

<img src="/Images/Background_1.png" style="position:absolute; top:0; left:0; z-index: -1;" />
<img src="/Images/Background_2.png" style="position:absolute; top:0; right:0; z-index: -1" />
<img src="/Images/Background_3.png" style="position:absolute; bottom:0; left:0; z-index: -1" />
<img src="/Images/Background_4.png" style="position:absolute; bottom: 0; right:0; z-index: -1" />
<img src="/Images/Background.jpg" style="position:absolute; top: 0; left:0; z-index: -2; width: 100%; height: 100%;" />

    <form id="form1" runat="server">
        <table style="border-spacing: 4px; width: 802px;">
            <tr>
                <td colspan="2" style="background-color: #2674ec; padding: 10px; text-align: center; border-radius: 14px;">

                    <table>
                        <tr>
                            <td style="font-size: 18pt; padding-right: 20px; vertical-align: middle; color: #80ceff;">TocaEdit Xbox 360 Controller Emulator 2.1.2.190 ∙∙∙ <a href="http://code.google.com/p/x360ce/downloads/list">DOWNLOAD</a></td>
                            <td style="display: none;"><img src="http://www.jocys.com/Files/Website/Button_Download.png" style="width: 132px; height: 29px; border-radius: 0px; padding: 1px; background-color: #dddddd;" /></td>
                        </tr>
                    </table>

                    <p style="text-align: justify; font-size: 11pt; color: #80ceff;">“Xbox 360 Controller Emulator” allows your controller (GamePad, Joystick, Wheel, ...) to function like “Xbox 360 Controller”. For example, it allows you to play games like “Grand Theft Auto” (GTA) or “Mafia II” with Logitech Steering Wheel. Application allows edit and test “Xbox 360 Controller Emulator Library” settings.</p>
                </td>
            </tr>

            <tr>
                <td style="background-color: #8ef096; padding: 0px; width: 400px;">
                    <img src="http://localhost.jocys.com/projects/x360ce/Images/x360ce_General_400px.png" style="width: 400px; height: auto;" /></td>
                <td style="background-color: #34963c; padding: 10px; width: 400px; border-radius: 14px;" rowspan="2" class="Controllers">
                    <p style="text-align: center; margin-top: 10px; color: #84e68c;">TOP CONTROLLERS</p>
                    <uc1:ControllersControl runat="server" ID="ControllersControl" />
                </td>
            </tr>
            <tr>
                <td style="background-color: #ae1919; padding: 10px; padding-bottom: 20px; border-radius: 14px;" class="Games">
                    <p style="text-align: center; margin-top: 10px; color: #ff7373;">TOP GAMES</p>
                    <uc1:GamesControl runat="server" ID="GamesControl" />
                </td>
            </tr>
            <tr>
                <td colspan="2" style="background-color: #ffbf00; padding: 10px; color: #111111; text-align: justify; font-size: 11pt; border-radius: 14px;">
                    <span class="title">System Requirements</span><br />
                    <br />
                    • Windows XP SP3 and newer.<br />
                    • DirectX End-User Runtimes (June 2010) (Required regardless of OS)<br />
                    • Visual C++ Redistributable for Visual Studio 2012 Update 1 (Most titles use the x86 runtime whether your OS is 64bit or not)<br />
                    • .NET 4.0 (link to 4.5, also installs 4.0)<br />
                    <br />
                    <span class="title">Introduction</span><br />
                    <br />
                    "Xbox 360 Controller Emulator" files:<br />
                    <br />
                    xinput1_3.dll (Library) - Wrapper library that translates the XInput calls to DirectInput calls, for support old, no XInput compatible GamePads.<br />
                    <br />
                    x360ce.exe - (Application) - Allows edit and test Library settings.<br />
                    <br />
                    x360ce.ini - (Configuration) - Contain Library settings (button, axis, slider maps).<br />
                    <br />
                    <span class="title">Installation</span><br />
                    <br />
                    Run this program from the directory the game executable. Xinput file occurs in several versions and some games require a change in its name. Know names:<br />
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
    </form>

</div>
</body>
</html>
