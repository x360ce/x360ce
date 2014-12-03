#pragma once

#include "Common.h"
#include "Logger.h"

#ifndef DISABLE_LOGGER
class Logger : NonCopyable
{
public:

    static Logger& GetInstance();
    void Shutdown();

    bool File(const char* filename, const char* commondir);
    bool Console(const char* title = nullptr, const char* console_notice = nullptr);
    void PrintTime(const char* format, ...);
    void Print(const char* format, va_list vaargs);
    void Print(const char* format, ...);
    void DisableTimePrint() { m_print_time = false; };

private:
    static Logger* m_instance;
    char m_buffer[1024];

    SYSTEMTIME m_systime;
    HANDLE m_console;
    HANDLE m_file;
    bool m_print_time;

    Mutex m_mtx;

    Logger() : m_console(0), m_file(0), m_print_time(true) {}
};

inline void LogFile(const char* logname, const char* commondir = nullptr)
{
    Logger::GetInstance().File(logname, commondir);
}

inline void LogConsole(const char* title = nullptr, const char* console_notice = nullptr)
{
    Logger::GetInstance().Console(title, console_notice);
}

inline void PrintLog(const char* format, ...)
{
    va_list vaargs;
    va_start(vaargs, format);
    Logger::GetInstance().Print(format, vaargs);
    va_end(vaargs);
}

inline void LogShutdown()
{
    Logger::GetInstance().Shutdown();
}

inline void DisableTimePrint()
{
    Logger::GetInstance().DisableTimePrint();
}

#else
#define INITIALIZE_LOGGER
#define LogFile(logname)
#define LogConsole(title, notice)
#define PrintLog(format, ...)

#endif