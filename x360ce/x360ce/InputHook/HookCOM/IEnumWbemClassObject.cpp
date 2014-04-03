#include "stdafx.h"

#include <wbemidl.h>
#include "IWbemClassObject.h"
#include "IEnumWbemClassObject.h"

hkIEnumWbemClassObject::hkIEnumWbemClassObject(IEnumWbemClassObject **ppIEnumWbemClassObject) {
	m_pWrapped = *ppIEnumWbemClassObject;
	*ppIEnumWbemClassObject = this;
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
{
	return m_pWrapped->QueryInterface(riid, ppvObject);
}

ULONG STDMETHODCALLTYPE hkIEnumWbemClassObject::AddRef(void)
{
	return m_pWrapped->AddRef();
}

ULONG STDMETHODCALLTYPE hkIEnumWbemClassObject::Release(void)
{
	ULONG count = m_pWrapped->Release();
	if (count == 0) delete this;
	return count;
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::Reset(void)
{
	return m_pWrapped->Reset();
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::Next(
	/* [in] */ long lTimeout,
	/* [in] */ ULONG uCount,
	/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
	/* [out] */ __RPC__out ULONG *puReturned)
{
	HRESULT hr = m_pWrapped->Next(lTimeout, uCount, apObjects, puReturned);

	//wrap IWbemClassObject
	new hkIWbemClassObject(apObjects);
	return hr;
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::NextAsync(
	/* [in] */ ULONG uCount,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pSink)
{
	return m_pWrapped->NextAsync(uCount, pSink);
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::Clone(
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	return m_pWrapped->Clone(ppEnum);
}

HRESULT STDMETHODCALLTYPE hkIEnumWbemClassObject::Skip(
	/* [in] */ long lTimeout,
	/* [in] */ ULONG nCount)
{
	return m_pWrapped->Skip(lTimeout, nCount);
}
