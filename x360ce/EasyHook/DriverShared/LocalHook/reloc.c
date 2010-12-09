/*
    EasyHook - The reinvention of Windows API hooking
 
    Copyright (C) 2009 Christoph Husse

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

    Please visit http://www.codeplex.com/easyhook for more information
    about the project and latest updates.
*/
#include "stdafx.h"

EASYHOOK_NT_INTERNAL LhGetInstructionLength(void* InPtr)
{
/*
Description:

    Takes a pointer to machine code and returns the length of the
    referenced instruction in bytes.
    
Returns:
    STATUS_INVALID_PARAMETER

        The given pointer references invalid machine code.
*/
	LONG			Length = -1;

	// might return wrong results for exotic instructions, leading to unknown application behavior...
#ifdef _M_X64
	Length = GetInstructionLength_x64(InPtr, 64);
#else
	Length = GetInstructionLength_x86(InPtr, 0);
#endif

	if(Length > 0)
		return Length;	
	else
		return STATUS_INVALID_PARAMETER;
}






EASYHOOK_NT_INTERNAL LhRoundToNextInstruction(
			void* InCodePtr,
			ULONG InCodeSize)
{
/*
Description:

    Will round the given code size up so that the return
    value spans at least over "InCodeSize" bytes and always
    ends on instruction boundaries.

Parameters:

    - InCodePtr

        A code portion the given size should be aligned to.

    - InCodeSize

        The minimum return value.

Returns:

    STATUS_INVALID_PARAMETER

        The given pointer references invalid machine code.
*/
	UCHAR*				Ptr = (UCHAR*)InCodePtr;
	UCHAR*				BasePtr = Ptr;
    NTSTATUS            NtStatus;

	while(BasePtr + InCodeSize > Ptr)
	{
		FORCE(NtStatus = LhGetInstructionLength(Ptr));

		Ptr += NtStatus;
	}

	return (ULONG)(Ptr - BasePtr);

THROW_OUTRO:
    return NtStatus;
}






