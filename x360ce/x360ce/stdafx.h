#pragma once

#include "targetver.h"

#pragma warning(disable: 4503)
#pragma warning(disable: 4239)

#define _BIND_TO_CURRENT_CRT_VERSION 1

#define WIN32_LEAN_AND_MEAN
#define VC_EXTRALEAN
#define STRICT

#ifdef DEBUG
#define _SECURE_SCL 1
#include <crtdbg.h>
#endif

#ifdef NDEBUG
#define _SECURE_SCL 0
#endif

// Windows Header Files:
#include <windows.h>
#include <xinput.h>

#ifdef OVERLOAD_NEW
inline void* operator new(size_t sz)
{
    return LocalAlloc(LMEM_FIXED, sz);
}

inline void operator delete(void* ptr)
{
    LocalFree(ptr);
}
#endif

//C++
#include <string>
#include <vector>
#include <map>
#include <algorithm>

// Portable STDINT

#if _MSC_VER < 1700
#include "pstdint.h"
#else
#include <stdint.h>
#endif

