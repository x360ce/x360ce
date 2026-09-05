# x360ce Architecture & Engineering Standards

## 1. System Overview

**Xbox 360 Controller Emulator (x360ce)** bridges DirectInput, HID, and legacy controllers to the standard Windows XInput API and ViGEmBus virtual Xbox 360 controller emulation.

This repository features the modernized, high-performance architecture of x360ce:
- **Ultra-Low Latency Engine**: 1000 Hz polling, 1ms multimedia timer resolution (`timeBeginPeriod(1)`), AboveNormal process priority, and Sustained Low-Latency GC.
- **Modern Setup & Installer**: Ergonomic, dark-mode native Windows UI supporting automatic game library scanning, Minecraft (Bedrock & Java), in-place file deployment with zero duplicates, and verified Twin USB controller profiles.
- **Anti-Monolith Architecture**: Strict modular design ensuring clean separation of concerns and file size constraints (<300 lines per module).

---

## 2. Layered Architecture

```
┌────────────────────────────────────────────────────────┐
│                   PRESENTATION LAYER                   │
│  - MainSetupForm (Core state & lifecycle)               │
│  - MainSetupForm.Layout (Direct2D-style layout engine)  │
│  - MainSetupForm.Events (Ergonomic UX & drag-and-drop)  │
│  - Custom Controls: ModernButton, ModernProgressBar,    │
│    ModernFolderCard, ModernDeviceGrid                  │
└──────────────────────────┬─────────────────────────────┘
                           │
┌──────────────────────────▼─────────────────────────────┐
│               DOMAIN & ORCHESTRATION LAYER             │
│  - SetupEngine (Deployment & pipeline orchestration)    │
│  - SetupEngine.Detection (Drive & library scanning,     │
│    PE architecture reader, Minecraft Bedrock/Java)     │
│  - SetupEngine.XmlConfig (Atomic XML persistence,      │
│    verified PadSettings calibration, Sider fix)        │
│  - SetupEngine.Models (Data contracts & DTOs)          │
└──────────────────────────┬─────────────────────────────┘
                           │
┌──────────────────────────▼─────────────────────────────┐
│                 ENGINE & EMULATION LAYER               │
│  - x360ce.Engine (DirectInput & XInput hook core)      │
│  - ViGEmClient (Virtual Xbox 360 Gamepad Driver)       │
│  - Win32 Multimedia Timer (1ms / 1000 Hz precision)   │
│  - Garbage Collection: SustainedLowLatency mode         │
└────────────────────────────────────────────────────────┘
```

---

## 3. Component Details

### 3.1 Presentation Layer (`Setup/`)
- **`MainSetupForm.cs`**: Form lifecycle, initialization, window shadow styling, and modern DWM title bar theme integration.
- **`MainSetupForm.Layout.cs`**: Complete control tree construction, responsive repositioning, and dynamic layout calculations.
- **`MainSetupForm.Events.cs`**: User interactions, multi-format drag-and-drop handler, background installation worker triggers, and executable launch delegates.
- **`ModernControls.cs`**: Anti-aliased custom controls rendered via GDI+ with rounded borders, hover physics, and contrast-compliant palettes.

### 3.2 Domain & Orchestration (`Setup/`)
- **`SetupEngine.cs`**: Coordinates atomic file deployment, ReadOnly flag handling, in-place overwrites, and real-time progress callbacks.
- **`SetupEngine.Detection.cs`**: Multi-drive game detection across Steam, Epic, GOG, XboxGames, EA, Ubisoft, and emulators. Full support for Minecraft Bedrock UWP and Minecraft Java Edition (Vanilla, Prism, CurseForge, Modrinth, Lunar, Badlion).
- **`SetupEngine.XmlConfig.cs`**: Reads and writes `x360ce.UserGames.xml`, `x360ce.UserSettings.xml`, and `x360ce.PadSettings.xml` to `C:\ProgramData\X360CE\Settings`. Solves button reversal and dual-input conflicts (e.g., PES / Sider).
- **`SetupEngine.Models.cs`**: Typed data models (`DetectedGameInfo`, `DetectedControllerInfo`).

### 3.3 Engine & Emulation (`Engine/`, `App.v4/`)
- **`ViGEmClient.cs`**: Communicates with the Nefarius ViGEmBus kernel-mode driver to present standard virtual X360 gamepads to Windows.
- **`Global.cs` & `MainForm.cs`**: Configured with 1000 Hz polling loop, Windows 1ms timer resolution, and low-latency garbage collection.

---

## 4. Engineering Standards & Conventions

1. **Anti-Monolith Standard**:
   - Monolithic files (>300 lines) are decomposed into focused partial classes or dedicated services.
   - Distinct separation between Presentation, Domain, Data, and Native layers.
2. **Deterministic File Deployment**:
   - Always update and replace existing files in place; never produce duplicate or orphaned files.
3. **Zero-Bug Execution**:
   - Defensive null checks, graceful exception handling across all file system and registry scans, and zero compiler warnings.
4. **Binary & Runtime Compatibility**:
   - Strict target runtime: `.NET Framework 4.6.2`, ensuring out-of-the-box compatibility on Windows 7 SP1, 8.1, 10, and 11 without requiring extra runtime downloads.