EASYHOOK_NT_INTERNAL LhRelocateEntryPoint(
				UCHAR* InEntryPoint,
				ULONG InEPSize,
				UCHAR* Buffer,
				ULONG* OutRelocSize)
{
/*
Description:

    Relocates the given entry point into the buffer and finally
    stores the relocated size in OutRelocSize.

Parameters:

    - InEntryPoint

        The entry point to relocate.

    - InEPSize

        Size of the given entry point in bytes.

    - Buffer

        A buffer receiving the relocated entry point.
        To ensure that there is always enough space, you should
        reserve around 100 bytes. After completion this method will
        store the real size in bytes in "OutRelocSize".

    - OutRelocSize

        Receives the size of the relocated entry point in bytes.

Returns:

*/
#ifdef _M_X64
    #define POINTER_TYPE    LONGLONG
#else
    #define POINTER_TYPE    LONG
#endif
	UCHAR*				pRes = Buffer;
	UCHAR*				pOld = InEntryPoint;
    UCHAR			    b1;
	UCHAR			    b2;
	ULONG			    OpcodeLen;
	POINTER_TYPE   	    AbsAddr;
	BOOL			    a16;
	BOOL			    IsRIPRelative;
    ULONG               InstrLen;
    NTSTATUS            NtStatus;

	ASSERT(InEPSize < 20);

	while(pOld < InEntryPoint + InEPSize)
	{
		b1 = *(pOld);
		b2 = *(pOld + 1);
		OpcodeLen = 0;
		AbsAddr = 0;
		a16 = FALSE;
		IsRIPRelative = FALSE;

		// check for prefixes
		switch(b1)
		{
		case 0x67: a16 = TRUE; continue;
		}

		/////////////////////////////////////////////////////////
		// get relative address value
		switch(b1)
		{
			case 0xE9: // jmp imm16/imm32
			{
				/* only allowed as first instruction and only if the trampoline can be planted 
				   within a 32-bit boundary around the original entrypoint. So the jumper will 
				   be only 5 bytes and whereever the underlying code returns it will always
				   be in a solid state. But this can only be guaranteed if the jump is the first
				   instruction... */
				if(pOld != InEntryPoint)
					THROW(STATUS_NOT_SUPPORTED, L"Hooking far jumps is only supported if they are the first instruction.");
				
				// ATTENTION: will continue in "case 0xE8"
			}
		case 0xE8: // call imm16/imm32
			{
				if(a16)
				{
					AbsAddr = *((__int16*)(pOld + 1));
					OpcodeLen = 3;
				}
				else
				{
					AbsAddr = *((__int32*)(pOld + 1));
					OpcodeLen = 5;
				}
			}break;

		/*
			The problem with (conditional) jumps is that there will be no return into the relocated entry point.
			So the execution will be proceeded in the original method and this will cause the whole
			application to remain in an unstable state. Only near jumps with 32-bit offset are allowed as
			first instruction (see above)...
		*/
		case 0xEB: // jmp imm8
		case 0xE3: // jcxz imm8
			{
				THROW(STATUS_NOT_SUPPORTED, L"Hooking near (conditional) jumps is not supported.");
			}break;
		case 0x0F:
			{
				if((b2 & 0xF0) == 0x80) // jcc imm16/imm32
					THROW(STATUS_NOT_SUPPORTED,  L"Hooking far conditional jumps is not supported.");
			}break;
		}

		if((b1 & 0xF0) == 0x70) // jcc imm8
			THROW(STATUS_NOT_SUPPORTED,  L"Hooking near conditional jumps is not supported.");

		/////////////////////////////////////////////////////////
		// convert to: mov eax, AbsAddr

		if(OpcodeLen > 0)
		{
			AbsAddr += (POINTER_TYPE)(pOld + OpcodeLen);

#ifdef _M_X64
			*(pRes++) = 0x48; // REX.W-Prefix
#endif
			*(pRes++) = 0xB8;

			*((LONGLONG*)pRes) = AbsAddr;

			pRes += sizeof(void*);

			// points into entry point?
			if((AbsAddr >= (LONGLONG)InEntryPoint) && (AbsAddr < (LONGLONG)InEntryPoint + InEPSize))
				/* is not really unhookable but not worth the effort... */
				THROW(STATUS_NOT_SUPPORTED, L"Hooking jumps into the hooked entry point is not supported.");

			/////////////////////////////////////////////////////////
			// insert alternate code
			switch(b1)
			{
			case 0xE8: // call eax
				{
					*(pRes++) = 0xFF;
					*(pRes++) = 0xD0;
				}break;
			case 0xE9: // jmp eax
				{
					*(pRes++) = 0xFF;
					*(pRes++) = 0xE0;
				}break;
			}

			/* such conversions shouldnt be necessary in general...
			   maybe the method was already hooked or uses some hook protection or is just
			   bad programmed. EasyHook is capable of hooking the same method
			   many times simultanously. Even if other (unknown) hook libraries are hooking methods that
			   are already hooked by EasyHook. Only if EasyHook hooks methods that are already
			   hooked with other libraries there can be problems if the other libraries are not
			   capable of such a "bad" circumstance.
			*/

			*OutRelocSize = (ULONG)(pRes - Buffer);
		}
		else
		{
#ifndef DRIVER
            if(DbgIsEnabled())
            {
                // RIP relative detection
                DbgRelocateRIPRelative((ULONGLONG)pOld, (ULONGLONG)pRes, &IsRIPRelative);
            }
#endif
		}

		// find next instruction
		FORCE(InstrLen = LhGetInstructionLength(pOld));

		if(OpcodeLen == 0)
		{
			// just copy the instruction
			if(!IsRIPRelative)
				RtlCopyMemory(pRes, pOld, InstrLen);

			pRes += InstrLen;
		}

		pOld += InstrLen;
		IsRIPRelative = FALSE;
	}

	*OutRelocSize = (ULONG)(pRes - Buffer);

	RETURN(STATUS_SUCCESS);

THROW_OUTRO:
FINALLY_OUTRO:
    return NtStatus;
}