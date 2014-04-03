#pragma once

class hkIWbemLocator : public IWbemLocator {
	IWbemLocator *m_pWrapped;
	
public:
	hkIWbemLocator(IWbemLocator **ppIWbemLocator);
	
	HRESULT STDMETHODCALLTYPE QueryInterface(
		/* [in] */ REFIID riid,
		/* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	HRESULT STDMETHODCALLTYPE ConnectServer( 
            /* [in] */ const BSTR strNetworkResource,
            /* [in] */ const BSTR strUser,
            /* [in] */ const BSTR strPassword,
            /* [in] */ const BSTR strLocale,
            /* [in] */ long lSecurityFlags,
            /* [in] */ const BSTR strAuthority,
            /* [in] */ IWbemContext *pCtx,
            /* [out] */ IWbemServices **ppNamespace);
        
};

