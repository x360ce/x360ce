/*
 * main.c
 * Sample kernel driver to show how diStorm can be easily compiled and used in Ring 0.
 *
 * /// Follow the README file in order to compile diStorm using the DDK. \\\
 * 
 * Izik, Gil Dabah
 * Jan 2007
 * http://ragestorm.net/distorm/
 */

#include <ntddk.h>
#include "../include/distorm.h"
#include "dummy.c"

// The number of the array of instructions the decoder function will use to return the disassembled instructions.
// Play with this value for performance...
#define MAX_INSTRUCTIONS (15)

void DriverUnload(IN PDRIVER_OBJECT DriverObject)
{
}

NTSTATUS DriverEntry(IN PDRIVER_OBJECT DriverObject, IN PUNICODE_STRING RegistryPath)
{
	UNICODE_STRING pFcnName;
	
	// Holds the result of the decoding.
	_DecodeResult res;
	// Decoded instruction information.
	_DecodedInst decodedInstructions[MAX_INSTRUCTIONS];
	// next is used for instruction's offset synchronization.
	// decodedInstructionsCount holds the count of filled instructions' array by the decoder.
	unsigned int decodedInstructionsCount = 0, i, next;
	// Default decoding mode is 32 bits, could be set by command line.
	_DecodeType dt = Decode32Bits;

	// Default offset for buffer is 0, could be set in command line.
	_OffsetType offset = 0;
	char* errch = NULL;

	// Buffer to disassemble.
	unsigned char *buf;
	int len = 100;

	// Register unload routine
	DriverObject->DriverUnload = DriverUnload;

	DbgPrint("diStorm Loaded!\n");

	// Get address of KeBugCheck
	RtlInitUnicodeString(&pFcnName, L"KeBugCheck");
	buf = (char *)MmGetSystemRoutineAddress(&pFcnName);
	offset = (unsigned) (_OffsetType)buf;

	DbgPrint("Resolving KeBugCheck @ 0x%08x\n", buf);
	// Decode the buffer at given offset (virtual address).

	while (1) {
		res = distorm_decode64(offset, (const unsigned char*)buf, len, dt, decodedInstructions, MAX_INSTRUCTIONS, &decodedInstructionsCount);
		if (res == DECRES_INPUTERR) {
			DbgPrint(("NULL Buffer?!\n"));
			break;
		}

		for (i = 0; i < decodedInstructionsCount; i++) {
			// Note that we print the offset as a 64 bits variable!!!
			// It might be that you'll have to change it to %08X...
			DbgPrint("%08I64x (%02d) %s %s %s\n", decodedInstructions[i].offset, decodedInstructions[i].size, 
				 (char*)decodedInstructions[i].instructionHex.p,
				 (char*)decodedInstructions[i].mnemonic.p,
				 (char*)decodedInstructions[i].operands.p);
		}

		if (res == DECRES_SUCCESS || decodedInstructionsCount == 0) {
			break; // All instructions were decoded.
		}

		// Synchronize:
		next = (unsigned int)(decodedInstructions[decodedInstructionsCount-1].offset - offset);
		next += decodedInstructions[decodedInstructionsCount-1].size;

		// Advance ptr and recalc offset.
		buf += next;
		len -= next;
		offset += next;
	}

	DbgPrint(("Done!\n"));
	return STATUS_UNSUCCESSFUL; // Make sure the driver doesn't stay resident, so we can recompile and run again!
}
