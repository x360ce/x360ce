#pragma once

#include "targetver.h"

#if _MSC_VER < 1700
#define _BIND_TO_CURRENT_CRT_VERSION 1

#ifdef NDEBUG
#define _SECURE_SCL 0
#endif
#endif

#include <cstdlib>
#include <string>
#include <vector>
#include <algorithm>

#define WIN32_LEAN_AND_MEAN
#define STRICT
#define NOMINMAX

#include <windows.h>
#include <xinput.h>
#include <dinput.h>

#include "Common.h"
