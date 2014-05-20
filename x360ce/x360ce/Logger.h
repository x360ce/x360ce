#pragma once

// C++ headers
#include <cstdio>
#include <cstdlib>
#include <string>
#include <memory>
#include <mutex>

#include <io.h>
#include <fcntl.h> 
#include <windows.h> 

// Windows headers
#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

// Local headers
#include <mutex>

// warning C4127: conditional expression is constant
#pragma warning(disable: 4127)

#if 1
#ifndef CURRENT_MODULE
extern "C" IMAGE_DOS_HEADER __ImageBase;
#define CURRENT_MODULE reinterpret_cast<HMODULE>(&__ImageBase)
#endif

#define INITIALIZE_LOGGER std::unique_ptr<Logger> Logger::m_instance; std::once_flag Logger::m_onceFlag;

class Logger
{
public:
	static Logger& GetInstance()
	{
		std::call_once(m_onceFlag,
			[] {
			m_instance.reset(new Logger);
		});
		return *m_instance.get();
	}

	virtual ~Logger()
	{
		if (m_console != nullptr)
		{
			FreeConsole();
			fclose(m_console);
		}

		if (m_file != nullptr)
		{
			fclose(m_file);
		}
	}

	bool file(const char* filename)
	{
		char logpath[MAX_PATH];
		if (PathIsRelativeA(filename))
		{
			DWORD dwLen = GetModuleFileNameA(CURRENT_MODULE, logpath, MAX_PATH);
			if (dwLen > 0 && PathRemoveFileSpecA(logpath))
				PathAppendA(logpath, filename);
			else strncpy_s(logpath, filename, _TRUNCATE);
		}

		m_file = _fsopen(logpath, "wt", _SH_DENYWR);
		return m_file != nullptr;
	}

	bool console(const char* title = nullptr, const char* console_notice = nullptr)
	{
		if (AllocConsole())
		{
			int hConHandle;
			intptr_t lStdHandle;
			
			lStdHandle = (intptr_t) GetStdHandle(STD_OUTPUT_HANDLE);
			hConHandle = _open_osfhandle(lStdHandle, _O_TEXT);

			m_console = _fdopen(hConHandle, "w");
			if (m_console)
			{
				*stdout = *m_console;
				setvbuf(stdout, NULL, _IONBF, 0);

				ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
				if (title) SetConsoleTitle(title);
				if (console_notice) puts(console_notice);
				return true;
			}
		}
		return false;
	}

	void print(const char* format, va_list vaargs)
	{
		bool log = m_file != nullptr;
		bool con = m_console != nullptr;
		if ((log || con) && format)
		{
			std::lock_guard<std::mutex> lock(m_mtx);

			static char* stamp = "[TIME]\t\t[THREAD]\t[LOG]\n";
			if (stamp)
			{
				if (con) printf(stamp);
				if (log) fprintf(m_file, stamp);
				stamp = nullptr;
			}

			GetLocalTime(&m_systime);
			if (con) { 
				printf_s("%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
					m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());
				vprintf_s(format, vaargs); 
				putc('\n', stdout);
			}
			if (log) {
				fprintf_s(m_file, "%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
					m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());
				vfprintf_s(m_file, format, vaargs);
				putc('\n', m_file);  
				fflush(m_file);
			}
		}
	}
private:
	static std::unique_ptr<Logger> m_instance;
	static std::once_flag m_onceFlag;

	// block constructors
	Logger(const Logger& src);
	Logger& operator=(const Logger& rhs);

	SYSTEMTIME m_systime;

	FILE* m_console;
	FILE* m_file;

	std::mutex m_mtx;
	
	Logger()
	{
		m_file = nullptr;
		m_console = nullptr;
	}
};

inline void LogFile(const char* logname)
{
	Logger::GetInstance().file(logname);
}

inline void LogConsole(const char* title = nullptr, const char* console_notice = nullptr)
{
	Logger::GetInstance().console(title, console_notice);
}

inline void PrintLog(const char* format, ...)
{
	va_list vaargs;
	va_start(vaargs, format);
	Logger::GetInstance().print(format, vaargs);
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