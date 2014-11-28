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

#include "stdafx.h"

#include "Utils.h"
#include "SWIP.h"

const std::string& SWIP::NULLSTRING = "";

bool SWIP::Load(const std::string& filename)
{
    return BuildPath(filename, &file_path);
}

bool SWIP::Save(const std::string& filename)
{
    bool out = false;
    for (Section& section : sections)
    {
        out = section.Save(filename);
    }
    return out;
}

const SWIP::Section* SWIP::GetSection(const std::string& sectionName) const
{
    for (const Section& sect : sections)
        if (!_stricmp(sect.name.c_str(), sectionName.c_str()))
            return (&(sect));
    return nullptr;
}

SWIP::Section* SWIP::GetSection(const std::string& sectionName)
{
    for (Section& sect : sections)
        if (!_stricmp(sect.name.c_str(), sectionName.c_str()))
            return (&(sect));
    return nullptr;
}

SWIP::Section* SWIP::CreateSection(const std::string& sectionName)
{
    Section* section = GetSection(sectionName);
    if (!section)
    {
        sections.push_back(Section(sectionName));
        section = &sections.back();
        section->Populate(file_path);
    }
    return section;
}

bool SWIP::DeleteSection(const std::string& sectionName)
{
    Section* s = GetSection(sectionName);
    if (!s)
        return false;
    for (auto iter = sections.begin(); iter != sections.end(); ++iter)
    {
        if (&(*iter) == s)
        {
            sections.erase(iter);
            return true;
        }
    }
    return false;
}

bool SWIP::Exists(const std::string& sectionName, const std::string& key) const
{
    const Section* section = GetSection(sectionName);
    if (!section)
        return false;
    return section->Exists(key);
}

bool SWIP::DeleteKey(const std::string& sectionName, const std::string& key)
{
    Section* section = GetSection(sectionName);
    if (!section)
        return false;
    return section->Delete(key);
}

bool SWIP::GetKeys(const std::string& sectionName, std::vector<std::string>* keys) const
{
    const Section* section = GetSection(sectionName);
    if (!section)
    {
        return false;
    }
    *keys = section->order;
    return true;
}

void SWIP::Sort()
{
    sections.sort();
}

/* SWIP::Section */

bool SWIP::Section::Exists(const std::string& key) const
{
    return values.find(key) != values.end();
}

bool SWIP::Section::Delete(const std::string& key)
{
    auto it = values.find(key);
    if (it == values.end())
        return false;

    values.erase(it);
    order.erase(std::find(order.begin(), order.end(), key));
    return true;
}

void SWIP::Section::Populate(const std::string& filename)
{
    std::string data;
    data.resize(32767);

    // read all section data to buffer using WinAPI
    GetPrivateProfileSectionA(name.c_str(), &data[0], 32767, filename.c_str());

    // store pointer for data iteration
    const char* pData = data.c_str();

    // last "key=value" string is double null terminated
    while (*pData != '\0')
    {
        // set both, key and val to "key=value" string
        std::string strkey(pData);
        std::string strval(pData);

        // get real key and value from "key=value" string
        strkey = strkey.substr(0, strkey.find("="));
        strval = strval.substr(strval.find("=") + 1, strval.size());

        // strip commentary NOTE: WinAPI strip only ';' commentaries at start of line!
        CleanLine(&strkey);
        CleanLine(&strval);

        // add key/value, skip empty to save memory
        if (!strkey.empty() && !strval.empty())
        {
            Set(strkey, strval);
        }

        // get next "key=value" string
        pData = pData + strlen(pData) + 1;
    }
}

bool SWIP::Section::Save(const std::string& filename)
{
    std::stringstream ss;
    for (const std::string& kvit : order)
    {
        auto pair = values.find(kvit);
        ss << pair->first << "=" << pair->second;
        ss << '\0';
    }
    ss << '\0';

    const std::string& keyval = ss.str();

    std::string outpath;
    BuildPath(filename, &outpath, false);

    HANDLE hFile = CreateFileA(outpath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
    CloseHandle(hFile);

    return WritePrivateProfileSectionA(name.c_str(), keyval.c_str(), outpath.c_str()) != 0;
}

void SWIP::Section::CleanLine(std::string* str) const
{
    if (!str) return;

    size_t pos = std::string::npos;

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

bool SWIP::Section::Get(const std::string& key, std::string* value, const std::string& defaultValue)
{
    auto it = values.find(key);
    if (it != values.end())
    {
        *value = it->second;
        return true;
    }
    else if (&defaultValue != &NULLSTRING)
    {
        *value = defaultValue;
        return true;
    }
    else
        return false;
}

bool SWIP::Section::Get(const std::string& key, bool* value, bool defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, s8* value, s8 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, u8* value, u8 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, s16* value, s16 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, u16* value, u16 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, s32* value, s32 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, u32* value, u32 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, s64* value, s64 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, u64* value, u64 defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, float* value, float defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

bool SWIP::Section::Get(const std::string& key, double* value, double defaultValue)
{
    std::string temp;
    bool retval = Get(key, &temp);
    if (retval && Convert(temp, value))
        return true;
    *value = defaultValue;
    return false;
}

void SWIP::Section::Set(const std::string& key, const std::string& newValue)
{
    auto it = values.find(key);
    if (it != values.end())
        it->second = newValue;
    else
    {
        values[key] = newValue;
        order.push_back(key);
    }
}

void SWIP::Section::Set(const std::string& key, const std::string& newValue, const std::string& defaultValue)
{
    if (newValue != defaultValue)
        Set(key, newValue);
    else
        Delete(key);
}
