#pragma once

#include <string>
#include <list>
#include <unordered_map>
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

class IniFile
{
public:
	class Section
	{
		friend class IniFile;

	public:
		Section() {}
		Section(const std::string& _name) : name(_name) {}

		bool Exists(const std::string& keyName) const;
		bool Delete(const std::string& keyName);

		bool Get(const std::string& keyName, std::string* value, const std::string& defaultValue = NULLSTRING);

		template<typename T>
		bool Get(const std::string& keyName, T* value, const T& defaultValue = 0)
		{
			std::string temp;
			bool retval = Get(keyName, &temp);
			if (retval && Convert(temp, value))
				return true;
			*value = defaultValue;
			return false;
		}

		bool Get(const std::string& keyName, std::vector<std::string>* values);


		void Set(const std::string& keyName, const std::string& newValue);
		void Set(const std::string& keyName, const char* newValue)
		{
			Set(keyName, std::string(newValue));
		}

		template<typename T>
		void Set(const std::string& keyName, const T& newValue)
		{
			Set(keyName, std::to_string(newValue));
		}

		void Set(const std::string& keyName, const bool newValue)
		{
			if (newValue)
				Set(keyName, "true");
			else
				Set(keyName, "false");
		}

		void Set(const std::string& keyName, const std::vector<std::string>& newValues);

		void SetComment(const std::string& keyName, const std::string& commentary);

		bool operator < (const Section& other) const
		{
			return _stricmp(name.c_str(), other.name.c_str()) < 0;
		}

	private:
		std::string name;
		std::map<std::string, std::string, InsensitiveCompare> values;
		std::map<std::string, std::string, InsensitiveCompare> comments;
		std::vector<std::string> order;

		void CleanLine(std::string* str) const;
		void Populate(const std::string& filename);
		bool Save(HANDLE hFile);
	};


	IniFile() { file_path.clear(); }

	bool Load(const std::string& filename);
	bool Save(const std::string& filename);

	bool Save()
	{
		return Save(file_path);
	}

	bool Exists(const std::string& sectionName, const std::string& key) const;
	void Sort();

	bool Get(const std::string& sectionName, const std::string& keyName, std::string* value, const std::string& defaultValue = NULLSTRING)
	{
		return CreateSection(sectionName)->Get(keyName, value, defaultValue);
	}

	template<typename T>
	bool Get(const std::string& sectionName, const std::string& keyName, T* value, const T& defaultValue = 0)
	{
		return CreateSection(sectionName)->Get(keyName, value, defaultValue);
	}

	bool Get(const std::string& sectionName, const std::string& key, std::vector<std::string>* values);

	bool GetKeys(const std::string& sectionName, std::vector<std::string>* keys);

	template<typename T>
	void Set(const std::string& sectionName, const std::string& keyName, const T& newValue)
	{
		return CreateSection(sectionName)->Set(keyName, newValue);
	}

	template<typename T>
	void Set(const std::string& sectionName, const std::string& keyName, const T& newValue, const std::string& commentary)
	{
		CreateSection(sectionName)->Set(keyName, newValue);
		CreateSection(sectionName)->SetComment(keyName, commentary);
	}

	void SetComment(const std::string& sectionName, const std::string& commentary);

	bool DeleteKey(const std::string& sectionName, const std::string& key);
	bool DeleteSection(const std::string& sectionName);

	const std::string& GetIniPath()
	{
		return file_path;
	}

private:
	std::list<Section> sections;
	std::map<std::string, std::string, InsensitiveCompare> comments;

	const Section* GetSection(const std::string& section) const;
	Section* GetSection(const std::string& section);
	Section* CreateSection(const std::string& section);

	static const std::string& NULLSTRING;
	std::string file_path;
};

