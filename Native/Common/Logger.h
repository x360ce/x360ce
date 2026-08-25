#pragma once

#include <io.h>
#include <fcntl.h>

#include <memory>
#include <string>

#include "NonCopyable.h"
#include "StringUtils.h"
#include "Utils.h"

#ifndef LOGGER_DISABLE
class Logger : NonCopyable
{
public:
	Logger() : m_systime(), m_system(), m_file(INVALID_HANDLE_VALUE) {}

	Logger::~Logger()
	{
		if (m_file)
			CloseHandle(m_file);
	}

	static Logger& Logger::Get()
	{
		static Logger instance;
		return instance;
	};

	bool File(const std::string& filename)
	{
		std::string logpath;
		FullPathFromPath(&logpath, filename);

		m_file = CreateFileA(logpath.c_str(), GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		return m_file != INVALID_HANDLE_VALUE;
	}

	bool System()
	{
		return (m_system = true);
	}

	void Print(const char* format, ...)
	{
		va_list args;
		va_start(args, format);
		Print(format, args);
		va_end(args);
	}

	void Print(const char* format, va_list args)
	{
		bool to_file = m_file != INVALID_HANDLE_VALUE;
		bool to_system = m_system;

		if ((to_file || to_system) && format)
		{
			int outsize = _vscprintf(format, args) + 1;
			std::unique_ptr<char[]> buffer(new char[outsize]);
			CharArrayFormatV(buffer.get(), outsize, format, args);

#ifdef LOGGER_DISABLE_TIME
			std::string to_print(buffer.get(), outsize - 1);
#else
			std::string to_print;
			GetTime(&to_print);
			to_print.append(buffer.get(), outsize - 1);
#endif

			to_print.push_back('\r');
			to_print.push_back('\n');

			DWORD lenout = 0;
			if (to_system) OutputDebugStringA(to_print.c_str());
			if (to_file) WriteFile(m_file, to_print.c_str(), (DWORD)to_print.size(), &lenout, NULL);
		}
	}

private:
	void GetTime(std::string* out)
	{
		static bool write_stamp = true;
		if (write_stamp)
		{
			static char stamp[] = "[TIME]\t\t[THREAD]\t[LOG]\n";
			DWORD lenout = 0;

			bool to_file = m_file != INVALID_HANDLE_VALUE;

			if (to_file) WriteFile(m_file, stamp, _countof(stamp) - 1, &lenout, NULL);
			write_stamp = false;
		}

		GetLocalTime(&m_systime);
		*out = StringFormat("%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
			m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());

	}

	SYSTEMTIME m_systime;

	bool m_system;
	HANDLE m_file;
};

inline void LogFile(const std::string& logname)
{
	Logger::Get().File(logname);
}

inline void LogSystem()
{
	Logger::Get().System();
}

inline void PrintLog(const char* format, ...)
{
	va_list args;
	va_start(args, format);
	Logger::Get().Print(format, args);
	va_end(args);
}

#define PrintFunc PrintLog(__FUNCTION__);

#else
#define LogFile(logname) (logname)
#define LogSystem();
#define PrintLog(format, ...) (format)
#endif
