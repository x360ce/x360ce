/*  minIni - Multi-Platform INI file parser, suitable for embedded systems
 *
 *  Copyright (c) CompuPhase, 2008-2011
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not
 *  use this file except in compliance with the License. You may obtain a copy
 *  of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 *  WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 *  License for the specific language governing permissions and limitations
 *  under the License.
 *
 *  Version: $Id: minIni.h 33 2011-06-20 10:51:17Z thiadmer.riemersma $
 */
#ifndef MININI_H
#define MININI_H

#include "minGlue.h"

#if (defined _UNICODE || defined __UNICODE__ || defined UNICODE) && !defined INI_ANSIONLY
  #include <tchar.h>
#elif !defined __T
  typedef char TCHAR;
#endif

#if !defined INI_BUFFERSIZE
  #define INI_BUFFERSIZE  512
#endif

#if defined __cplusplus
  extern "C" {
#endif

int   ini_getbool(const TCHAR *Section, const TCHAR *Key, int DefValue, const TCHAR *Filename);
long  ini_getl(const TCHAR *Section, const TCHAR *Key, long DefValue, const TCHAR *Filename);
int   ini_gets(const TCHAR *Section, const TCHAR *Key, const TCHAR *DefValue, TCHAR *Buffer, int BufferSize, const TCHAR *Filename);
int   ini_getsection(int idx, TCHAR *Buffer, int BufferSize, const TCHAR *Filename);
int   ini_getkey(const TCHAR *Section, int idx, TCHAR *Buffer, int BufferSize, const TCHAR *Filename);

#if defined INI_REAL
INI_REAL ini_getf(const TCHAR *Section, const TCHAR *Key, INI_REAL DefValue, const TCHAR *Filename);
#endif

#if !defined INI_READONLY
int   ini_putl(const TCHAR *Section, const TCHAR *Key, long Value, const TCHAR *Filename);
int   ini_puts(const TCHAR *Section, const TCHAR *Key, const TCHAR *Value, const TCHAR *Filename);
#if defined INI_REAL
int   ini_putf(const TCHAR *Section, const TCHAR *Key, INI_REAL Value, const TCHAR *Filename);
#endif
#endif /* INI_READONLY */

#if defined __cplusplus
  }
#endif
#endif /* MININI_H */
