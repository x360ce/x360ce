#pragma once

// Including SDKDDKVer.h defines the highest available Windows platform.

// If you wish to build your application for a previous Windows platform, include WinSDKVer.h and
// set the _WIN32_WINNT macro to the platform you wish to support before including SDKDDKVer.h.

#include <WinSDKVer.h>

#if _MSC_VER < 1700
#define _WIN32_WINNT _WIN32_WINNT_WINXP
#else
#define _WIN32_WINNT _WIN32_WINNT_VISTA
#endif

#include <SDKDDKVer.h>

#define DIRECTINPUT_VERSION 0x0800
