#pragma once

class hkIWbemClassObject : public IWbemClassObject {
	IWbemClassObject *m_pWrapped;
	
public:
	hkIWbemClassObject(IWbemClassObject **ppIWbemClassObject);
	
	HRESULT STDMETHODCALLTYPE QueryInterface(
		/* [in] */ REFIID riid,
		/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	HRESULT STDMETHODCALLTYPE GetQualifierSet(
		/* [out] */ IWbemQualifierSet **ppQualSet);

	HRESULT STDMETHODCALLTYPE Get(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [unique][in][out] */ VARIANT *pVal,
		/* [unique][in][out] */ CIMTYPE *pType,
		/* [unique][in][out] */ long *plFlavor);

	HRESULT STDMETHODCALLTYPE Put(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [in] */ VARIANT *pVal,
		/* [in] */ CIMTYPE Type);

	HRESULT STDMETHODCALLTYPE Delete(
		/* [string][in] */ LPCWSTR wszName);

	HRESULT STDMETHODCALLTYPE GetNames(
		/* [string][in] */ LPCWSTR wszQualifierName,
		/* [in] */ long lFlags,
		/* [in] */ VARIANT *pQualifierVal,
		/* [out] */ SAFEARRAY * *pNames);

	HRESULT STDMETHODCALLTYPE BeginEnumeration(
		/* [in] */ long lEnumFlags);

	HRESULT STDMETHODCALLTYPE Next(
		/* [in] */ long lFlags,
		/* [unique][in][out] */ BSTR *strName,
		/* [unique][in][out] */ VARIANT *pVal,
		/* [unique][in][out] */ CIMTYPE *pType,
		/* [unique][in][out] */ long *plFlavor);

	HRESULT STDMETHODCALLTYPE EndEnumeration(void);

	HRESULT STDMETHODCALLTYPE GetPropertyQualifierSet(
		/* [string][in] */ LPCWSTR wszProperty,
		/* [out] */ IWbemQualifierSet **ppQualSet);

	HRESULT STDMETHODCALLTYPE Clone(
		/* [out] */ IWbemClassObject **ppCopy);

	HRESULT STDMETHODCALLTYPE GetObjectText(
		/* [in] */ long lFlags,
		/* [out] */ BSTR *pstrObjectText);

	HRESULT STDMETHODCALLTYPE SpawnDerivedClass(
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppNewClass);

	HRESULT STDMETHODCALLTYPE SpawnInstance(
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppNewInstance);

	HRESULT STDMETHODCALLTYPE CompareTo(
		/* [in] */ long lFlags,
		/* [in] */ IWbemClassObject *pCompareTo);

	HRESULT STDMETHODCALLTYPE GetPropertyOrigin(
		/* [string][in] */ LPCWSTR wszName,
		/* [out] */ BSTR *pstrClassName);

	HRESULT STDMETHODCALLTYPE InheritsFrom(
		/* [in] */ LPCWSTR strAncestor);

	HRESULT STDMETHODCALLTYPE GetMethod(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [out] */ IWbemClassObject **ppInSignature,
		/* [out] */ IWbemClassObject **ppOutSignature);

	HRESULT STDMETHODCALLTYPE PutMethod(
		/* [string][in] */ LPCWSTR wszName,
		/* [in] */ long lFlags,
		/* [in] */ IWbemClassObject *pInSignature,
		/* [in] */ IWbemClassObject *pOutSignature);

	HRESULT STDMETHODCALLTYPE DeleteMethod(
		/* [string][in] */ LPCWSTR wszName);

	HRESULT STDMETHODCALLTYPE BeginMethodEnumeration(
		/* [in] */ long lEnumFlags);

	HRESULT STDMETHODCALLTYPE NextMethod(
		/* [in] */ long lFlags,
		/* [unique][in][out] */ BSTR *pstrName,
		/* [unique][in][out] */ IWbemClassObject **ppInSignature,
		/* [unique][in][out] */ IWbemClassObject **ppOutSignature);

	HRESULT STDMETHODCALLTYPE EndMethodEnumeration(void);

	HRESULT STDMETHODCALLTYPE GetMethodQualifierSet(
		/* [string][in] */ LPCWSTR wszMethod,
		/* [out] */ IWbemQualifierSet **ppQualSet);

	HRESULT STDMETHODCALLTYPE GetMethodOrigin(
		/* [string][in] */ LPCWSTR wszMethodName,
		/* [out] */ BSTR *pstrClassName);
        
};

