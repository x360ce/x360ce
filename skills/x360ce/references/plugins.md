# Plugins

A plugin adds a device to x360ce: a controller that exists only in code, or one x360ce cannot read by itself, such as
a controller fed over the network or a Bluetooth device decoded in code. x360ce lists it as a **Plugin** device, and
it is mapped to a controller like any other.

A plugin is one .NET Framework DLL, built from **one `.cs` file**.

## Where plugins come from

- **Inside x360ce.** The Test Controller, a pretend controller that moves by itself, is part of x360ce.exe.
- **The plugins folder:** `Plugins` in the x360ce data folder, usually `C:\ProgramData\X360CE\Plugins`. Each plugin
  has a folder of its own there, holding the DLL named like the folder and any DLLs it needs:
  `Plugins\MyDevice\MyDevice.dll`. A plugin put there loads at once; x360ce watches the folder. **Open Folder** on the
  **Plugins** page of the **Options** tab opens it.

## Which plugins load

- A plugin inside x360ce always loads.
- A plugin signed with the same certificate as x360ce.exe loads by itself.
- Any other plugin, signed by someone else or not signed, waits until **Allow third-party plugins** is ticked on the
  **Plugins** page. A plugin runs inside x360ce with all its rights, so allow plugins only from people you trust.
  Ticking the box loads the waiting plugins at once; unticking it takes effect when x360ce starts again.

The **Plugins** page lists every plugin found: the devices it adds, whether it loaded, who signed it, and why it did not
load. The **Issues** tab says so too. A plugin that throws an error is switched off until x360ce starts again.

## Use a plugin device

1. Open a controller tab and press **Add**. The list offers one device of each kind a loaded plugin makes, until one
   has been added.
2. Select it and press **Add Selected Device**. x360ce adds the device and maps it to the controller.
3. For another device of a kind already added, press **Add Plugin Device** on the **Devices** tab, then map it the
   same way.

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
   DLL; read one with `GetType().Assembly.GetManifestResourceStream("MyDevice.Resources.<file name>")`.
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
  plugin, to read the name and the counts. Keep the constructor light: start work, such as opening a socket, on the
  first `Read`.
- **`Read` must return at once.** It is called on x360ce's input thread, up to a thousand times a second. Anything
  slow, such as a network or Bluetooth read, belongs on a thread of the plugin's own, which keeps the newest values for
  `Read` to copy. Make nothing in `Read` that the garbage collector must clean up.
- **Let go of what you open.** A class that also implements `IDisposable` is disposed when its device is removed and
  when x360ce closes.
- **Explain the device.** The class's `[Description("...")]` is its help, Markdown allowed. x360ce shows it at the top
  of the device's tab, as plain text, and to AI agents with the device's controls.
- **Errors switch it off.** An exception from any member switches that kind of device off until x360ce starts again;
  the Plugins page and the Issues tab say why. A control of its own that cannot be made says why on its tab instead,
  and the device keeps working.
- **.NET Framework 4.6.2, C# 7.3.** The build settings in `Directory.Build.props` set both. A plugin can use anything in
  .NET Framework, and other DLLs it brings in its own folder; x360ce looks for a plugin's DLLs only there, so the DLLs
  of one plugin never stand in for another's.

### Settings, an MCP server, and a control of its own

A device can have a tab of its own on the controller page it is mapped to, named after it. The device's help heads the
tab, and under it a page for each part the plugin has: its own control first, then **MCP Server**, then **Properties**.

- **Settings, on the Properties page: the class's public properties.** They are shown as rows of a property grid, as Visual Studio's
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
  page, made with the instance it reads; the control's `Text` names the page, or it is called Control. The control runs
  on the interface thread while `Read` runs on the input thread, so a value the two share must be safe to read from
  both, such as a `volatile` field.

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
stick by hand. The Remote MCP Controller shows an MCP server, and `State`, which sets its controls.

### Sign a plugin

A plugin signed with the same certificate as x360ce.exe loads without **Allow third-party plugins**. Sign the DLL with
`signtool sign` after building it. Plugins of your own can stay unsigned: tick **Allow third-party plugins** for them.

## For AI agents

- `plugins_list` (MCP, or `x360ce.exe -Ai=plugins_list`) lists every plugin found, its state and why, the devices it
  adds and those added, and each plugin's documentation.
- The skill holds one document for each plugin, under `references/plugins/`; `x360ce.exe -Skill=plugins/TestController`
  prints one.
- `devices_list` and `device_map` work on plugin devices as on any device.
- `devices_list` lists each plugin device's `Tools`, and `device_call` calls one by the device's `InstanceGuid`:
  `device_call` with `tool=read` returns its settings and their values, and `tool=set_state` with `value` sets one, at
  the Configure level. These are the tools a device's own MCP server serves, reached through x360ce's door, so a program
  allowed in there can drive every plugin device without opening another port.
- A device's document lists the settings on its tab. With the device selected on its controller page, `ui_read` and
  `ui_set` reach the tab's grid rows like any other control: each row's path ends with its property's name, such as
  `.../SettingsPropertyGrid/State`.
