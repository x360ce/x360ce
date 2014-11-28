#include "Types.h"
#include "NonCopyable.h"

#include "Logger.h"
#include "Utils.h"
#include "Mutex.h"
#include "StringUtils.h"
#include "SWIP.h"

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

#if _MSC_VER >= 1700
#define NOINLINE __declspec(noinline)
#else
#define NOINLINE
#endif


