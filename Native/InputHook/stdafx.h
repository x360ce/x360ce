#pragma once

#include "targetver.h"

#ifdef DEBUG
#include <crtdbg.h>
#endif

#include <string>
#include <vector>
#include <algorithm>

#define WIN32_LEAN_AND_MEAN
#define STRICT

#include <windows.h>
#include <xinput.h>

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>
#include <dinput.h>

#include "Common.h"
