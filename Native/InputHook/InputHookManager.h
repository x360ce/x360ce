#pragma once

#include "NonCopyable.h"
#include "InputHook.h"

class InputHookManager : NonCopyable
{
public:
	InputHookManager() {}
	~InputHookManager() {};

	static InputHookManager& Get()
	{
		static InputHookManager instance;
		return instance;
	}

	InputHook& GetInputHook()
	{
		return m_inputHook;
	}

private:
	InputHook m_inputHook;
};
