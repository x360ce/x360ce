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

    static InputHook& GetInputHook()
    {
        static InputHook inputHook;
        return inputHook;
    }
};