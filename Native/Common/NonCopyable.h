#pragma once

// An inheritable class to disallow the copy constructor and operator= functions
class NonCopyable
{
protected:
	NonCopyable() {}
	NonCopyable(const NonCopyable&&) {}
	void operator=(const NonCopyable&&) {}
private:
	NonCopyable(NonCopyable&);
	NonCopyable& operator=(NonCopyable& other);
};
