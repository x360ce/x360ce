#pragma once

#ifndef CURRENT_MODULE
extern "C" IMAGE_DOS_HEADER __ImageBase;
#define CURRENT_MODULE reinterpret_cast<HMODULE>(&__ImageBase)
#endif

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

