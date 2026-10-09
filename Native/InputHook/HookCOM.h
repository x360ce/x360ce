#pragma once

#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>

#include <dinput.h>

class InputHookDevice;
class InputHook;

class HookCOM
{
private:
	friend class InputHook;
	static void (WINAPI *TrueCoUninitialize)();
	static HRESULT(WINAPI *TrueCoCreateInstance)(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv);
	static HRESULT(WINAPI *TrueCoCreateInstanceEx)(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults);
	static HRESULT(WINAPI *TrueCoGetClassObject)(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv);
	static HRESULT(STDMETHODCALLTYPE *TrueConnectServer)(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
		const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace);
	static HRESULT(STDMETHODCALLTYPE *TrueCreateInstanceEnum)(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum);
	static HRESULT(STDMETHODCALLTYPE *TrueNext)(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned);
	static HRESULT(STDMETHODCALLTYPE *TrueGet)(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor);

	static HRESULT STDMETHODCALLTYPE HookGet(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor);
	static HRESULT STDMETHODCALLTYPE HookNext(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned);
	static HRESULT STDMETHODCALLTYPE HookCreateInstanceEnum(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum);
	static HRESULT STDMETHODCALLTYPE HookConnectServer(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
		const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace);
	static HRESULT WINAPI HookCoCreateInstanceEx(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults);
	static HRESULT WINAPI HookCoCreateInstance(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv);
	static HRESULT WINAPI HookCoGetClassObject(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv);
	static void WINAPI HookCoUninitialize();

	static void DeviceStringChange(VARIANT *pVal, InputHookDevice* pInputHookDevice, const wchar_t* pNamespace);

	static HRESULT(STDMETHODCALLTYPE *ConnectServer)(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
		const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace);
	static HRESULT(STDMETHODCALLTYPE *CreateInstanceEnum)(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum);
	static HRESULT(STDMETHODCALLTYPE *Next)(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned);
	static HRESULT(STDMETHODCALLTYPE *Get)(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor);
};
