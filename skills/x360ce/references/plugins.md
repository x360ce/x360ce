# Plugins

A plugin adds a device to x360ce: a controller that exists only in code, or one x360ce cannot read by itself, such as
a controller fed over the network or a Bluetooth device decoded in code. x360ce lists it as a **Plugin** device, and
it is mapped to a controller like any other.

A plugin is one .NET Framework DLL, built from **one `.cs` file**, which can take in other files of its folder.

## Where plugins come from

- **Inside x360ce.** The Test Controller, a pretend controller that moves by itself, the Remote MCP Controller, whose
  controls a program sets over MCP, the Fitness Machine, which reads an exercise bike, treadmill, rower or cross
  trainer over Bluetooth, chosen on its tab's Connection page, Wheel Telemetry, which feeds live vehicle telemetry
  from racing games into the wheel centering spring, and Wheel Telemetry Games, which helps games send their
  telemetry to Wheel Telemetry from a tab on the Games tab, are part of x360ce.exe.
- **The plugins folder:** `Plugins` in the x360ce data folder, usually `C:\ProgramData\X360CE\Plugins`. Each plugin
  has a folder of its own there, holding the DLL named like the folder and any DLLs it needs:
  `Plugins\MyDevice\MyDevice.dll`. A plugin put there loads at once; x360ce watches the folder. **Open Folder** on the
  **Plugins** page of the **Options** tab opens it.

## Which plugins load

