#ifndef _INIREADER_H_
#define _INIREADER_H_

#include <vector>
#include <clocale>
#include <codecvt>

class IniReader
{
public:
	IniReader(const std::wstring& filename, bool skip = true);
	IniReader();
	virtual ~IniReader(void);

	void open(const std::wstring& filename);
	void setSection(const std::wstring& section);
	void clearSection();
	void skipEmpty(bool skip = true);

	const std::vector<std::wstring> getSections();
	const std::vector<std::wstring> getKeys(const std::wstring& section);
	const std::wstring getValue(const std::wstring& section, const std::wstring& key, const std::wstring& def);
	const std::wstring getValue(const std::wstring& sectionkey, const std::wstring& def);

	inline const long getValueAsLong(const std::wstring& keyname, const std::wstring& def)
	{
		return wcstol(this->getValue(keyname,def).c_str(),NULL,0);
	}

	inline const bool getValueAsBoolean(const std::wstring& keyname, const std::wstring& def)
	{
		return this->getValueAsLong(keyname,def) > 0;
	}

private:
	std::map<std::wstring,std::map<std::wstring,std::wstring>> iniMap;
	std::wstring selSection;
	bool empty_values;

	void prepare(std::wifstream& inifile);
	inline void trim(std::wstring& val);
	void parse(std::wifstream& inifile);

};

#endif