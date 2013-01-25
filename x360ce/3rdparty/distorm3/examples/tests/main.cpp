// diStorm64 library sample
// http://ragestorm.net/distorm/
// Arkon, Stefan, 2005


#include <stdio.h>
#include <stdlib.h>

#pragma comment(lib, "../../distorm.lib")

#include "../../include/distorm.h"

// The number of the array of instructions the decoder function will use to return the disassembled instructions.
// Play with this value for performance...
#define MAX_INSTRUCTIONS (1000)

int main(int argc, char **argv)
{
	_DecodeResult res;
	_DecodedInst decodedInstructions[1000];
	unsigned int decodedInstructionsCount = 0, i = 0;
	_OffsetType offset = 0;
	unsigned int dver = distorm_version();
	printf("diStorm version: %d.%d.%d\n", (dver >> 16), ((dver) >> 8) & 0xff, dver & 0xff);

	unsigned char rawData[] =
{
    0x55, 0x8b, 0xec ,0x8b ,0x45 ,0x08 ,0x03 ,0x45 ,0x0c ,0xc9 ,0xc3
} ;
	res = distorm_decode(offset, (const unsigned char*)rawData, sizeof(rawData), Decode32Bits, decodedInstructions, MAX_INSTRUCTIONS, &decodedInstructionsCount);
	for (int i = 0; i < decodedInstructionsCount; i++) {
		printf("%08I64x (%02d) %-24s %s%s%s\r\n", decodedInstructions[i].offset, decodedInstructions[i].size, (char*)decodedInstructions[i].instructionHex.p, (char*)decodedInstructions[i].mnemonic.p, decodedInstructions[i].operands.length != 0 ? " " : "", (char*)decodedInstructions[i].operands.p);
	}

	return 0;
}
