/*  SWIP - Simple Windows Ini Parser
*
*  Copyright (C) 2013-2014 Robert Krawczyk
*
*  SWIP is free software: you can redistribute it and/or modify it under the terms
*  of the GNU Lesser General Public License as published by the Free Software Found-
*  ation, either version 3 of the License, or (at your option) any later version.
*
*  SWIP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
*  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
*  PURPOSE.  See the GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License along with SWIP.
*  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once


#include <string>
#include <vector>
#include <algorithm>
#include <functional>

#include "mutex.h"

// Windows headers
#include <shlwapi.h>
#include <Shlobj.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

#include "Types.h"

// 'identifier' : decorated name length exceeded, name was truncated
#pragma warning(disable: 4503)

// define INI_ORDERED to use map
#ifdef INI_ORDERED
#include <map>
#define MAP_TYPE std::map
#else
#include <unordered_map>
#define MAP_TYPE std::unordered_map
#endif

#define SWIP_BUFFERSIZE 32767

class SWIP
{
public:

    // maps
    typedef MAP_TYPE<std::string, std::string> section_t;
    typedef MAP_TYPE<std::string, section_t> ini_t;

    explicit SWIP()
        :m_inipath(),
        m_inimap()
    {}

    explicit SWIP(const std::string& filename)
        :m_inipath(),
        m_inimap()
    {
        this->open(filename);
    }

    bool open(const std::string& filename)
    {
        if (this->internal_open(filename)) return false;
        if (!PathIsRelativeA(filename.c_str())) return false;

        char buffer[MAX_PATH];
        char path[MAX_PATH];
        if (SHGetFolderPathA(NULL, CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, path) == S_OK)
        {
            PathCombineA(buffer, path, "x360ce");
            PathCombineA(path, buffer, filename.c_str());
            if (PathFileExistsA(path) && PathIsDirectoryA(path) == FALSE) return this->internal_open(path);
            else return false;
        }
        return false;
    }

    virtual ~SWIP(void)
    {}


    bool is_open() const
    {
        return m_is_open;
    }

    const std::string get_inipath() const
    {
        return m_inipath;
    }

    std::string get_string(const std::string& section, const std::string& key, const std::string& def = std::string()) const
    {
        std::string toret = this->internal_get_string(section, key);

        if (toret.empty()) return def;
        else return toret;
    }

    bool get_bool(const std::string& section, const std::string& key, const bool& def = false) const
    {
        // get string, default is not used because conversion are slow!
        std::string strval = this->get_string(section, key);

        // and we can return default if empty here
        if (strval.empty()) return def;

        if (isdigit(strval[0]))
        {
            // convert to bool and return
            return strtol(strval.c_str(), NULL, 0) != 0;
        }
        else
        {
            // "true" and "false" strings
            std::transform(strval.begin(), strval.end(), strval.begin(), tolower);
            if (strval.compare("true") == 0) return true;
        }
        return false;
    }


    s32 get_int(const std::string& section, const std::string& key, const s32& def = 0) const
    {
        // get string, default is not used because conversion are slow!
        const std::string& strval = this->get_string(section, key);

        // and we can return default if empty here
        if (strval.empty()) return def;

        // convert to bool and return
        return strtol(strval.c_str(), NULL, 0);
    }

    u32 get_uint(const std::string& section, const std::string& key, const u32& def = 0) const
    {
        // get string, default is not used because conversion are slow!
        const std::string& strval = this->get_string(section, key);

        // and we can return default if empty here
        if (strval.empty()) return def;

        // convert to bool and return
        return strtoul(strval.c_str(), NULL, 0);
    }

    s64 get_int64(const std::string& section, const std::string& key, const s64& def = 0) const
    {
        // get string, default is not used because conversion are slow!
        const std::string& strval = this->get_string(section, key);

        // and we can return default if empty here
        if (strval.empty()) return def;

        // convert to bool and return
        return _strtoi64(strval.c_str(), NULL, 0);
    }

    u64 get_uint64(const std::string& section, const std::string& key, const u64& def = 0) const
    {
        // get string, default is not used because conversion are slow!
        const std::string& strval = this->get_string(section, key);

        // and we can return default if empty here
        if (strval.empty()) return def;

        // convert to bool and return
        return _strtoui64(strval.c_str(), NULL, 0);
    }

    section_t get_section(const std::string& section) const
    {
        auto secit = m_inimap.find(section);
        if (secit != m_inimap.end())
        {
            return secit->second;
        }
        return section_t();
    }

    bool set_string(const std::string& section, const std::string& key, const std::string& val)
    {
        std::string strval = " " + val;
        m_inimap[section][key] = val;
        return WritePrivateProfileStringA(section.c_str(), key.c_str(), strval.c_str(), m_inipath.c_str()) != 0;
    }

    bool set_bool(const std::string& section, const std::string& key, const bool& val)
    {
        std::string strval;
        if (val) strval = "true";
        else strval = "false";

        if (val) m_inimap[section][key] = strval;
        strval = " " + strval;
        return WritePrivateProfileStringA(section.c_str(), key.c_str(), strval.c_str(), m_inipath.c_str()) != 0;
    }

    bool set_int(const std::string& section, const std::string& key, const s32& val)
    {
        char cstr[2 * _MAX_INT_DIG];
        sprintf_s(cstr, sizeof(cstr), "%d", val);

        std::string strval = cstr;
        m_inimap[section][key] = strval;
        strval = " " + strval;
        return WritePrivateProfileStringA(section.c_str(), key.c_str(), strval.c_str(), m_inipath.c_str()) != 0;
    }

    bool set_uint(const std::string& section, const std::string& key, const u32& val)
    {
        char cstr[2 * _MAX_INT_DIG];
        sprintf_s(cstr, sizeof(cstr), "%u", val);

        std::string strval = cstr;
        m_inimap[section][key] = strval;
        strval = " " + strval;
        return WritePrivateProfileStringA(section.c_str(), key.c_str(), strval.c_str(), m_inipath.c_str()) != 0;
    }

    bool key_exist(const std::string& section, const std::string& key)
    {
        bool toret = this->internal_key_exist(section, key);

        // not found, populate section if needed
        if (toret == false && this->key_count(section) < this->populate_section(section))
        {
            toret = this->internal_key_exist(section, key);
        }
        return toret;
    }

    bool value_not_empty(const std::string& section, const std::string& key)
    {
        return this->get_string(section, key).empty() != true;
    }

    size_t key_count(const std::string& section)
    {
        auto itr = m_inimap.find(section);
        if (itr != m_inimap.end()) return itr->second.size();
        return 0;
    }

    size_t section_count() const
    {
        return m_inimap.size();
    }

private:
    bool internal_open(const std::string& filename)
    {
        // buffer for WinAPI functions
        char path[MAX_PATH];

        // check is path is relative
        if (PathIsRelativeA(filename.c_str()))
        {
            // if path is relative get full path to ini file
            DWORD dwLen = GetModuleFileNameA(g_CurrentModule, path, MAX_PATH);
            if (dwLen > 0 && PathRemoveFileSpecA(path))
            {
                PathAppendA(path, filename.c_str());
                m_inipath = path;

                // check if file exist and is not a directory
                if (PathFileExistsA(m_inipath.c_str())
                    && PathIsDirectoryA(m_inipath.c_str()) == FALSE)
                {
                    return m_is_open = this->populate_ini();
                }
            }
            else return m_is_open = false;
        }
        else
        {
            // if path is absolute copy path
            m_inipath = filename;

            // check if file exist and is not a directory
            if (PathFileExistsA(m_inipath.c_str())
                && PathIsDirectoryA(m_inipath.c_str()) == FALSE)
            {
                return m_is_open = this->populate_ini();
            }
        }
        return m_is_open = false;
    }

    std::string internal_get_string(const std::string& section, const std::string& key) const
    {
        if (m_inimap.empty()) return std::string();

        std::string internal_section = section;
        std::string internal_key = key;

        std::transform(internal_section.begin(), internal_section.end(), internal_section.begin(), tolower);
        std::transform(internal_key.begin(), internal_key.end(), internal_key.begin(), tolower);

        // find section
        auto secit = m_inimap.find(internal_section);
        if (secit != m_inimap.end())
        {
            // find key
            auto keyit = secit->second.find(internal_key);
            if (keyit != secit->second.end())
            {
                // return value
                return keyit->second;
            }
        }
        return std::string();
    }

    bool internal_key_exist(const std::string& section, const std::string& key)
    {
        auto secit = m_inimap.find(section);
        if (secit != m_inimap.end())
        {
            // find key
            auto keyit = secit->second.find(key);
            if (keyit != secit->second.end())
                return true;
        }
        return false;
    }

    size_t populate_section(const std::string& section)
    {
        std::string data;
        data.resize(SWIP_BUFFERSIZE);

        // read all section data to buffer using WinAPI
        GetPrivateProfileSectionA(section.c_str(), &data[0], SWIP_BUFFERSIZE, m_inipath.c_str());

        // store pointer for data iteration
        const char* pData = data.c_str();

        size_t count = 0;

        // last "key=value" string is double null terminated
        while (*pData != '\0')
        {
            // set both, key and val to "key=value" string
            std::string strSection(section);
            std::string strkey(pData);
            std::string strval(pData);

            // get real key and value from "key=value" string
            strkey = strkey.substr(0, strkey.find("="));
            strval = strval.substr(strval.find("=") + 1, strval.length());

            // strip commentary NOTE: WinAPI strip only ';' commentaries at start of line!
            strip_comment_and_trim(&strSection);
            strip_comment_and_trim(&strkey);
            strip_comment_and_trim(&strval);

            // add key/value to correct section, skip empty to save memory
            if (!strSection.empty() && !strkey.empty() && !strval.empty())
            {
                m_inimap[strSection][strkey] = strval;
                ++count;
            }

            // get next "key=value" string
            pData = pData + strlen(pData) + 1;
        }
        return count;
    }

    bool populate_ini()
    {
        // allocate buffers
        std::string data;
        data.resize(SWIP_BUFFERSIZE);

        // read all section names to buffer using WinAPI
        GetPrivateProfileSectionNamesA(&data[0], SWIP_BUFFERSIZE, m_inipath.c_str());

        // store pointer for sections iteration
        const char* pSection = data.c_str();

        // last section name is double null terminated
        while (*pSection != '\0')
        {
            this->populate_section(pSection);
            // get next section name
            pSection = pSection + strlen(pSection) + 1;
        }
        return !m_inimap.empty();
    }

    void strip_comment_and_trim(std::string* str) const
    {
        size_t pos = std::string::npos;

        if (str == nullptr)
            return;

        std::transform(str->begin(), str->end(), str->begin(), tolower);

        pos = str->find('#');
        if (pos != std::string::npos)
            str->resize(pos);

        pos = str->find(';');
        if (pos != std::string::npos)
            str->resize(pos);

        pos = str->find_first_not_of(' ');
        if (pos != std::string::npos)
            str->erase(0, pos);

        pos = str->find_last_not_of(' ') + 1;
        if (pos != std::string::npos)
            str->resize(pos);

        pos = str->find_first_not_of('"');
        if (pos != std::string::npos)
            str->erase(0, pos);

        pos = str->find_last_not_of('"') + 1;
        if (pos != std::string::npos)
            str->resize(pos);

        return;
    }

private:
    std::string m_inipath;
    ini_t m_inimap;
    bool m_is_open;
};

