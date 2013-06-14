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

    char CheckPrefix(const char* str)
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

    std::string& GetString(const char* strSection, const char* strKey, std::string defVal = std::string())
    {
        Mutex().Lock();
        m_strRet.clear();

        if(m_inifile.empty())
        {
            m_strRet = defVal;

            Mutex().Unlock();
            return m_strRet;
        }

        char* strOut = new char[MAX_PATH];
        ULONG32 size = GetPrivateProfileStringA(strSection, strKey, defVal.c_str(), strOut, MAX_PATH, m_inifile.c_str());

        if(size > 0 && *strOut != '\0')
        {
            char* pStr = strchr(strOut, '#');
            if (pStr) *pStr='\0';

            pStr = strchr(strOut, ';');
            if (pStr) *pStr='\0';
        }
        else
        {
            delete [] strOut;
            m_strRet = defVal;

            Mutex().Unlock();
            return m_strRet;
        }
        m_strRet = strOut;
        delete [] strOut;

        Mutex().Unlock();
        return m_strRet;
    }

    int32_t GetLong(const char* strSection, const char* strKey, int32_t defVal = 0)
    {
        char buf[2 * 32];
        sprintf_s(buf, "%ld", defVal);
        std::string str = GetString(strSection, strKey, std::string(buf));

        const char* cstr = str.c_str();
        if(CheckPrefix(cstr)) return strtol(cstr+1,NULL,0);
        else return strtol(cstr,NULL,0);
    }

    int16_t GetShort(const char* strFileSection, const char* strKey, int16_t iDefault = 0)
    {
        return static_cast<int16_t>(GetLong(strFileSection,strKey,iDefault));
    }

    int8_t GetSByte(const char* strFileSection, const char* strKey, int8_t iDefault = 0)
    {
        return static_cast<int8_t>(GetLong(strFileSection,strKey,iDefault));
    }

    uint32_t GetDword(const char* strSection, const char* strKey, uint32_t defVal = 0)
    {
        char buf[2 * 32];
        sprintf_s(buf, "%lu", defVal);
        std::string str = GetString(strSection, strKey, buf);

        const char* cstr = str.c_str();
        if(CheckPrefix(cstr)) return strtoul(cstr+1,NULL,0);
        else return strtoul(cstr,NULL,0);
    }

    uint16_t GetWord(const char* strFileSection, const char* strKey, uint16_t defVal = 0)
    {
        return static_cast<uint16_t>(GetDword(strFileSection,strKey,defVal));
    }

    uint8_t GetUByte(const char* strFileSection, const char* strKey, uint8_t defVal = 0)
    {
        return static_cast<uint8_t>(GetDword(strFileSection,strKey,defVal));
    }

    bool GetBool(const char* strFileSection, const char* strKey, bool defVal = false)
    {
        return GetDword(strFileSection,strKey,defVal) != 0;
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

    std::string m_strRet;
};

#endif // _INI_H_
