#pragma once

class hkIEnumWbemClassObject : public IEnumWbemClassObject {
	IEnumWbemClassObject *m_pWrapped;
	
public:
	hkIEnumWbemClassObject(IEnumWbemClassObject **ppIEnumWbemClassObject);
	
	HRESULT STDMETHODCALLTYPE QueryInterface(
		/* [in] */ REFIID riid,
		/* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	HRESULT STDMETHODCALLTYPE Reset(void);

	HRESULT STDMETHODCALLTYPE Next(
		/* [in] */ long lTimeout,
		/* [in] */ ULONG uCount,
		/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
		/* [out] */ __RPC__out ULONG *puReturned);

	HRESULT STDMETHODCALLTYPE NextAsync(
		/* [in] */ ULONG uCount,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pSink);

	HRESULT STDMETHODCALLTYPE Clone(
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

	HRESULT STDMETHODCALLTYPE Skip(
		/* [in] */ long lTimeout,
		/* [in] */ ULONG nCount);
        
};

