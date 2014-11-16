#pragma once

// C++ headers
#include <cstdio>
#include <cstdlib>
#include <string>
#include <memory>

#include <io.h>
#include <fcntl.h> 
#include <windows.h> 

// Windows headers
#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#if _MSC_VER < 1700
#include "mutex.h"
#else
#include <mutex>
#endif

// warning C4127: conditional expression is constant
#pragma warning(disable: 4127)

#if 1
#ifndef CURRENT_MODULE
extern "C" IMAGE_DOS_HEADER __ImageBase;
#define CURRENT_MODULE reinterpret_cast<HMODULE>(&__ImageBase)
#endif

#define INITIALIZE_LOGGER Logger* Logger::m_instance;
class Logger
{
public:
	static Logger* GetInstance()
	{
		if (!m_instance) m_instance = new Logger;
		return m_instance;
	}

	virtual ~Logger()
	{
		if (m_console != nullptr)
		{
			FreeConsole();
			CloseHandle(m_console);
		}

		if (m_file != nullptr)
		{
			CloseHandle(m_file);
		}

        delete m_instance;
	}

	bool file(const char* filename)
	{
		std::string logpath;
		if (PathIsRelativeA(filename))
		{
			char tmp_logpath[MAX_PATH];
			DWORD dwLen = GetModuleFileNameA(CURRENT_MODULE, tmp_logpath, MAX_PATH);
			if (dwLen > 0 && PathRemoveFileSpecA(tmp_logpath))
				PathAppendA(tmp_logpath, filename);
			logpath = tmp_logpath;
		}

		m_file = CreateFileA(logpath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		return m_file != INVALID_HANDLE_VALUE;
	}

	bool console(const char* title = nullptr, const char* console_notice = nullptr)
	{
		//if (AllocConsole())
		{
			m_console = GetStdHandle(STD_OUTPUT_HANDLE);
			if (m_console != INVALID_HANDLE_VALUE)
			{
				ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
				if (title) SetConsoleTitleA(title);
				if (console_notice)
				{
					DWORD len = strlen(console_notice);
					DWORD lenout = 0;
					WriteConsoleA(m_console, console_notice, len, &lenout, NULL);
				}
			}
			return m_console != INVALID_HANDLE_VALUE;
		}
		return false;
	}

	void print(const char* format, va_list vaargs)
	{
		bool log = m_file != INVALID_HANDLE_VALUE;
		bool con = m_console != INVALID_HANDLE_VALUE;

		if ((log || con) && format)
		{
#if _MSC_VER < 1700
			lock_guard lock(m_mtx);
#else
			std::lock_guard<std::mutex> lock(m_mtx);
#endif

			DWORD len = 0;
			DWORD lenout = 0;
			vsnprintf_s(m_print_buffer, 1024, 1024, format, vaargs);
			strncat_s(m_print_buffer, 1024, "\r\n", _TRUNCATE);

			len = strlen(m_print_buffer);
			lenout = 0;

			if (con) WriteConsoleA(m_console, m_print_buffer, len, &lenout, NULL);
			if (log) WriteFile(m_file, m_print_buffer, len, &lenout, NULL);

		}
	}
private:
	static Logger* m_instance;
	// block constructors
	Logger(const Logger& src);
	Logger& operator=(const Logger& rhs);

	char m_print_buffer[1024];

	HANDLE m_console;
	HANDLE m_file;

#if _MSC_VER < 1700
	recursive_mutex m_mtx;
#else
	std::mutex m_mtx;
#endif

	Logger()
	{
		m_file = nullptr;
		m_console = nullptr;
	}
};

inline void LogFile(const char* logname)
{
	Logger::GetInstance()->file(logname);
}

inline void LogConsole(const char* title = nullptr, const char* console_notice = nullptr)
{
	Logger::GetInstance()->console(title, console_notice);
}

inline void PrintLog(const char* format, ...)
{
	va_list vaargs;
	va_start(vaargs, format);
	Logger::GetInstance()->print(format, vaargs);
	va_end(vaargs);
}

#define PrintFunc() PrintLog(__FUNCTION__)
#define PrintFuncSig() PrintLog(__FUNCSIG__)

#else
#define LogFile(logname) logname
#define LogConsole(title) title
#define PrintLog(format, ...) format
#define PrintFunc()
#define PrintFuncSig()

#endif