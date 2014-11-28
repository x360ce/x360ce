#pragma once

class Mutex
{
public:
    Mutex()
    {
        InitializeCriticalSection(&cs);
    }

    ~Mutex()
    {
        DeleteCriticalSection(&cs);
    }

    bool TryLock()
    {
        return TryEnterCriticalSection(&cs) != FALSE;
    }

    void Lock()
    {
        EnterCriticalSection(&cs);
    }

    void Unlock()
    {
        LeaveCriticalSection(&cs);
    }

    CRITICAL_SECTION& Get()
    {
        return cs;
    }

private:
    CRITICAL_SECTION cs;
    Mutex(const Mutex& other);
};

class LockGuard : NonCopyable
{
public:
    LockGuard(Mutex& mtx) : mtx_(mtx) { mtx_.Lock(); }
    ~LockGuard() { mtx_.Unlock(); }

private:
    Mutex &mtx_;
};
