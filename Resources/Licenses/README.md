# Licences of the parts x360ce carries

Each part x360ce carries from another project, and its licence. The licence files are the projects' own, unchanged,
under the names the projects give them. A licence that has to stay beside its part, such as a font's or a vendored
library's, stays there, and the table links to it.

Both programs embed this file and the licence files of the parts they carry. Their About page's License tab shows
x360ce's own licence, then each part below that names the program, in this order (`Engine/Common/ThirdPartyLicences.cs`).
x360ce 3 carries the XInput library, so a part of the library names x360ce 3 too.

## Parts

| Part | Used for | Programs | Address | Licence | Licence file | Provenance |
|---|---|---|---|---|---|---|
| SharpDX | Reading controllers through DirectInput and XInput | x360ce 4, x360ce 3 | https://github.com/sharpdx/SharpDX | MIT License | [License.txt](SharpDX/License.txt) | `License.txt` at commit `8e5df9f17b1d328c595a9df5851dbb2537b55621`, the commit SharpDX 4.2.0's DLLs name, retrieved 2026-11-16, SHA-256 `ed6e2ed433cc7006268284316ae05c40ec7350903c2cc67ffe8db6cdfca887ca` |
| ViGEmBus 1.21.442 | The virtual Xbox 360 controllers' driver, which x360ce 4 installs on Windows 10 and 11 | x360ce 4 | https://github.com/nefarius/ViGEmBus | BSD 3-Clause License | [LICENSE](ViGEmBus_1_21_442/LICENSE) | `LICENSE` at tag `v1.21.442.0`, commit `d2ba1096d48983c5c8a9a98f5f05878d0f0eb478`, the version of the setup in `ViGEmBus.zip`, retrieved 2026-11-16, SHA-256 `3959f6ca50466675190590dfd024585744b6456ae7d39a3222e6550d62882586` |
| ViGEmBus 1.16.112 | The same driver, which x360ce 4 installs on Windows 7 and 8 | x360ce 4 | https://github.com/nefarius/ViGEmBus | GNU General Public License v3.0 | [LICENSE](ViGEmBus_1_16_112/LICENSE) | `LICENSE` at tag `v1.16.112`, commit `499340f66b42b41c54c96f72cc327ce5b24d8d74`, the version of the driver files in `ViGEmBus.zip`, retrieved 2026-11-16, SHA-256 `8ceb4b9ee5adedde47b31e975c1d90c73ad27b6b165a1dcd80c7c545eb65b903` |
| ViGEmClient | Connecting x360ce 4 to the ViGEmBus driver (`ViGEmClient.dll` 1.14.2.0) | x360ce 4 | https://github.com/nefarius/ViGEmClient | MIT License | [LICENSE](ViGEm/LICENSE) | `LICENSE` at commit `7fe523a80e628ac6e80eb72f34f13be81eff5745`, the last before x360ce took `ViGEmClient.dll` on 2018-01-29 (`f6188d7a`), and the same at `master`, retrieved 2026-11-16, SHA-256 `445b3bccd103d39cab7e9b276dd966d60269cf5c44b4b8a118e48bc4c7c4f0db` |
| ViGEm.NET | x360ce 4's side of ViGEmClient (`App.v4/ViGEm/Client`) | x360ce 4 | https://github.com/nefarius/ViGEm.NET | MIT License | [LICENSE](ViGEm/LICENSE) | `LICENSE` at commit `624beb2a15a89f1248118f781a43f6270886e2d3`, the last before x360ce took the client on 2018-01-29 (`f6188d7a`), and the same at `master`, retrieved 2026-11-16, SHA-256 `445b3bccd103d39cab7e9b276dd966d60269cf5c44b4b8a118e48bc4c7c4f0db` |
| HidGuardian | Hiding controllers from games, which x360ce 4 now only removes (`HidGuardian.zip`, 1.14.3.0) | x360ce 4 | https://github.com/nefarius/HidGuardian | MIT License | [LICENSE](ViGEm/LICENSE) | `LICENSE` at commit `81f9e377c305ac63168d9427642ffe7c221d0c81` of 2018-01-28, the first licence the repository published after version 1.14.3.0 of 2018-01-19, retrieved 2026-11-16, SHA-256 `445b3bccd103d39cab7e9b276dd966d60269cf5c44b4b8a118e48bc4c7c4f0db` |
| SDL_GameControllerDB | Knowing which button, stick and hat of a controller is which part of a gamepad | x360ce 4, x360ce 3, XInput library | https://github.com/mdqinc/SDL_GameControllerDB | zlib License | [LICENSE](SDL_GameControllerDB/LICENSE) | `LICENSE` at commit `07af1d50187e94c861c9f9f5d8a55b148f11306b`, the commit the built-in database is taken from, retrieved 2026-11-16, SHA-256 `97f35b302b361680ec1e891e95d2d52097bb95abff361434916d99dc1305f127` |
| MinHook | Letting the XInput library hook a game's calls (HookMasks) | x360ce 3, XInput library | https://github.com/TsudaKageyu/minhook | BSD 2-Clause License | [LICENSE.txt](../../MinHook/LICENSE.txt) | The `MinHook` git submodule at commit `05c06c5bbca226b72ffb40fc0caaef33bcaf6f74` (`v1.3.4-13`), the file GitHub serves at that commit, retrieved 2026-11-16, SHA-256 `d5c2224982b0b95a16b098a561335fa93f3eaf4d0aa964b7843edb986df78dc8` |
| zlib | Letting the XInput library read its built-in controller databases | x360ce 3, XInput library | https://github.com/madler/zlib | zlib License | [LICENSE](../../Native/Common/zlib/LICENSE) | zlib 1.4, tag `v1.4`, commit `99623ef7097352da7993ba614161fd6a311e046e`, verified as `Native/Common/zlib/SOURCE.md` records, SHA-256 `e32ff4e00d9d94930537635291da39e7e612703334bf6fde8c7f1686fe8a45a2` |
| Selawik | The interface font, when chosen | x360ce 4 | https://github.com/microsoft/Selawik | SIL Open Font License 1.1 | [LICENSE.txt](../Fonts/Selawik/LICENSE.txt) | `LICENSE.txt` at tag `1.01`, commit `b65ff9ce7c4673e75ec5c622924a6b6f61f2f876`, the release whose `selawk.ttf` and `selawkb.ttf` x360ce carries, and the same at `master`, retrieved 2026-11-16, SHA-256 `77b7c2506d4efb22e09c8ccf10159f4956eab3ef7c007fef95de136bcf45300c` |
| Inconsolata | The font of formulas, presets and values | x360ce 4 | https://github.com/googlefonts/Inconsolata | SIL Open Font License 1.1 | [OFL.txt](../Fonts/Inconsolata/OFL.txt) | `OFL.txt` at commit `39212f90dae56b526abc65121a7039a542eb11f9` (`main`), unchanged since commit `6a4cc5433ac9bf536a1894a1980d2ca9bd8554b9` of 2017-08-08, retrieved 2026-11-16, SHA-256 `5d362a6f8690517fd9a5573128a081d8bbbb2f92714cf00556e08fbbe9600426` |
| IconExperience G2 icons | The icons on buttons, menus and tabs | x360ce 4, x360ce 3 | https://www.iconexperience.com/g_collection/ | IconExperience License Agreement (G-Collection v2.0) | [IconExperience.License.txt](IconExperience/IconExperience.License.txt) | The licence on https://www.iconexperience.com/g_collection/license/, title and paragraphs word for word, retrieved 2026-11-16; the file x360ce 4 carried from commit `dc836579` of 2021-05-21. `scripts/art/build_icons.py` recolours the icons. SHA-256 `4e03e35b7e07595160dae3f739e1c0f15d25e612241b9ae9879de5d7d4d62ffc` |

