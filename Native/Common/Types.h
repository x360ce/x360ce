#pragma once

#include <stdint.h>

typedef int8_t    s8;
typedef uint8_t   u8;
typedef int16_t  s16;
typedef uint16_t u16;
typedef int32_t  s32;
typedef uint32_t u32;
typedef int64_t  s64;
typedef uint64_t u64;

typedef	intptr_t sPointer;
typedef uintptr_t uPointer;

typedef float  f32;
typedef double f64;

union Word
{
	f32 float32;
	u32 bits32;
	u16 bits16[2];
	u8  bits8[4];
};

union DWord
{
	u64 bits64;
	f64 float64;
	f32 float32[2];
	u32 bits32[2];
	u16 bits16[4];
	u8  bits8[8];

	Word word[2];
};

union QWord
{
	u64 bits64[2];
	f64 float64[2];
	f32 float32[4];
	u32 bits32[4];
	u16 bits16[8];
	u8  bits8[16];

	Word word[4];
	DWord dword[2];
};
