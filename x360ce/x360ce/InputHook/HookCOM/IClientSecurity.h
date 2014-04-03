#pragma once

class hkIClientSecurity : public IClientSecurity {
	IClientSecurity *m_pWrapped;
	IWbemServices *m_pIWbemServices;
	
public:
	hkIClientSecurity(IClientSecurity **ppIClientSecurity, IWbemServices** ppIWbemServices);
	
	HRESULT STDMETHODCALLTYPE QueryInterface(
		/* [in] */ REFIID riid,
		/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	HRESULT STDMETHODCALLTYPE QueryBlanket(
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
		_Out_opt_  DWORD *pCapabilites);

	HRESULT STDMETHODCALLTYPE SetBlanket(
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
		_In_  DWORD dwCapabilities);

	HRESULT STDMETHODCALLTYPE CopyProxy(
		/* [annotation][in] */
		_In_  IUnknown *pProxy,
		/* [annotation][out] */
		_Outptr_  IUnknown **ppCopy);
};

