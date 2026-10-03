#include "stdafx.h"
#include "Timer.h"

Timer::Timer()
{
	QueryPerformanceFrequency(&m_frequency);
	m_startCount.QuadPart = 0;
	m_endCount.QuadPart = 0;

	m_stopped = 0;
	m_startTimeInMicroSec = 0;
	m_endTimeInMicroSec = 0;
}

Timer::~Timer()
{
}

void Timer::Start()
{
	m_startCount.QuadPart = 0;
	QueryPerformanceCounter(&m_startCount);
}

void Timer::Stop()
{
	m_stopped = true;
	QueryPerformanceCounter(&m_endCount);
}

double Timer::GetElapsedTimeInMicroSec()
{
	if (!m_stopped)
		QueryPerformanceCounter(&m_endCount);

	m_startTimeInMicroSec = m_startCount.QuadPart * (1000000.0 / m_frequency.QuadPart);
	m_endTimeInMicroSec = m_endCount.QuadPart * (1000000.0 / m_frequency.QuadPart);

	return m_endTimeInMicroSec - m_startTimeInMicroSec;
}

double Timer::GetElapsedTimeInMilliSec()
{
	return GetElapsedTimeInMicroSec() * 0.001;
}

double Timer::GetElapsedTimeInSec()
{
	return GetElapsedTimeInMicroSec() * 0.000001;
}

double Timer::GetElapsedTime()
{
	return GetElapsedTimeInSec();
}
