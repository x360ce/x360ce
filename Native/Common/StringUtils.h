#pragma once

#include "Types.h"

#include <string>
#include <sstream>

std::string StringFormat(const char* format, ...);
std::wstring StringFormat(const wchar_t* format, ...);

bool CharArrayFormatV(char* out, int outsize, const char* format, va_list args);
bool CharArrayFormatV(wchar_t* out, int outsize, const wchar_t* format, va_list args);

bool Convert(const std::string &str, s8 *const output);
bool Convert(const std::string &str, u8 *const output);
bool Convert(const std::string &str, s16 *const output);
bool Convert(const std::string &str, u16 *const output);
bool Convert(const std::string &str, s32 *const output);
bool Convert(const std::string &str, u32 *const output);
bool Convert(const std::string &str, s64 *const output);
bool Convert(const std::string &str, u64 *const output);
bool Convert(const std::string &str, float *const output);
bool Convert(const std::string &str, double *const output);
bool Convert(const std::string &str, bool *output);

bool Convert(const std::string &str, long *const output);
bool Convert(const std::string &str, unsigned long *const output);

std::string CP1252ToUTF8(const std::string& str);
std::string UTF16ToUTF8(const std::wstring& str);
std::wstring UTF8ToUTF16(const std::string& str);

#ifdef _UNICODE
inline std::string TStrToUTF8(const std::wstring& str)
{
	return UTF16ToUTF8(str);
}

inline std::wstring UTF8ToTStr(const std::string& str)
{
	return UTF8ToUTF16(str);
}
#else
inline std::string TStrToUTF8(const std::string& str)
{
	return str;
}

inline std::string UTF8ToTStr(const std::string& str)
{
	return str;
}
#endif
