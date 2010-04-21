namespace Microsoft.Xna.Framework.Input
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct GamePadButtons
    {
        internal ButtonState _a;
        internal ButtonState _b;
        internal ButtonState _x;
        internal ButtonState _y;
        internal ButtonState _leftStick;
        internal ButtonState _rightStick;
        internal ButtonState _leftShoulder;
        internal ButtonState _rightShoulder;
        internal ButtonState _back;
        internal ButtonState _start;
        internal ButtonState _bigButton;
        public GamePadButtons(Buttons buttons)
        {
            this._a = ((buttons & Buttons.A) == Buttons.A) ? ButtonState.Pressed : ButtonState.Released;
            this._b = ((buttons & Buttons.B) == Buttons.B) ? ButtonState.Pressed : ButtonState.Released;
            this._x = ((buttons & Buttons.X) == Buttons.X) ? ButtonState.Pressed : ButtonState.Released;
            this._y = ((buttons & Buttons.Y) == Buttons.Y) ? ButtonState.Pressed : ButtonState.Released;
            this._start = ((buttons & Buttons.Start) == Buttons.Start) ? ButtonState.Pressed : ButtonState.Released;
            this._back = ((buttons & Buttons.Back) == Buttons.Back) ? ButtonState.Pressed : ButtonState.Released;
            this._leftStick = ((buttons & Buttons.LeftStick) == Buttons.LeftStick) ? ButtonState.Pressed : ButtonState.Released;
            this._rightStick = ((buttons & Buttons.RightStick) == Buttons.RightStick) ? ButtonState.Pressed : ButtonState.Released;
            this._leftShoulder = ((buttons & Buttons.LeftShoulder) == Buttons.LeftShoulder) ? ButtonState.Pressed : ButtonState.Released;
            this._rightShoulder = ((buttons & Buttons.RightShoulder) == Buttons.RightShoulder) ? ButtonState.Pressed : ButtonState.Released;
            this._bigButton = ((buttons & Buttons.BigButton) == Buttons.BigButton) ? ButtonState.Pressed : ButtonState.Released;
        }

        public ButtonState A
        {
            get
            {
                return this._a;
            }
        }
        public ButtonState B
        {
            get
            {
                return this._b;
            }
        }
        public ButtonState Back
        {
            get
            {
                return this._back;
            }
        }
        public ButtonState X
        {
            get
            {
                return this._x;
            }
        }
        public ButtonState Y
        {
            get
            {
                return this._y;
            }
        }
        public ButtonState Start
        {
            get
            {
                return this._start;
            }
        }
        public ButtonState LeftShoulder
        {
            get
            {
                return this._leftShoulder;
            }
        }
        public ButtonState LeftStick
        {
            get
            {
                return this._leftStick;
            }
        }
        public ButtonState RightShoulder
        {
            get
            {
                return this._rightShoulder;
            }
        }
        public ButtonState RightStick
        {
            get
            {
                return this._rightStick;
            }
        }
        public ButtonState BigButton
        {
            get
            {
                return this._bigButton;
            }
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((GamePadButtons) obj));
        }

        public override int GetHashCode()
        {
            return Helpers.SmartGetHashCode(this);
        }

        public override string ToString()
        {
            string str = string.Empty;
            if (this._a == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "A";
            }
            if (this._b == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "B";
            }
            if (this._x == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "X";
            }
            if (this._y == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "Y";
            }
            if (this._leftShoulder == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "LeftShoulder";
            }
            if (this._rightShoulder == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "RightShoulder";
            }
            if (this._leftStick == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "LeftStick";
            }
            if (this._rightStick == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "RightStick";
            }
            if (this._start == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "Start";
            }
            if (this._back == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "Back";
            }
            if (this._bigButton == ButtonState.Pressed)
            {
                str = str + ((str.Length != 0) ? " " : "") + "BigButton";
            }
            if (str.Length == 0)
            {
                str = "None";
            }
            return string.Format(CultureInfo.CurrentCulture, "{{Buttons:{0}}}", new object[] { str });
        }

        public static bool operator ==(GamePadButtons left, GamePadButtons right)
        {
            return ((((((left._a == right._a) && (left._b == right._b)) && ((left._x == right._x) && (left._y == right._y))) && (((left._leftShoulder == right._leftShoulder) && (left._leftStick == right._leftStick)) && ((left._rightShoulder == right._rightShoulder) && (left._rightStick == right._rightStick)))) && ((left._back == right._back) && (left._start == right._start))) && (left._bigButton == right._bigButton));
        }

        public static bool operator !=(GamePadButtons left, GamePadButtons right)
        {
            if (((((left._a == right._a) && (left._b == right._b)) && ((left._x == right._x) && (left._y == right._y))) && (((left._leftShoulder == right._leftShoulder) && (left._leftStick == right._leftStick)) && ((left._rightShoulder == right._rightShoulder) && (left._rightStick == right._rightStick)))) && ((left._back == right._back) && (left._start == right._start)))
            {
                return (left._bigButton != right._bigButton);
            }
            return true;
        }
    }
}

