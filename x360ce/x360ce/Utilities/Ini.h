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
        char c = static_cast<char>(tolower(*str));

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
        else
        {
            SetLastPrefix(0);
            return 0;
        }
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

    uint32_t GetString(const char* strFileSection, const char* strKey, char* strOutput, char* strDefault = 0)
    {
        if(m_inifile.empty()) return 0;

        Mutex().Lock();

        uint32_t ret = GetPrivateProfileStringA(strFileSection, strKey, strDefault, strOutput, MAX_PATH, m_inifile.c_str());

        if(strOutput)
        {
            char* pStr = strchr(strOutput, L'#');
            if (pStr) *pStr=L'\0';

            pStr = strchr(strOutput, L';');
            if (pStr) *pStr=L'\0';
        }

        Mutex().Unlock();

        return ret;
    }

    int32_t GetLong(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        char out[MAX_PATH];
        char def[MAX_PATH];

        sprintf_s(def,MAX_PATH,"%d",iDefault);
        if(GetString(strFileSection,strKey,out,def))
        {
            if(CheckPrefix(out)) return strtol(out+1,NULL,0);
            else return strtol(out,NULL,0);
        }
        return 0;
    }

    int16_t GetShort(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        return static_cast<short>(GetLong(strFileSection,strKey,iDefault));
    }

    int8_t GetSByte(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        return static_cast<int8_t>(GetLong(strFileSection,strKey,iDefault));
    }

    uint32_t GetDword(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        char out[MAX_PATH];
        char def[MAX_PATH];

        sprintf_s(def,MAX_PATH,"%d",iDefault);
        if(GetString(strFileSection,strKey,out,def))
        {
            if(CheckPrefix(out)) return strtoul(out+1,NULL,0);
            else return strtoul(out,NULL,0);
        }
        return 0;
    }

    uint16_t GetWord(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        return static_cast<unsigned short>(GetDword(strFileSection,strKey,iDefault));
    }

    uint8_t GetUByte(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        return static_cast<unsigned char>(GetDword(strFileSection,strKey,iDefault));
    }

    bool GetBool(const char* strFileSection, const char* strKey, int iDefault = 0)
    {
        return GetDword(strFileSection,strKey,iDefault) !=0;
    }

    char GetLastPrefix()
    {
        return m_prefix;
    }

    bool is_open()
    {
        return !m_inifile.empty();
    }

    const char* GetFilename()
    {
        return m_inifile.c_str();
    }

private:
    std::string m_inifile;
    char m_prefix;
};

#endif // _INI_H_
