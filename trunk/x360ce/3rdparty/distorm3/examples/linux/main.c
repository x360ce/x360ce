// diStorm64 library sample
// http://ragestorm.net/distorm/
// Arkon, Stefan, 2005
// Mikhail, 2006
// JvW, 2007

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <errno.h>
#include <sys/stat.h>

// For the compilers who don't have sysexits.h, which is not an ISO/ANSI include!
#define EX_OK           0
#define EX_USAGE       64
#define EX_DATAERR     65
#define EX_NOINPUT     66
#define EX_NOUSER      67
#define EX_NOHOST      68
#define EX_UNAVAILABLE 69
#define EX_SOFTWARE    70
#define EX_OSERR       71
#define EX_OSFILE      72
#define EX_CANTCREAT   73
#define EX_IOERR       74
#define EX_TEMPFAIL    75
#define EX_PROTOCOL    76
#define EX_NOPERM      77
#define EX_CONFIG      78

#include "../../include/distorm.h"

// The number of the array of instructions the decoder function will use to return the disassembled instructions.
// Play with this value for performance...
#define MAX_INSTRUCTIONS (1000)

int main(int argc, char **argv)
{
	// Version of used compiled library.
	unsigned int dver = 0;
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
	FILE* f;
	unsigned long filesize = 0, bytesread = 0;
	struct stat st;

	// Buffer to disassemble.
	unsigned char *buf, *buf2;

	// Disassembler version.
	dver = distorm_version();
	printf("diStorm version: %u.%u.%u\n", (dver >> 16), ((dver) >> 8) & 0xff, dver & 0xff);

	// Check params.
	if (argc < 2 || argc > 4) {
		printf("Usage: ./disasm [-b16] [-b64] filename [memory offset]\r\nRaw disassembler output.\r\nMemory offset is origin of binary file in memory (address in hex).\r\nDefault decoding mode is -b32.\r\nexample:   disasm -b16 demo.com 789a\r\n");
		return EX_USAGE;
	}

	if (strncmp(argv[param], "-b16", 4) == 0) {
		dt = Decode16Bits;
		param++;
	} else if (strncmp(argv[param], "-b64", 4) == 0) {
		dt = Decode64Bits;
		param++;
	} else if (*argv[param] == '-') {
		fputs("Decoding mode size isn't specified!\n", stderr);
		return EX_USAGE;
	} else if (argc == 4) {
		fputs("Too many parameters are set.\n", stderr);
		return EX_USAGE;
	}
	if (param >= argc) {
		fputs("Filename is missing.\n", stderr);
		return EX_USAGE;
	}
	if (param + 1 == argc-1) { // extra param?
#ifdef SUPPORT_64BIT_OFFSET
		offset = strtoull(argv[param + 1], &errch, 16);
#else
		offset = strtoul(argv[param + 1], &errch, 16);
#endif
		if (*errch != '\0') {
			fprintf(stderr, "Offset `%s' couldn't be converted.\n", argv[param + 1]);
			return EX_USAGE;
		}
	}

	f = fopen(argv[param], "rb");
	if (f == NULL) { 
		perror(argv[param]);
		return EX_NOINPUT;
	}

	if (fstat(fileno(f), &st) != 0) {
		perror("fstat");
		fclose(f);
		return EX_NOINPUT;
	}
	filesize = st.st_size;

	// We read the whole file into memory in order to make life easier,
	// otherwise we would have to synchronize the code buffer as well (so instructions won't be split).
	buf2 = buf = malloc(filesize);
	if (buf == NULL) {
		perror("File too large.");
		fclose(f);
		return EX_UNAVAILABLE;
	}
	bytesread = fread(buf, 1, filesize, f);
	if (bytesread != filesize) {
		perror("Can't read file into memory.");
		free(buf);
		fclose(f);
		return EX_IOERR;
	}

	fclose(f);

	printf("bits: %d\nfilename: %s\norigin: ", dt == Decode16Bits ? 16 : dt == Decode32Bits ? 32 : 64, argv[param]);
#ifdef SUPPORT_64BIT_OFFSET
	if (dt != Decode64Bits) printf("%08llx\n", offset);
	else printf("%016llx\n", offset);
#else
	printf("%08x\n", offset);
#endif

	// Decode the buffer at given offset (virtual address).
	while (1) {
		// If you get an undefined reference linker error for the following line,
		// change the SUPPORT_64BIT_OFFSET in distorm.h.
		res = distorm_decode(offset, (const unsigned char*)buf, filesize, dt, decodedInstructions, MAX_INSTRUCTIONS, &decodedInstructionsCount);
		if (res == DECRES_INPUTERR) {
			// Null buffer? Decode type not 16/32/64?
			fputs("Input error, halting!\n", stderr);
			free(buf2);
			return EX_SOFTWARE;
		}

		for (i = 0; i < decodedInstructionsCount; i++)
#ifdef SUPPORT_64BIT_OFFSET
			printf("%0*llx (%02d) %-24s %s%s%s\r\n", dt != Decode64Bits ? 8 : 16, decodedInstructions[i].offset, decodedInstructions[i].size, (char*)decodedInstructions[i].instructionHex.p, (char*)decodedInstructions[i].mnemonic.p, decodedInstructions[i].operands.length != 0 ? " " : "", (char*)decodedInstructions[i].operands.p);
#else
			printf("%08x (%02d) %-24s %s%s%s\r\n", decodedInstructions[i].offset, decodedInstructions[i].size, (char*)decodedInstructions[i].instructionHex.p, (char*)decodedInstructions[i].mnemonic.p, decodedInstructions[i].operands.length != 0 ? " " : "", (char*)decodedInstructions[i].operands.p);
#endif

		if (res == DECRES_SUCCESS) break; // All instructions were decoded.
		else if (decodedInstructionsCount == 0) break;

		// Synchronize:
		next = (unsigned int)(decodedInstructions[decodedInstructionsCount-1].offset - offset);
		next += decodedInstructions[decodedInstructionsCount-1].size;
		// Advance ptr and recalc offset.
		buf += next;
		filesize -= next;
		offset += next;
	}

	// Release buffer
	free(buf2);

	return EX_OK;
}
