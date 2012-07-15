#include "stdafx.h"
#include "IniReader.h"

void IniReader::prepare(std::wifstream& inifile)
{
	wchar_t bom_hi;
	wchar_t bom_lo;

	inifile.get(bom_hi);
	inifile.get(bom_lo);

	if(bom_hi == 0xFF && bom_lo == 0xFE) //UTF16LE
		inifile.imbue(std::locale(inifile.getloc(),new std::codecvt_utf16<wchar_t, 1114111UL, std::little_endian>));
	else if(bom_hi == 0xFE && bom_lo == 0xFF) //UTF16BE
		inifile.imbue(std::locale(inifile.getloc(),new std::codecvt_utf16<wchar_t, 1114111UL>));
	else if(bom_hi == 0xEF && bom_lo == 0xBB) //UTF8
	{
		inifile.get(bom_lo);
		if(bom_lo == 0xBF)
			inifile.imbue(std::locale(inifile.getloc(),new std::codecvt_utf8<wchar_t, 1114111UL>));
	}
	else //ANSI
		inifile.seekg(std::ios::beg);
}

inline void IniReader::trim(std::wstring& str)
{
	if(str.empty())
		return;

	size_t pos = std::wstring::npos;

	pos = str.size()-1;
	if(str[pos] == L'\r')
		str.erase(pos,1);

	if(str.empty())
		return;

	pos = str.find(L'#');
	if( pos != std::wstring::npos)
		str.resize(pos);

	pos = str.find(L';');
	if( pos != std::wstring::npos)
		str.resize(pos);

	str.erase(0, str.find_first_not_of(L' '));
	str.erase(str.find_last_not_of(L' ')+1);

	str.shrink_to_fit();
}


IniReader::IniReader()
	:empty_values(1)
{
	std::setlocale(LC_ALL, "en_US.utf8");
}

IniReader::IniReader(const std::wstring& filename, bool skip)
{
	std::setlocale(LC_ALL, "en_US.utf8");
	empty_values = !skip;
	this->open(filename);
}

void IniReader::open(const std::wstring& filename)
{
	std::wifstream inifile;
	inifile.open(filename,std::ios::binary);
	if(!inifile.is_open())
		return;

	this->prepare(inifile);
	this->parse(inifile);
	inifile.close();
}

IniReader::~IniReader(void)
{
}

void IniReader::parse(std::wifstream& inifile)
{
	std::wstring section;
	std::wstring key;
	std::wstring val;

	while(inifile.good())
	{
		std::wstring line;
		std::getline(inifile,line);

		//fast skip comment
		if(line[0] == L'#' || line[0] == L';')
			continue;

		this->trim(line);

		size_t sectionBeg = std::wstring::npos;
		size_t sectionEnd = std::wstring::npos;

		if ((sectionBeg = line.find(L'[')) != std::wstring::npos && (sectionEnd = line.find(L']')) != std::wstring::npos)
			section = line.substr(sectionBeg+1,sectionEnd-1);
		else
		{
			size_t keyBeg = 0;
			size_t keyEnd = line.find(L'=');

			if(keyEnd != std::wstring::npos)
			{
				key = line.substr(keyBeg,keyEnd);

				size_t valBeg = keyEnd+1;
				size_t valEnd = line.size();

				val = line.substr(valBeg,valEnd);
				if(!val.empty() || this->empty_values)
					iniMap[section][key] = val;
			}
		}
	}
}

void IniReader::setSection(const std::wstring& section)
{
	this->selSection = section;
};

void IniReader::clearSection()
{
	this->selSection.clear();
	this->selSection.shrink_to_fit();
};

void IniReader::skipEmpty(bool skip)
{
	this->empty_values = !skip;
}

const std::vector<std::wstring> IniReader::getSections()
{
	std::map <std::wstring, std::map <std::wstring, std::wstring> >::iterator sectionit;
	sectionit = this->iniMap.begin();

	std::vector<std::wstring> sections;

	for(sectionit; sectionit != this->iniMap.end(); ++sectionit)
	{
		sections.push_back(sectionit->first);
	}
	return sections;
}

const std::vector<std::wstring> IniReader::getKeys(const std::wstring& section)
{
	std::map <std::wstring, std::map <std::wstring, std::wstring> >::iterator sectionit;
	std::map <std::wstring, std::wstring>::iterator keyit;

	std::vector<std::wstring> keys;

	sectionit = this->iniMap.find(section);
	if(sectionit != this->iniMap.end())
	{
		keyit = sectionit->second.begin();
		for(keyit; keyit != sectionit->second.end(); ++keyit)
		{
			keys.push_back(keyit->first);
		}
	}
	return keys;
}


const std::wstring IniReader::getValue(const std::wstring& section, const std::wstring& key, const std::wstring& def)
{
	if(this->iniMap.empty())
		return std::wstring();

	std::map <std::wstring, std::map <std::wstring, std::wstring> >::iterator sectionit;
	std::map <std::wstring, std::wstring>::iterator keyit;

	sectionit = this->iniMap.find(section);
	if(sectionit != this->iniMap.end())
	{
		keyit = sectionit->second.find(key);
		if(keyit != sectionit->second.end())
			return keyit->second;;
	}
	return def;
}

const std::wstring IniReader::getValue(const std::wstring& sectionkey, const std::wstring& def)
{
	if(this->iniMap.empty())
		return std::wstring();

	std::wstring section;
	std::wstring key;

	if(sectionkey.find(L".") != std::wstring::npos)
	{
		section = sectionkey.substr(0,sectionkey.find(L"."));
		key = sectionkey.substr(sectionkey.find(L".")+1,sectionkey.length());
	}
	else if(this->selSection.size() > 0)
	{
		section = this->selSection;
		key = sectionkey;
	}
	else return def;

	return this->getValue(section,key,def);
}