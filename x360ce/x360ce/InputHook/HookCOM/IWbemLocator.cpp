#include "stdafx.h"

#include <wbemidl.h>
#include "IWbemServices.h"
#include "IWbemLocator.h"

hkIWbemLocator::hkIWbemLocator(IWbemLocator **ppIWbemLocator) {
	m_pWrapped = *ppIWbemLocator;
	*ppIWbemLocator = this;
}

HRESULT STDMETHODCALLTYPE hkIWbemLocator::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject)
{
	return m_pWrapped->QueryInterface(riid, ppvObject);
}

ULONG STDMETHODCALLTYPE hkIWbemLocator::AddRef(void)
{
	return m_pWrapped->AddRef();
}

ULONG STDMETHODCALLTYPE hkIWbemLocator::Release(void)
{
	ULONG count = m_pWrapped->Release();
	if (count == 0) delete this;
	return count;
}

HRESULT STDMETHODCALLTYPE hkIWbemLocator::ConnectServer(
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace)
{
	LogPrint("ConnectServer");
	HRESULT hr = m_pWrapped->ConnectServer(strNetworkResource, strUser, strPassword, strLocale, lSecurityFlags, strAuthority, pCtx, ppNamespace);

	// wrapp IWbemServices
	if (SUCCEEDED(hr)) new hkIWbemServices(ppNamespace); 
	else LogPrint("COMERROR: %X", hr);

	return hr;
}

