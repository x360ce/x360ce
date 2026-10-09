#pragma once

#include <windows.h>

class Timer
{
public:
	Timer();
	~Timer();

	void Start();
	void Stop();

	double GetElapsedTime();
	double GetElapsedTimeInSec();
	double GetElapsedTimeInMilliSec();
	double GetElapsedTimeInMicroSec();

private:
	double m_startTimeInMicroSec;
	double m_endTimeInMicroSec;
	bool m_stopped;
	LARGE_INTEGER m_frequency;
	LARGE_INTEGER m_startCount;
	LARGE_INTEGER m_endCount;
};
