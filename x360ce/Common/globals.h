#pragma once

//useful macros
#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }
#define SAFE_DELETE_ARRAY(p) { if(p) { delete[] (p);   (p)=NULL; } }
#define SAFE_FREE(p)  { if(p) { free(p);     (p)=NULL; } }
#define SAFE_RELEASE(p) { if(p) { (p)->Release(); (p)=NULL; } }
#define IN_RANGE(val, min, max) ((val) > (min) && (val) < (max))
#define IN_RANGE2(val, min, max) ((val) >= (min) && (val) <= (max))
#define STRINGIFY(x) #x
#define TOSTRING(x) STRINGIFY(x)

#define FFB_LEFTMOTOR 0
#define FFB_RIGHTMOTOR 1

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

