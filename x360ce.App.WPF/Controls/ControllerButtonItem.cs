using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace x360ce.App.Controls
{


    public enum ButtonName
    {
        Back,
        Guide,
        Start,
        A_Button,
        B_Button,
        X_Button,
        Y_Button,
        L_Trigger,
        L_Bumper,
        L_Stick_X_Axis,
        L_Stick_Y_Axis,
        L_Stick_Button,
        L_Stick_Up,
        L_Stick_Left,
        L_Stick_Right,
        L_Stick_Down,
        R_Trigger,
        R_Bumper,
        R_Stick_X_Axis,
        R_Stick_Y_Axis,
        R_Stick_Button,
        R_Stick_Up,
        R_Stick_Left,
        R_Stick_Right,
        R_Stick_Down,
        D_Pad,
        D_Pad_Up,
        D_Pad_Left,
        D_Pad_Right,
        D_Pad_Down,
    }

    class ControllerButtonItem
    {
        private string MainName { get; set; }
        private Control MenuControl { get; set; }
        private Control ButtonControl { get; set; }

        public ControllerButtonItem(string mainName, Control menuControl, Control buttonControl)
        {
            MainName = mainName;
            MenuControl = menuControl;
            ButtonControl = buttonControl;
        }
    }
}