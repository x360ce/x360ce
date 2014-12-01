#include "stdafx.h"

#ifndef DISABLE_LOGGER

#include <string>
#include <io.h>
#include <fcntl.h> 
#include <windows.h> 

#include "Utils.h"
#include "Mutex.h"
#include "NonCopyable.h"

#include "Logger.h"

Logger* Logger::m_instance = nullptr;

Logger& Logger::GetInstance()
{
    if (!m_instance) m_instance = new Logger;
    return *m_instance;
};

void Logger::Shutdown()
{
    if (m_console)
        FreeConsole();

    if (m_file)
        CloseHandle(m_file);

    delete m_instance;
}

bool Logger::File(const char* filename)
{
    std::string logpath;
    BuildPath(filename, &logpath, false);

    m_file = CreateFileA(logpath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    return m_file != INVALID_HANDLE_VALUE;
}

bool Logger::Console(const char* title, const char* console_notice)
{
    AllocConsole();

    m_console = GetStdHandle(STD_OUTPUT_HANDLE);
    if (m_console != INVALID_HANDLE_VALUE)
    {
        ShowWindow(GetConsoleWindow(), SW_MAXIMIZE);
        if (title) SetConsoleTitleA(title);
        if (console_notice)
        {
            size_t len = strlen(console_notice);
            DWORD lenout = 0;
            WriteConsoleA(m_console, console_notice, (DWORD)len, &lenout, NULL);
        }
    }
    return m_console != INVALID_HANDLE_VALUE;

    return false;
}

void Logger::PrintTime(const char* format, ...)
{
    bool to_console = m_console != INVALID_HANDLE_VALUE;
    bool to_file = m_file != INVALID_HANDLE_VALUE;

    if ((to_console || to_file) && format)
    {
        va_list arglist;
        va_start(arglist, format);
        vsprintf_s(m_buffer, format, arglist);
        va_end(arglist);

        size_t len = strlen(m_buffer);
        DWORD lenout = 0;

        if (to_console) WriteConsoleA(m_console, m_buffer, (DWORD)len, &lenout, NULL);
        if (to_file) WriteFile(m_file, m_buffer, (DWORD)len, &lenout, NULL);
    }
}

void Logger::Print(const char* format, va_list vaargs)
{
    bool to_console = m_console != INVALID_HANDLE_VALUE;
    bool to_file = m_file != INVALID_HANDLE_VALUE;

    if ((to_console || to_file) && format)
    {
        LockGuard lock(m_mtx);

        size_t len = 0;
        DWORD lenout = 0;
        static char* stamp = "[TIME]\t\t[THREAD]\t[LOG]\n";
        if (stamp)
        {
            len = strlen(stamp);
            if (to_console) WriteConsoleA(m_console, stamp, (DWORD)len, &lenout, NULL);
            if (to_file) WriteFile(m_file, stamp, (DWORD)len, &lenout, NULL);
            stamp = nullptr;
        }

        GetLocalTime(&m_systime);
        PrintTime("%02u:%02u:%02u.%03u\t%08u\t", m_systime.wHour, m_systime.wMinute,
            m_systime.wSecond, m_systime.wMilliseconds, GetCurrentThreadId());

        vsnprintf_s(m_buffer, 1024, 1024, format, vaargs);
        strncat_s(m_buffer, 1024, "\r\n", _TRUNCATE);

        len = strlen(m_buffer);
        lenout = 0;

        if (to_console) WriteConsoleA(m_console, m_buffer, (DWORD)len, &lenout, NULL);
        if (to_file) WriteFile(m_file, m_buffer, (DWORD)len, &lenout, NULL);
    }
}

void Logger::Print(const char* format, ...)
{
    va_list vaargs;
    va_start(vaargs, format);
    Print(format, vaargs);
    va_end(vaargs);
}

#endif 