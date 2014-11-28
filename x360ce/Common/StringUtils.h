#pragma once

#include <string>
#include <sstream>

std::string StringFormat(const char* format, ...);
bool ArrayFormatV(char* out, int outsize, const char* format, va_list args);

template<size_t count>
inline void ArrayFormat(char(&out)[count], const char* format, ...)
{
    va_list args;
    va_start(args, format);
    ArrayFormatV(out, count, format, args);
    va_end(args);
}

bool Convert(const std::string &str, bool *output);
bool Convert(const std::string &str, u32 *output);
bool Convert(const std::string &str, u64 *output);

template <typename T>
static bool Convert(const std::string &str, T *const output)
{
    std::istringstream iss(str);

    T tmp = 0;
    if (iss >> tmp)
    {
        *output = tmp;
        return true;
    }
    else
        return false;
}

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

#if _MSC_VER < 1700

namespace std
{
#define _LLFMT	"%I64"

#define _TOSTRING(buf, fmt, val)	\
	sprintf_s(buf, sizeof (buf), fmt, val)

    inline string to_string(int _Val)
    {	// convert int to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, "%d", _Val);
        return (string(_Buf));
    }

    inline string to_string(unsigned int _Val)
    {	// convert unsigned int to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, "%u", _Val);
        return (string(_Buf));
    }

    inline string to_string(long _Val)
    {	// convert long to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, "%ld", _Val);
        return (string(_Buf));
    }

    inline string to_string(unsigned long _Val)
    {	// convert unsigned long to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, "%lu", _Val);
        return (string(_Buf));
    }

    inline string to_string(_Longlong _Val)
    {	// convert long long to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, _LLFMT "d", _Val);
        return (string(_Buf));
    }

    inline string to_string(_ULonglong _Val)
    {	// convert unsigned long long to string
        char _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOSTRING(_Buf, _LLFMT "u", _Val);
        return (string(_Buf));
    }

    inline string to_string(long double _Val)
    {	// convert long double to string
        typedef back_insert_iterator<string> _Iter;
        typedef num_put<char, _Iter> _Nput;
        const _Nput& _Nput_fac = use_facet<_Nput>(locale());
        ostream _Ios((streambuf *)0);
        string _Str;

        _Ios.setf(ios_base::fixed);
        _Nput_fac.put(_Iter(_Str), _Ios, ' ', _Val);
        return (_Str);
    }

    inline string to_string(double _Val)
    {	// convert double to string
        return (to_string((long double)_Val));
    }

    inline string to_string(float _Val)
    {	// convert float to string
        return (to_string((long double)_Val));
    }

    // to_wstring WIDE CONVERSIONS

#define _WLLFMT	L"%I64"

#define _TOWSTRING(buf, fmt, val)	\
	swprintf_s(buf, sizeof (buf) / sizeof (wchar_t), fmt, val)

    inline wstring to_wstring(int _Val)
    {	// convert int to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, L"%d", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(unsigned int _Val)
    {	// convert unsigned int to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, L"%u", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(long _Val)
    {	// convert long to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, L"%ld", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(unsigned long _Val)
    {	// convert unsigned long to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, L"%lu", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(_Longlong _Val)
    {	// convert long long to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, _WLLFMT L"d", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(_ULonglong _Val)
    {	// convert unsigned long long to wstring
        wchar_t _Buf[2 * _MAX_INT_DIG];

        _CSTD _TOWSTRING(_Buf, _WLLFMT L"u", _Val);
        return (wstring(_Buf));
    }

    inline wstring to_wstring(long double _Val)
    {	// convert long double to wstring
        typedef back_insert_iterator<wstring> _Iter;
        typedef num_put<wchar_t, _Iter> _Nput;
        const _Nput& _Nput_fac = use_facet<_Nput>(locale());
        ostream _Ios((streambuf *)0);
        wstring _Str;

        _Ios.setf(ios_base::fixed);
        _Nput_fac.put(_Iter(_Str), _Ios, L' ', _Val);
        return (_Str);
    }

    inline wstring to_wstring(double _Val)
    {	// convert double to wstring
        return (to_wstring((long double)_Val));
    }

    inline wstring to_wstring(float _Val)
    {	// convert float to wstring
        return (to_wstring((long double)_Val));
    }
}
#endif