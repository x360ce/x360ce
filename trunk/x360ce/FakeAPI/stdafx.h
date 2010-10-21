// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

// TODO: reference additional headers your program requires here

#define _BIND_TO_CURRENT_CRT_VERSION 1

#define STRICT

#ifndef UNICODE
#define UNICODE
#endif

#ifdef DEBUG
#define _SECURE_SCL 1
#include <crtdbg.h>
#endif

#ifdef NDEBUG
#define _SECURE_SCL 0
#endif

// Windows Header Files:
#include <windows.h>
#include <math.h>
#include <time.h>
#include <fstream>
#include <CGuid.h>
