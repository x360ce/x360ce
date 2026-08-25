#pragma once

class COMDeleter
{
public:
	template <typename T>
	void operator()(T* ptr)
	{
		ptr->Release();
	}
};
