#include <Xinput.h>

#ifndef VXBOX_API
#define VXBOX_API __declspec(dllimport)
#endif

//////////// Interface Functions /////////////////////////
extern "C"
{

	/// Status
	VXBOX_API BOOL	__cdecl	 isVBusExists(void);
	VXBOX_API BOOL	__cdecl	 GetNumEmptyBusSlots(UCHAR * nSlots);
	VXBOX_API BOOL	__cdecl	 isControllerExists(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 isControllerOwned(UINT UserIndex);


	// Virtual device Plug-In/Unplug
	VXBOX_API BOOL	__cdecl	 PlugIn(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 UnPlug(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 UnPlugForce(UINT UserIndex);

	// Data Transfer (Data to the device)
	VXBOX_API BOOL	__cdecl	 SetBtnA(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnB(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnX(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnY(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnStart(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnBack(UINT UserIndex, BOOL Press);
	VXBOX_API BOOL	__cdecl	 SetBtnLT(UINT UserIndex, BOOL Press); // Left Thumb/Stick
	VXBOX_API BOOL	__cdecl	 SetBtnRT(UINT UserIndex, BOOL Press); // Right Thumb/Stick
	VXBOX_API BOOL	__cdecl	 SetBtnLB(UINT UserIndex, BOOL Press); // Left Bumper
	VXBOX_API BOOL	__cdecl	 SetBtnRB(UINT UserIndex, BOOL Press); // Right Bumper
 	VXBOX_API BOOL	__cdecl	 SetTriggerL(UINT UserIndex, BYTE Value); // Left Trigger
 	VXBOX_API BOOL	__cdecl	 SetTriggerR(UINT UserIndex, BYTE Value); // Right Trigger
	VXBOX_API BOOL	__cdecl	 SetAxisX(UINT UserIndex, SHORT Value); // Left Stick X
	VXBOX_API BOOL	__cdecl	 SetAxisY(UINT UserIndex, SHORT Value); // Left Stick Y
	VXBOX_API BOOL	__cdecl	 SetAxisRx(UINT UserIndex, SHORT Value); // Right Stick X
	VXBOX_API BOOL	__cdecl	 SetAxisRy(UINT UserIndex, SHORT Value); // Right Stick Y
	VXBOX_API BOOL	__cdecl	 SetDpadUp(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 SetDpadRight(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 SetDpadDown(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 SetDpadLeft(UINT UserIndex);
	VXBOX_API BOOL	__cdecl	 SetDpadOff(UINT UserIndex);

	// Data Transfer (Feedback from the device)
	VXBOX_API BOOL	__cdecl	 GetLedNumber(UINT UserIndex, PBYTE pLed);
	VXBOX_API BOOL	__cdecl	 GetVibration(UINT UserIndex, PXINPUT_VIBRATION pVib);

}  // extern "C"
