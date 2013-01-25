// diStorm64 library sample
// http://ragestorm.net/distorm/
// Arkon, Stefan, 2005


#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "../../include/distorm.h"

// Link the library into our project.
#pragma comment(lib, "../../distorm.lib")

// The number of the array of instructions the decoder function will use to return the disassembled instructions.
// Play with this value for performance...
#define MAX_INSTRUCTIONS (1000)

int main(int argc, char **argv)
{
	// Version of used compiled library.
	unsigned long dver = 0;
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

	// Index to file name in argv.
	int param = 1;

	// Handling file.
	HANDLE file;
	DWORD filesize, bytesread;

	// Buffer to disassemble.
	unsigned char *buf, *buf2;

	// Disassembler version.
	dver = distorm_version();
	printf("diStorm version: %d.%d.%d\n", (dver >> 16), ((dver) >> 8) & 0xff, dver & 0xff);

	// Check params.
	if (argc < 2 || argc > 4) {
		printf("Usage: disasm.exe [-b16] [-b64] filename [memory offset]\r\nRaw disassembler output.\r\nMemory offset is origin of binary file in memory (address in hex).\r\nDefault decoding mode is -b32.\r\nexample:   disasm -b16 demo.com 789a\r\n");
		return -1;
	}

	if (strncmp(argv[param], "-b16", 4) == 0) {
		dt = Decode16Bits;
		param++;
	} else if (strncmp(argv[param], "-b64", 4) == 0) {
		dt = Decode64Bits;
		param++;
	} else if (*argv[param] == '-') {
		printf("Decoding mode size isn't specified!");
		return -1;
	} else if (argc == 4) {
		printf("Too many parameters are set.");
		return -1;
	}
	if (param >= argc) {
		printf("Filename is missing.");
		return -1;
	}
	if (param + 1 == argc-1) { // extra param?
#ifdef SUPPORT_64BIT_OFFSET
		offset = _strtoui64(argv[param + 1], &errch, 16);
#else
		offset = strtoul(argv[param + 1], &errch, 16);
#endif
		if (*errch != '\0') {
			printf("Offset couldn't be converted.");
			return -1;
		}
	}

	file = CreateFile(argv[param], GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (file == INVALID_HANDLE_VALUE) { 
		printf("Could not open file %s (error %d)\n", argv[param], GetLastError());
		return -2;
	}

	if ((filesize = GetFileSize(file, NULL)) < 0) {
		printf("Error getting filesize (error %d)\n", GetLastError());
		CloseHandle(file);
		return -3;
	}

	// We read the whole file into memory in order to make life easier,
	// otherwise we would have to synchronize the code buffer as well (so instructions won't be split).
	buf2 = buf = (unsigned char*)malloc(filesize);
	if (!ReadFile(file, buf, filesize, &bytesread, NULL)) {
		printf("Error reading file (error %d)\n", GetLastError());
		CloseHandle(file);
		free(buf);
		return -3;
	}

	if (filesize != bytesread) {
		printf("Internal read-error in system\n");
		CloseHandle(file);
		free(buf);
		return -3;
	}

	CloseHandle(file);

	printf("bits: %d\nfilename: %s\norigin: ", dt == Decode16Bits ? 16 : dt == Decode32Bits ? 32 : 64, argv[param]);
#ifdef SUPPORT_64BIT_OFFSET
	if (dt != Decode64Bits) printf("%08I64x\n", offset);
	else printf("%016I64x\n", offset);
#else
	printf("%08x\n", offset);
#endif

	// Decode the buffer at given offset (virtual address).
	while (1) {
		// If you get an unresolved external symbol linker error for the following line,
		// change the SUPPORT_64BIT_OFFSET in distorm.h.
		res = distorm_decode(offset, (const unsigned char*)buf, filesize, dt, decodedInstructions, MAX_INSTRUCTIONS, &decodedInstructionsCount);
		if (res == DECRES_INPUTERR) {
			// Null buffer? Decode type not 16/32/64?
			printf("Input error, halting!");
			free(buf2);
			return -4;
		}

		for (i = 0; i < decodedInstructionsCount; i++) {
#ifdef SUPPORT_64BIT_OFFSET
			printf("%0*I64x (%02d) %-24s %s%s%s\n", dt != Decode64Bits ? 8 : 16, decodedInstructions[i].offset, decodedInstructions[i].size, (char*)decodedInstructions[i].instructionHex.p, (char*)decodedInstructions[i].mnemonic.p, decodedInstructions[i].operands.length != 0 ? " " : "", (char*)decodedInstructions[i].operands.p);
#else
			printf("%08x (%02d) %-24s %s%s%s\n", decodedInstructions[i].offset, decodedInstructions[i].size, (char*)decodedInstructions[i].instructionHex.p, (char*)decodedInstructions[i].mnemonic.p, decodedInstructions[i].operands.length != 0 ? " " : "", (char*)decodedInstructions[i].operands.p);
#endif
		}

		if (res == DECRES_SUCCESS) break; // All instructions were decoded.
		else if (decodedInstructionsCount == 0) break;

		// Synchronize:
		next = (unsigned long)(decodedInstructions[decodedInstructionsCount-1].offset - offset);
		next += decodedInstructions[decodedInstructionsCount-1].size;
		// Advance ptr and recalc offset.
		buf += next;
		filesize -= next;
		offset += next;
	}

	// Release buffer
	free(buf2);

	return 0;
}
