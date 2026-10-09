#include "stdafx.h"

#include <memory>
#include <string>
#include <sstream>

#include <Windows.h>

#include "Types.h"
#include "StringUtils.h"

bool CharArrayFormatV(char* out, int outsize, const char* format, va_list args)
{
	int count;
	static _locale_t locale = nullptr;
	if (!locale) locale = _create_locale(LC_ALL, ".1252");
	count = _vsnprintf_s_l(out, outsize, outsize, format, locale, args);

	if (count > 0 && count < outsize)
	{
		out[count] = '\0';
		return true;
	}
	else
	{
		out[outsize - 1] = '\0';
		return false;
	}
}

bool CharArrayFormatV(wchar_t* out, int outsize, const wchar_t* format, va_list args)
{
	int count = _vsnwprintf_s(out, outsize, outsize, format, args);
	if (count > 0 && count < outsize)
	{
		out[count] = L'\0';
		return true;
	}
	else
	{
		out[outsize - 1] = L'\0';
		return false;
	}
}

std::string StringFormat(const char* format, ...)
{
	va_list args;
	int required = 0;

	va_start(args, format);
	required = _vscprintf(format, args);

	std::unique_ptr<char[]> buf(new char[required + 1]);
	CharArrayFormatV(buf.get(), required + 1, format, args);
	va_end(args);

	std::string temp = buf.get();
	return temp;
}

std::wstring StringFormat(const wchar_t* format, ...)
{
	va_list args;
	int required = 0;

	va_start(args, format);
	required = _vscwprintf(format, args);

	std::unique_ptr<wchar_t[]> buf(new wchar_t[required + 1]);
	CharArrayFormatV(buf.get(), required + 1, format, args);
	va_end(args);

	std::wstring temp = buf.get();
	return temp;
}

bool Convert(const std::string &str, s8 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	s32 value = strtol(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<s8>(value);
	return true;
}

bool Convert(const std::string &str, u8 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	u32 value = strtoul(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<u8>(value);
	return true;
}

bool Convert(const std::string &str, s16 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	s32 value = strtol(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<s16>(value);
	return true;
}

bool Convert(const std::string &str, u16 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	u32 value = strtoul(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<u16>(value);
	return true;
}

bool Convert(const std::string &str, s32 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	s32 value = strtol(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<s32>(value);
	return true;
}

bool Convert(const std::string &str, u32 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	u32 value = strtoul(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<u32>(value);
	return true;
}

bool Convert(const std::string &str, s64 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	s64 value = strtoll(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<s64>(value);
	return true;
}

bool Convert(const std::string &str, u64 *const output)
{
	char *endptr = nullptr;
	errno = 0;

	u64 value = strtoull(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<u64>(value);
	return true;
}

bool Convert(const std::string &str, float *const output)
{
	char *endptr = nullptr;
	errno = 0;

	float value = strtof(str.c_str(), &endptr);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<float>(value);
	return true;
}

bool Convert(const std::string &str, double *const output)
{
	char *endptr = nullptr;
	errno = 0;

	double value = strtod(str.c_str(), &endptr);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<double>(value);
	return true;
}

bool Convert(const std::string &str, bool *const output)
{
	if ("1" == str || !_stricmp("true", str.c_str()))
		*output = true;
	else if ("0" == str || !_stricmp("false", str.c_str()))
		*output = false;
	else
		return false;

	return true;
}

bool Convert(const std::string &str, long *const output)
{
	char *endptr = nullptr;
	errno = 0;

	long value = strtol(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<long>(value);
	return true;
}

bool Convert(const std::string &str, unsigned long *const output)
{
	char *endptr = nullptr;
	errno = 0;

	unsigned long value = strtoul(str.c_str(), &endptr, 0);

	if (!endptr || *endptr)
		return false;

	if (errno == ERANGE)
		return false;

	*output = static_cast<unsigned long>(value);
	return true;
}

std::string UTF16ToUTF8(const std::wstring& input)
{
	auto const size = WideCharToMultiByte(CP_UTF8, 0, input.data(), (int)input.size(), nullptr, 0, nullptr, nullptr);

	std::string output;
	output.resize(size);

	if (size == 0 || size != WideCharToMultiByte(CP_UTF8, 0, input.data(), (int)input.size(), &output[0], (int)output.size(), nullptr, nullptr))
	{
		output.clear();
	}

	return output;
}

std::wstring CPToUTF16(u32 code_page, const std::string& input)
{
	auto const size = MultiByteToWideChar(code_page, 0, input.data(), (int)input.size(), nullptr, 0);

	std::wstring output;
	output.resize(size);

	if (size == 0 || size != MultiByteToWideChar(code_page, 0, input.data(), (int)input.size(), &output[0], (int)output.size()))
	{
		output.clear();
	}

	return output;
}

std::wstring UTF8ToUTF16(const std::string& input)
{
	return CPToUTF16(CP_UTF8, input);
}

std::string SHIFTJISToUTF8(const std::string& input)
{
	return UTF16ToUTF8(CPToUTF16(932, input));
}

std::string CP1252ToUTF8(const std::string& input)
{
	return UTF16ToUTF8(CPToUTF16(1252, input));
}
