#pragma once

class COMDeleter
{
public:
	template <typename T>
	void operator()(T* ptr)
	{
		if (ptr)
		{
			PrintLog("Releasing COM Object %p", ptr);
			ptr->Release();
			ptr = nullptr;
		}
	}
};
