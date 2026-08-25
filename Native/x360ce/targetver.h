#pragma once

// Including SDKDDKVer.h defines the highest available Windows platform.

// If you wish to build your application for a previous Windows platform, include WinSDKVer.h and
// set the _WIN32_WINNT macro to the platform you wish to support before including SDKDDKVer.h.

#include <WinSDKVer.h>

// Build-time API gate. Keeping WIN8 lets the compiler accept legacy XInput
// entry points (XInputEnable etc.) that the Win 10 SDK marks #pragma deprecated.
// The resulting DLL still runs on Windows 10 and 11; the supported runtime
// minimum is declared in README.MD ("Windows 10 or newer").
#define _WIN32_WINNT _WIN32_WINNT_WIN8

#include <SDKDDKVer.h>

#define DIRECTINPUT_VERSION 0x0800
