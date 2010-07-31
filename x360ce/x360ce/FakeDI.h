/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 ToCA Edit
*
*  x360ce is free software: you can redistribute it and/or modify it under the terms
*  of the GNU Lesser General Public License as published by the Free Software Found-
*  ation, either version 3 of the License, or (at your option) any later version.
*
*  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
*  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
*  PURPOSE.  See the GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License along with x360ce.
*  If not, see <http://www.gnu.org/licenses/>.
*/

#include "DirectInput.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
extern HRESULT (WINAPI *GenuineDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
extern HRESULT (STDMETHODCALLTYPE *GenuineCreateDevice) (LPDIRECTINPUT8 This, REFGUID rguid, LPDIRECTINPUTDEVICE8 *lplpDirectInputDevice, LPUNKNOWN pUnkOuter);
extern HRESULT (STDMETHODCALLTYPE *GenuineGetProperty) (LPDIRECTINPUTDEVICE8 This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
extern HRESULT (STDMETHODCALLTYPE *GenuineGetDeviceInfo) (LPDIRECTINPUTDEVICE8 This, LPDIDEVICEINSTANCE pdidi);
extern HRESULT (STDMETHODCALLTYPE *GenuineEnumDevices) (LPDIRECTINPUT8 This, DWORD dwDevType,LPDIENUMDEVICESCALLBACK lpCallback,LPVOID pvRef,DWORD dwFlags);
extern LPDIENUMDEVICESCALLBACK lpGenuineCallback;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////