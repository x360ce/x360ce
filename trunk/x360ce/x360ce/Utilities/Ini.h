/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#ifndef _INI_H_
#define _INI_H_

#include "globals.h"
#include <Shlwapi.h>
#include "Utilities\CriticalSection.h"
#include <string.h>
#include <Shlobj.h>

class Ini
{
private:
    void SetLastPrefix(char c)
    {
        m_prefix=c;
    }

    char CheckPrefix(char* str)
    {
        char c = (char) tolower(*str);

        if (c == 'a')
        {
            SetLastPrefix(c);
            return c;
        }
        else if (c == 's')
        {
            SetLastPrefix(c);
            return c;
        }
        else if (c == 'x')
        {
            SetLastPrefix(c);
            return c;
        }
        else if (c == 'h')
        {
            SetLastPrefix(c);
            return c;
        }
        else if (c == 'z')
        {
            SetLastPrefix(c);
            return c;
        }
        else return 0;
    }
public:
    Ini(const char* filename)
    {
        Mutex();
        char buffer[MAX_PATH];
        char path[MAX_PATH];

        // check curret directory
        // get current module (dll) path and strip file specification
        GetModuleFileNameA(NULL, buffer, MAX_PATH);
        PathRemoveFileSpecA(buffer);
        // add file name to buffer
        PathCombineA(path,buffer,filename);
        // check if path exist and is not a directory
        if(PathFileExistsA(path) && PathIsDirectoryA(path) == FALSE) m_inifile = path;

        // if file was not found in current directory do same as above for ProgramData
        if(m_inifile.empty() && SHGetFolderPathA(NULL,CSIDL_COMMON_APPDATA,NULL,SHGFP_TYPE_CURRENT,path) == S_OK)
        {
            //add directory name
            PathCombineA(buffer,path,"x360ce");
            PathCombineA(path,buffer,filename);
            if(PathFileExistsA(path) && PathIsDirectoryA(path) == FALSE) m_inifile = path;
        }
    }
    virtual ~Ini(void) {}

    static CriticalSection& Mutex()
    {
        static CriticalSection mutex;
        return mutex;
    }

    DWORD GetString(const char* strFileSection, const char* strKey, char* strOutput, char* strDefault = 0)
    {
        if(m_inifile.empty()) return 0;

        Mutex().Lock();

        DWORD ret;
        char* pStr;
        ret = GetPrivateProfileStringA(strFileSection, strKey, strDefault, strOutput, MAX_PATH, m_inifile.c_str());

        pStr = strchr(strOutput, L'#');
        if (pStr) *pStr=L'\0';
        pStr = strchr(strOutput, L';');
        if (pStr) *pStr=L'\0';

        Mutex().Unlock();

        return ret;
    }

    long GetLong(const char* strFileSection, const char* strKey, INT iDefault = 0)
    {
        char tmp[MAX_PATH];
        char def[MAX_PATH];
        DWORD ret;

        sprintf_s(def,MAX_PATH,"%d",iDefault);
        ret = GetString(strFileSection,strKey,tmp,def);
        if(ret)
        {
            if(CheckPrefix(tmp)) return strtol(tmp+1,NULL,0);
            else return strtol(tmp,NULL,0);
        }
        return 0;
    }

    DWORD GetDword(const char* strFileSection, const char* strKey, INT iDefault = 0)
    {
        char tmp[MAX_PATH];
        char def[MAX_PATH];
        DWORD ret;

        sprintf_s(def,MAX_PATH,"%d",iDefault);
        ret = GetString(strFileSection,strKey,tmp,def);

        if(ret) return strtoul(tmp,NULL,0);
        return 0;
    }

    bool GetBool(const char* strFileSection, const char* strKey, INT iDefault = 0)
    {
        return GetDword(strFileSection,strKey,iDefault) !=0;
    }

    char GetLastPrefix()
    {
        return m_prefix;
    }

private:
    std::string m_inifile;
    char m_prefix;
};

#endif // _INI_H_
