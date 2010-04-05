/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 ToCA Edit
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Found-
 *  ation, either version 3 of the License, or (at your option) any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */
 
#include "stdafx.h"
#include "utils.h"
#include <detours.h>

HANDLE currentthread = NULL;

extern void FakeWMI();
extern void FakeDInput();
extern void FakeThread(HANDLE);

HANDLE (WINAPI *OldCreateThread)(
						   __in_opt   LPSECURITY_ATTRIBUTES lpThreadAttributes,
						   __in       SIZE_T dwStackSize,
						   __in       LPTHREAD_START_ROUTINE lpStartAddress,
						   __in_opt   LPVOID lpParameter,
						   __in       DWORD dwCreationFlags,
						   __out_opt  LPDWORD lpThreadId
						   ) = CreateThread;

HANDLE WINAPI NewCreateThread(
								 __in_opt   LPSECURITY_ATTRIBUTES lpThreadAttributes,
								 __in       SIZE_T dwStackSize,
								 __in       LPTHREAD_START_ROUTINE lpStartAddress,
								 __in_opt   LPVOID lpParameter,
								 __in       DWORD dwCreationFlags,
								 __out_opt  LPDWORD lpThreadId
								 )
{

	HANDLE t = OldCreateThread(lpThreadAttributes,dwStackSize,lpStartAddress,lpParameter,dwCreationFlags,lpThreadId);

	FakeWMI();
	//FakeDInput();

	if(OldCreateThread == NULL) FakeThread(t);

	return t;

}


void FakeThread(HANDLE t)
{
	WriteLog(_T("FakeThread"));

	DetourTransactionBegin();
	DetourUpdateThread(t);
	DetourAttach(&(PVOID&)OldCreateThread, NewCreateThread);
	DetourTransactionCommit();

}