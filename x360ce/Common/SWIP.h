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
#include <list>
#include <map>
#include <vector>

#include <Windows.h>
#include "Types.h"
#include "StringUtils.h"

struct InsensitiveCompare
{
    bool operator() (const std::string& a, const std::string& b) const
    {
        return _stricmp(a.c_str(), b.c_str()) < 0;
    }
};

class SWIP
{
public:
    class Section
    {
        friend class SWIP;

    public:
        Section() {}
        Section(const std::string& _name) : name(_name) {}

        bool Exists(const std::string& key) const;
        bool Delete(const std::string& key);

        bool Get(const std::string& key, std::string* value, const std::string& defaultValue = NULLSTRING);
        bool Get(const std::string& key, s8* value, s8 defaultValue = 0);
        bool Get(const std::string& key, u8* value, u8 defaultValue = 0);
        bool Get(const std::string& key, s16* value, s16 defaultValue = 0);
        bool Get(const std::string& key, u16* value, u16 defaultValue = 0);
        bool Get(const std::string& key, s32* value, s32 defaultValue = 0);
        bool Get(const std::string& key, u32* value, u32 defaultValue = 0);
        bool Get(const std::string& key, s64* value, s64 defaultValue = 0);
        bool Get(const std::string& key, u64* value, u64 defaultValue = 0);
        bool Get(const std::string& key, bool* value, bool defaultValue = false);
        bool Get(const std::string& key, float* value, float defaultValue = false);
        bool Get(const std::string& key, double* value, double defaultValue = false);

        bool Get(const std::string& key, std::vector<std::string>* values);

        void Set(const std::string& key, const std::string& newValue);
        void Set(const std::string& key, const std::string& newValue, const std::string& defaultValue);

        void Set(const std::string& key, s32 newValue)
        {            
            Set(key, std::to_string(newValue));
        }

        void Set(const std::string& key, u32 newValue)
        {
            Set(key, std::to_string(newValue));
        }

        void Set(const std::string& key, float newValue)
        {
            Set(key, std::to_string(newValue));
        }

        void Set(const std::string& key, double newValue)
        {
            Set(key, std::to_string(newValue));
        }

        void Set(const std::string& key, bool newValue)
        {
            Set(key, std::to_string(newValue));

        }

        template<typename T>
        void Set(const std::string& key, T newValue, const T defaultValue)
        {
            if (newValue != defaultValue)
                Set(key, newValue);
            else
                Delete(key);
        }

        void Set(const std::string& key, const std::vector<std::string>& newValues);

        bool operator < (const Section& other) const
        {
            return name < other.name;
        }

    protected:
        std::string name;
        std::map<std::string, std::string, InsensitiveCompare> values;
        std::vector<std::string> order;

        void CleanLine(std::string* str) const;
        void Populate(const std::string& filename);
        bool Save(const std::string& filename);
    };


    SWIP() { file_path.clear(); }

    bool Load(const std::string& filename);
    bool Save(const std::string& filename);

    bool Exists(const std::string& sectionName, const std::string& key) const;
    void Sort();

    bool Get(const std::string& sectionName, const std::string& key, std::string* value, const std::string& defaultValue = NULLSTRING)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, s8* value, s8 defaultValue = 0)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, u8* value, u8 defaultValue = 0)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, s16* value, s16 defaultValue = 0)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, u16* value, u16 defaultValue = 0)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, s32* value, s32 defaultValue = 0L)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, u32* value, u32 defaultValue = 0UL)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, s64* value, s64 defaultValue = 0LL)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, u64* value, u64 defaultValue = 0ULL)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, bool* value, bool defaultValue = false)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, float* value, float defaultValue = false)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, double* value, double defaultValue = false)
    {
        return CreateSection(sectionName)->Get(key, value, defaultValue);
    }

    bool Get(const std::string& sectionName, const std::string& key, std::vector<std::string>* values);

    bool GetKeys(const std::string& sectionName, std::vector<std::string>* keys) const;

    bool DeleteKey(const std::string& sectionName, const std::string& key);
    bool DeleteSection(const std::string& sectionName);

    const std::string& GetIniPath()
    {
        return file_path;
    }

private:
    std::list<Section> sections;

    const Section* GetSection(const std::string& section) const;
    Section* GetSection(const std::string& section);
    Section* CreateSection(const std::string& section);

    static const std::string& NULLSTRING;
    std::string file_path;
};

