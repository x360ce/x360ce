#include "stdafx.h"

#include <wbemidl.h>
#include "IClientSecurity.h"

hkIClientSecurity::hkIClientSecurity(IClientSecurity **ppIClientSecurity, IWbemServices** ppIWbemServices) {
	m_pWrapped = *ppIClientSecurity;
	*ppIClientSecurity = this;

	m_pIWbemServices = *ppIWbemServices;
}

HRESULT STDMETHODCALLTYPE hkIClientSecurity::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject)
{
	return m_pWrapped->QueryInterface(riid, ppvObject);
}

ULONG STDMETHODCALLTYPE hkIClientSecurity::AddRef(void)
{
	return m_pWrapped->AddRef();
}

ULONG STDMETHODCALLTYPE hkIClientSecurity::Release(void)
{
	ULONG count = m_pWrapped->Release();
	if (count == 0) delete this;
	return count;
}

HRESULT STDMETHODCALLTYPE hkIClientSecurity::QueryBlanket(
	/* [annotation][in] */
	_In_  IUnknown *pProxy,
	/* [annotation][out] */
	_Out_  DWORD *pAuthnSvc,
	/* [annotation][out] */
	_Out_opt_  DWORD *pAuthzSvc,
	/* [annotation][out] */
	__RPC__deref_out_opt  OLECHAR **pServerPrincName,
	/* [annotation][out] */
	_Out_opt_  DWORD *pAuthnLevel,
	/* [annotation][out] */
	_Out_opt_  DWORD *pImpLevel,
	/* [annotation][out] */
	_Outptr_result_maybenull_  void **pAuthInfo,
	/* [annotation][out] */
	_Out_opt_  DWORD *pCapabilites)
{
	return m_pWrapped->QueryBlanket(m_pIWbemServices, pAuthnSvc, pAuthzSvc, pServerPrincName, pAuthnLevel, pImpLevel, pAuthInfo, pCapabilites);
}

HRESULT STDMETHODCALLTYPE hkIClientSecurity::SetBlanket(
	/* [annotation][in] */
	_In_  IUnknown *pProxy,
	/* [annotation][in] */
	_In_  DWORD dwAuthnSvc,
	/* [annotation][in] */
	_In_  DWORD dwAuthzSvc,
	/* [annotation][in] */
	__RPC__in_opt  OLECHAR *pServerPrincName,
	/* [annotation][in] */
	_In_  DWORD dwAuthnLevel,
	/* [annotation][in] */
	_In_  DWORD dwImpLevel,
	/* [annotation][in] */
	_In_opt_  void *pAuthInfo,
	/* [annotation][in] */
	_In_  DWORD dwCapabilities)
{
	LogPrint("SetBlanket");
	return m_pWrapped->SetBlanket(m_pIWbemServices, dwAuthnSvc, dwAuthzSvc, pServerPrincName, dwAuthnLevel, dwImpLevel, pAuthInfo, dwCapabilities);
}

HRESULT STDMETHODCALLTYPE hkIClientSecurity::CopyProxy(
	/* [annotation][in] */
	_In_  IUnknown *pProxy,
	/* [annotation][out] */
	_Outptr_  IUnknown **ppCopy)
{
	return m_pWrapped->CopyProxy(m_pIWbemServices, ppCopy);
}

