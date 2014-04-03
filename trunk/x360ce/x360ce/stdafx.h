/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#pragma once

#include "targetver.h"

#define _BIND_TO_CURRENT_CRT_VERSION 1

#define WIN32_LEAN_AND_MEAN
#define VC_EXTRALEAN
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
#include <shlwapi.h>
#include <Shlobj.h>

//C++
#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <algorithm>

// Portable STDINT
#include "pstdint.h"

#include "Logger.h"