- Built-in plugins embedded inside x360ce are enabled and load by default. Wheel Telemetry Games is off until its
  **Enabled** box is ticked: see [Wheel Telemetry Games](#wheel-telemetry-games).
- A plugin signed with the same certificate as x360ce.exe loads by itself.
- Any other plugin, signed by someone else or not signed, waits until **Allow third-party plugins** is ticked on the
  **Plugins** page. A plugin runs inside x360ce with all its rights, so allow plugins only from people you trust.
  Ticking the box loads the waiting plugins at once; unticking it takes effect when x360ce starts again.
- Each row on the **Plugins** page has an **Enabled** checkbox. Unticking a plugin immediately disables it (stopping its
  devices, telemetry listeners, MCP server and plugin UI such as the **Wheel Telemetry** controller tab) and keeps it
  unloaded on future starts until ticked again.

The **Plugins** page lists every plugin found: whether it is enabled, the devices or telemetry providers it adds, whether it loaded, who signed it, and why it did not
load. The **Issues** tab says so too for plugins that failed or wait for permission (intentionally disabled plugins are not reported as issues). A plugin that throws an error is switched off until x360ce starts again.

## Use a plugin device

1. Open a controller tab and press **Add**. The list offers one device of each kind a loaded plugin makes, until one
   has been added.
2. Select it and press **Add Selected Device**. x360ce adds the device and maps it to the controller.
3. For another device of a kind already added, press **Add Plugin Device** on the **Devices** tab, then map it the
   same way.

## Wheel Telemetry Games

Helps a game send its telemetry to x360ce's Wheel Telemetry plugin, so your wheel feels the road. Its games: GTA V (Legacy and Enhanced), Forza Horizon 6 and F1 25. Each game's own steps are on its page; these are the steps for every game.

**Read the warnings first.** A game's page starts with them. GTA V needs mods: play story mode only, switch BattlEye off in the game's launcher, and remove the mods before you play GTA Online. Rockstar can ban a player for mods, and x360ce cannot promise it will not.

1. On the Options tab, open the Plugins page.
2. Tick Enabled on the WheelTelemetry.Games row. It is off until you tick it.
3. On the Games tab, press Add... and choose the game's program file, or press Scan.
4. Select the game in the list.
5. Under the list, open the Wheel Telemetry Games tab. Its first page is named after the game.
6. Read the warnings at the top of the page.
7. Do the page's steps in order. For a mod, press Official page, download it, press Open game folder, and unzip it there. Its line says Found when it is there. x360ce does not include or download mods.
8. Where the page has Install, press it. It writes only the files it lists into the game's folder. Remove deletes them.
9. Leave Port at 37150, where x360ce's Wheel Telemetry plugin listens. A game set in its own menus is given the port in its steps.
10. Open the controller tab your wheel is mapped to, such as Controller 1. Its device list shows the wheel, ticked.
11. On its Wheel tab, tick Centering spring. x360ce uses a game's telemetry only while the centering spring is on, for a wheel with force feedback.
12. On its Game Telemetry tab, on the Telemetry page, keep Enable live game telemetry ticked and Listen port at 37150, the port the game sends to. Both are so at first.
13. Start the game and drive.
14. On the Game Telemetry tab, Live Telemetry shows the format, the speed and the packets a second, such as Forza Data Out - 72 km/h (60 Hz). It says Waiting for game telemetry... until packets arrive.

**When nothing arrives**

- The game and Listen port must name the same port. The line under the status says whether another program holds it.
- The game sends to 127.0.0.1, this computer, so no firewall rule is needed.
- Forza Horizon 6 from the Microsoft Store or the Xbox app needs the one command its page shows.
- F1 25 sends what x360ce reads with UDP Format 2025.
- GTA V: after the game is updated, Script Hook V works again only once its own update for that game version is out. Download it again. Script Hook V .NET writes ScriptHookVDotNet.log in the game's folder; an error about x360ce.Telemetry.3.cs there says why the script did not start.

**To remove everything**

1. Press Remove on the game's page.
2. Delete from the game's folder the files you unzipped there, such as ScriptHookV.dll, dinput8.dll or xinput1_4.dll, and Script Hook V .NET's files.
3. In a game set in its own menus, switch its telemetry off there.
4. On the Options tab's Plugins page, untick Enabled on the WheelTelemetry.Games row.

## Write a plugin

1. Install the .NET SDK 10 or newer (`dotnet --version`).
2. Write the plugin kit into a new folder:

   ```
   x360ce.exe /PluginKit=C:\MyPlugins
   ```

   It writes `x360ce.Plugins.dll` (the types a plugin implements), `Directory.Build.props` (the build settings),
   `Build-Plugins.ps1` (builds every plugin), `TestController\TestController.cs` (a sample to copy) and this help as
   `README.md`.
3. Copy the `TestController` folder, and give the folder and the `.cs` file in it the plugin's name:
   `MyDevice\MyDevice.cs`. Or start from the example below. Give the class a new name, a new `Name`, and a new
   `ProductGuid` (PowerShell: `[guid]::NewGuid()`). Files in a `Resources` folder beside the `.cs` file go inside the
   DLL; read one with `GetType().Assembly.GetManifestResourceStream("MyDevice.Resources.<file name>")`. A plugin too
   long for one file can be split: the first lines of `MyDevice.cs` name each other file of the folder, such as
   `#:include MyDevice.Page.cs`, and the build takes them in, as the Fitness Machine's four files are built.
4. Build every plugin in the folder:

   ```
   .\Build-Plugins.ps1
   ```

   Each becomes one file, `bin\MyDevice\MyDevice.dll`. To build one plugin alone:
   `dotnet build MyDevice\MyDevice.cs -o bin\MyDevice`.
5. Copy the `bin\MyDevice` folder into the plugins folder. It loads at once; tick **Allow third-party plugins** if it is
   not signed with the x360ce certificate.

### A whole plugin

```csharp
using System;
using System.ComponentModel;
using System.Diagnostics;
using x360ce.Plugins;

[Description("A controller whose X axis swings from side to side and whose button 1 blinks, once a second.")]
public class SwingController : IPluginDevice
{
	static readonly Guid Product = new Guid("3b4e2f8a-1c5d-4e6f-9a7b-8c9d0e1f2a3b");
	readonly Stopwatch _watch = Stopwatch.StartNew();

	public string Name { get { return "Swing Controller"; } }
	public Guid ProductGuid { get { return Product; } }
	public int AxisCount { get { return 1; } }
	public int SliderCount { get { return 0; } }
	public int PovCount { get { return 0; } }
	public int ButtonCount { get { return 1; } }

	public void Read(PluginState state)
	{
		var seconds = _watch.Elapsed.TotalSeconds;
		state.Axes[0] = (int)(PluginState.Middle + Math.Sin(seconds * 2 * Math.PI) * PluginState.Middle);
		state.Buttons[0] = seconds % 1 < 0.5;
	}
}
```

### The interface

A plugin implements `x360ce.Plugins.IPluginDevice`, in `x360ce.Plugins.dll`:

| Member | What x360ce reads from it |
|---|---|
| `string Name` | The device's name on the Devices tab and in the Add Plugin Device list. |
| `Guid ProductGuid` | Names the kind of device. Keep it the same in every version: saved mappings find the device by it. |
| `int AxisCount` | 0 to 6: X, Y, Z, X Rotation, Y Rotation, Z Rotation, in that order. |
| `int SliderCount` | 0 to 8. |
| `int PovCount` | 0 to 4 hat switches. |
| `int ButtonCount` | 0 to 128. |
| `void Read(PluginState state)` | Writes where the controls are now. |
| `void SetForce(int largeMotor, int smallMotor)` | Optional, from `IPluginForceFeedback`: takes the game's force. |
| `string Mapping` | Optional, from `IPluginMapping`: how the device is mapped, as a row of SDL's controller database. |
| `bool TryReadTelemetry(ref WheelTelemetry telemetry)` | Optional, from `IPluginWheelTelemetry`: feeds live vehicle speed, slip angle, grip, curb impulse and damping into the wheel centering spring. |

A device that also implements `x360ce.Plugins.IPluginForceFeedback` feels the game. x360ce calls its `SetForce` on the
input thread just before `Read`, whenever the force changes: each motor 0 to 65535, as the game asks for it, before the
tab's strengths, and 0 and 0 when it stops. **Enable** on the controller tab's **Force Feedback** page decides whether
the game's force reaches the device, as for any device; on several tabs it takes the strongest of each motor. Like
`Read`, `SetForce` must return at once. The Fitness Machine turns it into the machine's resistance or road grade.

A device that also implements `x360ce.Plugins.IPluginMapping` recommends how it is mapped, in the form of SDL's
controller database (`gamecontrollerdb.txt`). When the device is put on a controller tab, and when the tab's
**Auto Preset** is clicked, the tab is filled from it instead of from x360ce's guess, and the tab's **SDL** page shows
the row. `Mapping` holds the row's fields; x360ce adds the device's own GUID, name and platform. SDL's axes `a0`,
`a1`... count the device's axes in order, then its first two sliders; `b0` is button 1 and `h0` the first hat switch.
A whole axis on a trigger, `a0`, takes it from released at 0 to pressed all the way at 65535; `+a0` reads only the
axis's upper half. x360ce reads `Mapping` once, when it loads the plugin, and it is not a setting. The Fitness Machine
puts its forward speed on the right trigger and its reverse speed on the left:

```csharp
public string Mapping { get { return "righttrigger:a0,lefttrigger:a1"; } }
```

`PluginState`, the same object on every call, holding what was written last:

| Field | Values |
|---|---|
| `int[] Axes` (6) | 0 to 65535; `PluginState.Middle` (32767) is the middle, 0 is left or up. |
| `int[] Sliders` (8) | 0 to 65535. |
| `int[] Povs` (4) | `PluginState.PovReleased` (-1), or the direction in hundredths of a degree clockwise from up, 0 to 35999. |
| `bool[] Buttons` (128) | True while pressed. |

Rules:

- **One class is one kind of device.** x360ce finds every public class that implements `IPluginDevice` and has a
  public constructor without parameters. It makes one instance for each device added, and one more when it loads the
  plugin, to read the name, the counts and the mapping. Keep the constructor light: start work, such as opening a
  socket, on the first `Read`.
- **`Read` must return at once.** It is called on x360ce's input thread, up to a thousand times a second. Anything
  slow, such as a network or Bluetooth read, belongs on a thread of the plugin's own, which keeps the newest values for
  `Read` to copy. Make nothing in `Read` that the garbage collector must clean up.
- **x360ce reads a device only while it is ticked.** That is while it is ticked on the **Devices** tab and in the list
  of a controller tab of the game chosen. Unticked there, removed, or as x360ce closes, the device is let go of; ticked
  again, x360ce makes a new instance. So the first `Read` is the moment to connect to what the device reads, and
  `Dispose` the moment to let go of it.
- **Let go of what you open.** A class that also implements `IDisposable` is disposed when x360ce stops reading its
  device: when it is unticked or removed, and when x360ce closes. `Dispose` is called on the input thread, so it must
  not wait: it asks the plugin's own thread to let go.
- **Explain the device.** The class's `[Description("...")]` is its help, Markdown allowed. x360ce shows it on the
  **Help** page of the device's tab, as it shows its own help, and gives it to AI agents with the device's controls. A
  long help can be a `const string` the attribute names.
- **Errors switch it off.** An exception from any member switches that kind of device off until x360ce starts again;
  the Plugins page and the Issues tab say why. A control of its own that cannot be made says why on its tab instead,
  and the device keeps working.
- **Off until ticked, when it should be.** `[assembly: x360ce.Plugins.PluginOffByDefault]` in any file of the plugin
  keeps it off until the person ticks **Enabled** on the **Plugins** page: x360ce offers none of its devices and makes
  none of its classes until then. Wheel Telemetry Games, which can write into a game's folder, is off by default.
- **.NET Framework 4.6.2, C# 7.3.** The build settings in `Directory.Build.props` set both. A plugin can use anything in
  .NET Framework, and other DLLs it brings in its own folder; x360ce looks for a plugin's DLLs only there, so the DLLs
  of one plugin never stand in for another's.
- **Windows' own APIs, with no package.** Where Windows has them, the build settings also reference Windows' WinRT
  metadata in `%WINDIR%\System32\WinMetadata` (`Windows.Foundation`, `Windows.Devices` and `Windows.Storage`), so a
  plugin can use Bluetooth, sensors and Windows' other WinRT APIs. Such a plugin needs Windows 10 or later. It awaits
  a WinRT operation through a `TaskCompletionSource`, as the Fitness Machine does, because the library that awaits
  one directly needs the Windows SDK.

### A plugin for a game

A plugin can add a tab for a game to the **Games** tab instead of a device: a public class that implements
`x360ce.Plugins.IPluginGame`, and its page, a public Windows Forms control whose constructor takes the class.

| Member | What x360ce does with it |
|---|---|
| `string Name` | Names the tab, after x360ce's own **x360ce** page of the game. |
| `string[] FileNames` | The program files of the games it is for, such as `GTA5.exe`. Read each time a game is selected; the tab shows only for these. |
| `IPluginTelemetryData TelemetryData` | Set by x360ce before anything else: its telemetry formats and games, read only. |
| `string GamePath` | Set by x360ce before it makes the page: the selected game's file. |

The tab holds the page, named by its `Text`, then **Help**, the class's `[Description]`. Another game selected, the
plugin unticked or x360ce closing disposes both. The tab is as tall as x360ce's own page of the game: make a longer page
scroll itself, AutoScroll with its table docked to the top.

- **A plugin that helps a program send telemetry to the Wheel Telemetry plugin is named WheelTelemetry.{Name}.**
  Wheel Telemetry Games is `WheelTelemetry.Games`.
- **A new game is a row of data, not code.** x360ce keeps its telemetry games in its settings, `TelemetryGames.xml`
  and the files beside it, made from the `[x360ce]` tables of its database: a game's program file, the format it
  sends, its warnings and steps, the mods it needs with their official pages, and the files a plugin installs. Wheel
  Telemetry Games makes every game's page from those rows, and `TelemetryData` gives them to any plugin.
- **The formats and their official specifications** are x360ce's `TelemetryFormats.xml`, the formats the Wheel
  Telemetry plugin reads, by the ids it gives them, with a link to each publisher's specification where there is one.

### Settings, an MCP server, and a control of its own

A device can have a tab of its own on the controller page it is mapped to, named after it, with a page for each part
the plugin has: its own control first, then **MCP Server**, then **Properties**, and last **Help**, the device's help.

- **Settings, on the Properties page: the class's public properties**, but for those of the interfaces above. They are shown as rows of a property grid, as Visual Studio's
  Properties window shows a component's: grouped, or in A to Z order with the buttons above the grid, and the selected
  row's help under it. A `bool` or an `enum` is chosen from a list, a number or a `string` is typed. A property without
  a public setter is a status, greyed and read again once a second. Each device keeps its own values, saved with the
  other settings; a value kept for the device is bold. **Reset** on a row's right-click menu gives that row back the
  value a new device starts with, and the **Reset** button gives back every row's. x360ce sets the values on the thread
  that calls `Read`, just before a read, so they need no lock.
- **An MCP server of its own, on the MCP Server page: a public `PluginMcpServer` property.** The page shows the server's settings, with the
  same rows as AI assistant access on the Options tab's AI page: whether it is open, its level, address, port and token,
  and **Copy prompt** for an AI assistant. The server is closed until the person opens it there; an AI assistant cannot
  open it, nor read its token. While it is open, it serves this device's settings as tools: `read` returns every setting
  with its value, and a `set_` tool sets each one a person can change, such as `set_state` for `State`. `set_` tools
  need the Configure level. A caller reaches this device alone, never the rest of x360ce. x360ce chooses the port from
  its [port plan](Help.v4.md#ports): the controller and the place the device is mapped to, so the first device on
  controller 4 listens on 37141. A device on no controller has no port until it is mapped, or the person types one.

  ```csharp
  public PluginMcpServer McpServer { get; } = new PluginMcpServer();
  ```

  A server a plugin opens itself, such as a UDP one, takes a port in the plan's range for plugins: 37150 to 37199.

- **A control of its own: a Windows Forms control whose constructor takes the device.** x360ce places it on the first
  page, made with the instance it reads; the control's `Text` names the page, or it is called Control. While x360ce
  reads none, as while the device is unticked, the control is made with an instance of the tab's own: given the
  device's values, never read, and disposed with the tab. The control runs on the interface thread while `Read` runs
  on the input thread, so a value the two share must be safe to read from both, such as a `volatile` field. A setting
  the control changes is kept for the device when the class implements `INotifyPropertyChanged` and raises
  `PropertyChanged` with the setting's name, on the interface thread: the Fitness Machine's Connection page keeps the
  machine chosen so.

Attributes shape the settings:

| Attribute | What it does |
|---|---|
| `[DisplayName("Port")]` | The row's name. Without it, the property's name. |
| `[Description("...")]` | The help shown under the grid while the row is selected, and to AI agents. On an enum value, the list's text for it. |
| `[Category("Network")]` | The group the row is shown in. Without it, Settings, or Status for a status. |
| `[Range(1, 65535)]` | A number's limits, from `System.ComponentModel.DataAnnotations`. A value outside them is refused, and the grid says why. |
| `[Browsable(false)]` | Leaves a property off the tab. |
| `[TypeConverter(typeof(...))]` | A converter that offers standard values makes the row a list. Unless it says its values are the only ones, other values can be typed too. |

A controller fed over the network might choose the address it listens on as IIS bindings do:

```csharp
public class AddressConverter : StringConverter
{
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
	// Other addresses can be typed too.
	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		var addresses = Dns.GetHostAddresses(Dns.GetHostName())
			.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
			.Select(x => x.ToString());
		return new StandardValuesCollection(new[] { "All Unassigned" }.Concat(addresses).ToArray());
	}
}

[Category("Network"), DisplayName("IP address"), Description("The address to listen on.")]
[TypeConverter(typeof(AddressConverter))]
public string Address { get; set; } = "All Unassigned";

[Category("Network"), DisplayName("Port"), Description("The UDP port to listen on.")]
[Range(1, 65535)]
public int Port { get; set; } = 26760;

[Category("Network"), DisplayName("Packets received"), Description("Packets received since the device was added.")]
public int Packets { get { return _packets; } }
```

The Test Controller shows settings for its movement and its buttons, and `TestControllerPad`, a square that steers its
stick by hand. The Remote MCP Controller shows an MCP server, and `State`, which sets its controls. The Fitness
Machine shows `FitnessConnection`, its **Connection** page, which finds the machine and chooses it as the device's
`Machine` setting, and statuses that show what the machine sends. Draw a control of your own in the system colours
and lay it out with tables and automatic sizes, so it follows the theme, the font and the zoom, and give every
control a name: AI assistant access reaches a control by its name.

### Sign a plugin

A plugin signed with the same certificate as x360ce.exe loads without **Allow third-party plugins**. Sign the DLL with
`signtool sign` after building it. Plugins of your own can stay unsigned: tick **Allow third-party plugins** for them.

## For AI agents

- `plugins_list` (MCP, or `x360ce.exe -Ai=plugins_list`) lists every plugin found, its state and why, the devices it
  adds and those added, and each plugin's documentation.
- The skill holds one document for each plugin, under `references/plugins/`; `x360ce.exe -Skill=plugins/TestController`
  prints one.
- `devices_list` and `device_map` work on plugin devices as on any device.
- A plugin's own control is a page of its device's tab like any other: with the device selected on its controller page,
  `ui_pages` lists it, `ui_open` opens it, and `ui_read`, `ui_set` and `ui_invoke` reach its controls by their names,
  such as the Fitness Machine's `ScanButton` on its `ControlTabPage`, the Connection page. There, `ui_read` of
  `MachinesDataGridView` lists the machines heard as rows, `ui_set` with a row's index chooses one for the device, and
  `ui_read` of `ReadingsPanel` lists each value by its name, such as `SpeedValueLabel`. `ui_set` of the device's row's
  `IsEnabledColumn`, in the controller tab's `MappedDevicesDataGridView` or the Devices tab's `DevicesDataGridView`,
  connects it and lets it go.
- `devices_list` lists each plugin device's `Tools`, and `device_call` calls one by the device's `InstanceGuid`:
  `device_call` with `tool=read` returns its settings and their values, and `tool=set_state` with `value` sets one, at
  the Configure level. These are the tools a device's own MCP server serves, reached through x360ce's door, so a program
  allowed in there can drive every plugin device without opening another port.
- A device's document lists the settings on its tab. With the device selected on its controller page, `ui_read` and
  `ui_set` reach the tab's grid rows like any other control: each row's path ends with its property's name, such as
  `.../SettingsPropertyGrid/State`.
