#include "stdafx.h"

#include <wbemidl.h>
#include "IWbemClassObject.h"

HRESULT HookGet(HRESULT hr, VARIANT **ppVal);

hkIWbemClassObject::hkIWbemClassObject(IWbemClassObject **ppIWbemClassObject) {
	m_pWrapped = *ppIWbemClassObject;
	*ppIWbemClassObject = this;
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject)
{
	return m_pWrapped->QueryInterface(riid, ppvObject);
}

ULONG STDMETHODCALLTYPE hkIWbemClassObject::AddRef(void)
{
	return m_pWrapped->AddRef();
}

ULONG STDMETHODCALLTYPE hkIWbemClassObject::Release(void)
{
	ULONG count = m_pWrapped->Release();
	if (count == 0) delete this;
	return count;
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetQualifierSet(
	/* [out] */ IWbemQualifierSet **ppQualSet)
{
	return m_pWrapped->GetQualifierSet(ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Get(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	HRESULT hr = m_pWrapped->Get(wszName, lFlags, pVal, pType, plFlavor);
	return HookGet(hr, &pVal);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Put(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [in] */ VARIANT *pVal,
	/* [in] */ CIMTYPE Type)
{
	return m_pWrapped->Put(wszName, lFlags, pVal, Type);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Delete(
	/* [string][in] */ LPCWSTR wszName)
{
	return m_pWrapped->Delete(wszName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetNames(
	/* [string][in] */ LPCWSTR wszQualifierName,
	/* [in] */ long lFlags,
	/* [in] */ VARIANT *pQualifierVal,
	/* [out] */ SAFEARRAY * *pNames)
{
	return m_pWrapped->GetNames(wszQualifierName, lFlags, pQualifierVal, pNames);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::BeginEnumeration(
	/* [in] */ long lEnumFlags)
{
	return m_pWrapped->BeginEnumeration(lEnumFlags);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Next(
	/* [in] */ long lFlags,
	/* [unique][in][out] */ BSTR *strName,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	return m_pWrapped->Next(lFlags, strName, pVal, pType, plFlavor);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::EndEnumeration(void)
{
	return m_pWrapped->EndEnumeration();
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetPropertyQualifierSet(
	/* [string][in] */ LPCWSTR wszProperty,
	/* [out] */ IWbemQualifierSet **ppQualSet)
{
	return m_pWrapped->GetPropertyQualifierSet(wszProperty, ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Clone(
	/* [out] */ IWbemClassObject **ppCopy)
{
	return m_pWrapped->Clone(ppCopy);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetObjectText(
	/* [in] */ long lFlags,
	/* [out] */ BSTR *pstrObjectText)
{
	return m_pWrapped->GetObjectText(lFlags, pstrObjectText);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::SpawnDerivedClass(
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppNewClass)
{
	return m_pWrapped->SpawnDerivedClass(lFlags, ppNewClass);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::SpawnInstance(
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppNewInstance)
{
	return m_pWrapped->SpawnInstance(lFlags, ppNewInstance);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::CompareTo(
	/* [in] */ long lFlags,
	/* [in] */ IWbemClassObject *pCompareTo)
{
	return m_pWrapped->CompareTo(lFlags, pCompareTo);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetPropertyOrigin(
	/* [string][in] */ LPCWSTR wszName,
	/* [out] */ BSTR *pstrClassName)
{
	return m_pWrapped->GetPropertyOrigin(wszName, pstrClassName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::InheritsFrom(
	/* [in] */ LPCWSTR strAncestor)
{
	return m_pWrapped->InheritsFrom(strAncestor);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethod(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppInSignature,
	/* [out] */ IWbemClassObject **ppOutSignature)
{
	return m_pWrapped->GetMethod(wszName, lFlags, ppInSignature, ppOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::PutMethod(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [in] */ IWbemClassObject *pInSignature,
	/* [in] */ IWbemClassObject *pOutSignature)
{
	return m_pWrapped->PutMethod(wszName, lFlags, pInSignature, pOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::DeleteMethod(
	/* [string][in] */ LPCWSTR wszName)
{
	return m_pWrapped->DeleteMethod(wszName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::BeginMethodEnumeration(
	/* [in] */ long lEnumFlags)
{
	return m_pWrapped->BeginMethodEnumeration(lEnumFlags);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::NextMethod(
	/* [in] */ long lFlags,
	/* [unique][in][out] */ BSTR *pstrName,
	/* [unique][in][out] */ IWbemClassObject **ppInSignature,
	/* [unique][in][out] */ IWbemClassObject **ppOutSignature)
{
	return m_pWrapped->NextMethod(lFlags, pstrName, ppInSignature, ppOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::EndMethodEnumeration(void)
{
	return m_pWrapped->EndMethodEnumeration();
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethodQualifierSet(
	/* [string][in] */ LPCWSTR wszMethod,
	/* [out] */ IWbemQualifierSet **ppQualSet)
{
	return m_pWrapped->GetMethodQualifierSet(wszMethod, ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethodOrigin(
	/* [string][in] */ LPCWSTR wszMethodName,
	/* [out] */ BSTR *pstrClassName)
{
	return m_pWrapped->GetMethodOrigin(wszMethodName, pstrClassName);
}