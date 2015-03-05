#include "stdafx.h"

#include <memory>

#include "Utils.h"
#include "IniFile.h"

const std::string& IniFile::NULLSTRING = "";

bool IniFile::Load(const std::string& filename)
{
    sections.clear();
    return FullPathFromPath(&file_path, filename);
}

bool IniFile::Save(const std::string& filename)
{
    std::string outpath;
    FullPathFromPath(&outpath, filename);
    HANDLE hFile = CreateFileA(outpath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

    bool out = false;
    for (auto& sectit = sections.begin(); sectit != sections.end(); ++sectit)
    {
        std::stringstream ss;
        auto commentit = comments.find(sectit->name);
        if (commentit != comments.end())
            ss << commentit->second << std::endl;
        const std::string& keyval = ss.str();
        DWORD outCount;
        WriteFile(hFile, keyval.c_str(), keyval.size(), &outCount, NULL);

        if (sectit->Save(hFile))
            out = true;
    }
    CloseHandle(hFile);
    return out;
}

const IniFile::Section* IniFile::GetSection(const std::string& sectionName) const
{
    for (auto& sectit = sections.begin(); sectit != sections.end(); ++sectit)
        if (!_stricmp(sectit->name.c_str(), sectionName.c_str()))
            return (&(*sectit));
    return nullptr;
}

IniFile::Section* IniFile::GetSection(const std::string& sectionName)
{
    for (auto& sectit = sections.begin(); sectit != sections.end(); ++sectit)
        if (!_stricmp(sectit->name.c_str(), sectionName.c_str()))
            return (&(*sectit));
    return nullptr;
}

IniFile::Section* IniFile::CreateSection(const std::string& sectionName)
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

bool IniFile::DeleteSection(const std::string& sectionName)
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

bool IniFile::Exists(const std::string& sectionName, const std::string& keyName) const
{
    const Section* section = GetSection(sectionName);
    if (!section)
        return false;
    return section->Exists(keyName);
}

bool IniFile::DeleteKey(const std::string& sectionName, const std::string& keyName)
{
    Section* section = GetSection(sectionName);
    if (!section)
        return false;
    return section->Delete(keyName);
}

bool IniFile::GetKeys(const std::string& sectionName, std::vector<std::string>* keys)
{
    const Section* section = CreateSection(sectionName);
    if (!section)
    {
        return false;
    }
    *keys = section->order;
    return true;
}

void IniFile::Sort()
{
    sections.sort();
}

void IniFile::SetComment(const std::string& sectionName, const std::string& commentary)
{
    auto it = comments.find(sectionName);
    if (it != comments.end())
        it->second = commentary;
    else
    {
        comments[sectionName] = commentary;
    }
}

/* IniFile::Section */

bool IniFile::Section::Exists(const std::string& keyName) const
{
    return values.find(keyName) != values.end();
}

bool IniFile::Section::Delete(const std::string& keyName)
{
    auto it = values.find(keyName);
    if (it == values.end())
        return false;

    values.erase(it);
    order.erase(std::find(order.begin(), order.end(), keyName));
    return true;
}

void IniFile::Section::Populate(const std::string& filename)
{
    std::unique_ptr<char[]> data(new char[32767]);

    // read all section data to buffer using WinAPI
    GetPrivateProfileSectionA(name.c_str(), data.get(), 32767, filename.c_str());

    // store pointer for data iteration
    const char* pData = data.get();

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

bool IniFile::Section::Save(HANDLE hFile)
{
    std::stringstream ss;
    ss << "[" << name << "]" << std::endl;
    const std::string& sectionName = ss.str();

    DWORD outCount;
    WriteFile(hFile, sectionName.c_str(), sectionName.size(), &outCount, NULL);

    bool ret = false;

    for (auto& keyit = order.begin(); keyit != order.end(); ++keyit)
    {
        std::stringstream ss;

        auto commentit = comments.find(*keyit);
        if (commentit != comments.end())
            ss << commentit->second << std::endl;

        auto pair = values.find(*keyit);
        ss << pair->first << "=" << pair->second << std::endl;
        const std::string& keyval = ss.str();

        DWORD outCount;
        WriteFile(hFile, keyval.c_str(), keyval.size(), &outCount, NULL);
        ret = keyval.size() == outCount;
    }
    return ret;
}

void IniFile::Section::CleanLine(std::string* str) const
{
    if (!str) return;

    size_t pos = str->find('#');
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

bool IniFile::Section::Get(const std::string& keyName, std::string* value, const std::string& defaultValue)
{
    auto it = values.find(keyName);
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

void IniFile::Section::SetComment(const std::string& keyName, const std::string& commentary)
{
    auto it = comments.find(keyName);
    if (it != comments.end())
        it->second = commentary;
    else
    {
        comments[keyName] = commentary;
    }
}

void IniFile::Section::Set(const std::string& keyName, const std::string& newValue)
{
    auto it = values.find(keyName);
    if (it != values.end())
        it->second = newValue;
    else
    {
        values[keyName] = newValue;
        order.push_back(keyName);
    }
}

