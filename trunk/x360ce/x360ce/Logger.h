#pragma once

// C++ headers
#include <string>
#include <fstream>
#include <sstream>
#include <iomanip>

// Windows headers
#include <windows.h>
#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#include <mutex.h>

// warning C4127: conditional expression is constant
#pragma warning(disable: 4127)

#ifndef CURRENT_MODULE
extern "C" IMAGE_DOS_HEADER __ImageBase;
#define CURRENT_MODULE reinterpret_cast<HMODULE>(&__ImageBase)
#endif

#define INITIALIZE_LOGGER Logger* Logger::m_logger = nullptr; const char* Logger::m_stamp = "[TIME]\t\t\t[THREAD]\t[LOG]\r\n";   

#define LOGMAXBUFFER 1024
#define LOGTIMECHARCOUNT 22
#define LOGSTAMPCOUNT 25

class Logger
{
public:
	static Logger* getInstance()
	{
		if (m_logger == nullptr)
			m_logger = new Logger;

		return m_logger;
	}

	static void resetInstance()
	{
		delete m_logger;
		m_logger = nullptr;
	}

	bool file(const std::string& filename)
	{
		m_file = CreateFileA(toabspath(filename).c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		return m_file != INVALID_HANDLE_VALUE;
	}

	bool console(const char* title = nullptr)
	{
		if (AllocConsole())
		{
			m_console = GetStdHandle(STD_OUTPUT_HANDLE);
			if (m_console != INVALID_HANDLE_VALUE)
			{
				if (title) SetConsoleTitleA(title);
				ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
			}
			return m_console != INVALID_HANDLE_VALUE;
		}
		return false;
	}

	bool is_file()
	{
		return m_file != INVALID_HANDLE_VALUE;
	}

	bool is_console()
	{
		return m_console != INVALID_HANDLE_VALUE;
	}

	void print_console(const char* format, va_list vaargs)
	{
		bool con = m_console != INVALID_HANDLE_VALUE;
		if (con && format)
		{
			lock_guard lock(m_mtx);

			if (!m_printed_stamp) { print_stamp(); m_printed_stamp = true; }

			GetLocalTime(&m_systime);
			print_timestamp(false, con, "%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
				m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());

			vsnprintf_s(m_buffer, LOGMAXBUFFER, LOGMAXBUFFER - LOGTIMECHARCOUNT - 2 - 1, format, vaargs);
			strncat_s(m_buffer, LOGMAXBUFFER, "\r\n", _TRUNCATE);

			size_t len = strlen(m_buffer);
			DWORD lenout = 0;

			if (con) WriteConsoleA(m_console, m_buffer, len, &lenout, NULL);
		}
	}

	void print(const char* format, va_list vaargs)
	{
		bool log = m_file != INVALID_HANDLE_VALUE;
		bool con = m_console != INVALID_HANDLE_VALUE;
		if ((log || con) && format)
		{
			lock_guard lock(m_mtx);
			if (!m_printed_stamp) { print_stamp(); m_printed_stamp = true; }

			GetLocalTime(&m_systime);
			print_timestamp(log, con, "%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
				m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());

			vsnprintf_s(m_buffer, LOGMAXBUFFER, LOGMAXBUFFER - LOGTIMECHARCOUNT - 2 - 1, format, vaargs);
			strncat_s(m_buffer, LOGMAXBUFFER, "\r\n", _TRUNCATE);

			size_t len = strlen(m_buffer);
			DWORD lenout = 0;

			if (con) WriteConsoleA(m_console, m_buffer, len, &lenout, NULL);
			if (log) WriteFile(m_file, m_buffer, len, &lenout, NULL);
		}
	}

private:
	static const char* m_stamp;
	bool m_printed_stamp;

	static Logger* m_logger;

	SYSTEMTIME m_systime;
	HANDLE m_console;
	HANDLE m_file;

	recursive_mutex m_mtx;

	char* m_buffer;

	Logger(const Logger &);
	Logger& operator=(const Logger&);

	Logger()
	{
		m_file = INVALID_HANDLE_VALUE;
		m_console = INVALID_HANDLE_VALUE;

		m_buffer = new char[LOGMAXBUFFER];
	}

	~Logger()
	{
		if (m_console != INVALID_HANDLE_VALUE)
		{
			CloseHandle(m_console);
			FreeConsole();
		}

		if (m_file != INVALID_HANDLE_VALUE)
		{
			CloseHandle(m_file);
		}

		delete[] m_buffer;
	}

	std::string toabspath(const std::string& filename)
	{
		if (PathIsRelativeA(filename.c_str()))
		{
			char logpath[MAX_PATH];

			// get current path
			DWORD dwLen = GetModuleFileNameA(CURRENT_MODULE, logpath, MAX_PATH);
			if (dwLen > 0 && PathRemoveFileSpecA(logpath))
			{
				PathAppendA(logpath, filename.c_str());
			}
			return logpath;
		}
		return filename;
	}

	void print_stamp()
	{
		bool log = m_file != INVALID_HANDLE_VALUE;
		bool con = m_console != INVALID_HANDLE_VALUE;

		DWORD lenout = 0;
		if (con) WriteConsoleA(m_console, m_stamp, LOGSTAMPCOUNT, &lenout, NULL);
		if (log) WriteFile(m_file, m_stamp, LOGSTAMPCOUNT, &lenout, NULL);
	}

	void print_timestamp(bool file, bool console, const char* format, ...)
	{
		if ((file || console) && format)
		{
			char buffer[LOGTIMECHARCOUNT + 1];

			va_list arglist;
			va_start(arglist, format);
			vsprintf_s(buffer, format, arglist);
			va_end(arglist);

			DWORD lenout = 0;

			if (console) WriteConsoleA(m_console, buffer, LOGTIMECHARCOUNT, &lenout, NULL);
			if (file) WriteFile(m_file, buffer, LOGTIMECHARCOUNT, &lenout, NULL);
		}
	}
};


inline void LogFile(const char* logname)
{
	Logger::getInstance()->file(logname);
}

inline void LogConsole(const char* title)
{
	Logger::getInstance()->console(title);
}

inline void LogPrint(const char* format, ...)
{
	va_list vaargs;
	va_start(vaargs, format);
	Logger::getInstance()->print(format, vaargs);
	va_end(vaargs);
}

inline void LogPrintConsole(const char* format, ...)
{
	va_list vaargs;
	va_start(vaargs, format);
	Logger::getInstance()->print_console(format, vaargs);
	va_end(vaargs);
}


inline bool LogIsFile()
{
	return Logger::getInstance()->is_file();
}

inline bool LogIsConsole()
{
	return Logger::getInstance()->is_console();
}

#define PrintFunc LogPrint(__FUNCTION__)
#define PrintFuncSig LogPrint(__FUNCSIG__)