The SHA-256 is of the file with Unix line endings, as Git stores it and as the projects publish it.

To add a part: put its licence file here in a folder of its own, taken unchanged from the project's own repository or
release at the version x360ce carries, add its row, and embed the file in each program the row names under its path
in this repository (`<EmbeddedResource Include="..\{path}"><Link>{path}</Link></EmbeddedResource>`).
`Tests/Common/ThirdPartyLicenceTest.cs` checks every row against the files and both programs.

## Not yet confirmed

x360ce carries these too, but no licence for them is confirmed yet, so the About page does not list them.

| Part | Used for | Programs | What is known | What is missing |
|---|---|---|---|---|
| DX Tweak (`App.v4/Resources/DXTweak/DXTweak2.exe`) | The DX Tweak button: calibrating a DirectInput device | x360ce 4 | Logitech DX Tweaker 5.02.114, from Logitech Gaming Software, "© 1999-2008 Logitech. All rights reserved.", signed by Logitech. Added in commit `fbc285ba` (2020-07-25) with a link to https://www.simracinghub.com/guides/448-dxtweak | Logitech's terms for carrying it |
| Device Console (`devcon.x86.exe`, `devcon.x64.exe` in `ViGEmBus.zip` and `HidGuardian.zip`) | Installing and removing the ViGEmBus 1.16.112 and HidGuardian drivers | x360ce 4 | Microsoft's DevCon, versions 10.0.10586.0 and 10.0.10240.16399, "© Microsoft Corporation" | Where these copies came from, and the terms for carrying them |
| KMDF co-installer (`WdfCoinstaller01009.dll` in `ViGEmBus.zip` and `HidGuardian.zip`) | Letting Windows 7 and 8 install those drivers | x360ce 4 | Microsoft, version 1.9.7600.16385, signed by Microsoft | The terms for carrying it |
| LoadingCircle (`App.v4/Controls/LoadingCircle*.cs`, `App.v3/Controls/LoadingCircle*.cs`) | The circle that turns while x360ce is busy | x360ce 4, x360ce 3 | By Martin R. Gagné, 2006-2007; each file's header carries a BSD-style licence. The CodeProject article it was published with is no longer online | A licence file from its author |
| WinVerify (`App.v4/Common/WinVerify.cs`) | Checking files' digital signatures | x360ce 4 | Microsoft All-In-One Code Framework sample CSCreateCabinet; its header names the Microsoft Public License | The licence text as that project gives it |
| PEReader (`Engine/JocysCom/Win32/PEReader.cs`) | Reading whether a program is 32 or 64 bit | x360ce 4, x360ce 3 | By Jeroen Frijters, 2009-2011, changed by Evaldas Jocys in 2014; its header carries a zlib-style licence | A licence file from its author |
| Controller pictures (`Resources/Images/shared/xbox`, `Resources/Images/v4/xbox/nav`, `scripts/art/controller`) and the program's logo (`Resources/Images/shared/app`) | The controller pages' pictures and the program's icon | x360ce 4, x360ce 3 | In the repository since 2015 (commit `2aeba2e0`) or later, with no source recorded | Their origin and licence |
