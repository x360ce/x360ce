#pragma once

#include <vector>

#include "NonCopyable.h"
#include "Controller.h"

class ControllerManager : NonCopyable
{
public:
    ControllerManager() {}
    ~ControllerManager() { GetControllers().clear(); };

    static ControllerManager& Get()
    {
        static ControllerManager instance;
        return instance;
    }

    static std::vector<Controller>& GetControllers()
    {
        static std::vector<Controller> controllers;
        return controllers;
    }
};