#include "stdafx.h"

#include <wbemidl.h>
#include "IWbemClassObject.h"

HRESULT HookGet(
	HRESULT hr,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor);

hkIWbemClassObject::hkIWbemClassObject(IWbemClassObject **ppIWbemClassObject) {
	m_pWrapped = *ppIWbemClassObject;
	*ppIWbemClassObject = this;
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
{
	m_pWrapped->QueryInterface(riid, ppvObject);
}

ULONG STDMETHODCALLTYPE hkIWbemClassObject::AddRef(void)
{
	m_pWrapped->AddRef();
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
	m_pWrapped->GetQualifierSet(ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Get(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	HRESULT hr = m_pWrapped->Get(wszName, lFlags, pVal, pType, plFlavor);
	return HookGet(hr, wszName, lFlags, pVal, pType, plFlavor);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Put(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [in] */ VARIANT *pVal,
	/* [in] */ CIMTYPE Type)
{
	m_pWrapped->Put(wszName, lFlags, pVal, Type);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Delete(
	/* [string][in] */ LPCWSTR wszName)
{
	m_pWrapped->Delete(wszName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetNames(
	/* [string][in] */ LPCWSTR wszQualifierName,
	/* [in] */ long lFlags,
	/* [in] */ VARIANT *pQualifierVal,
	/* [out] */ SAFEARRAY * *pNames)
{
	m_pWrapped->GetNames(wszQualifierName, lFlags, pQualifierVal, pNames);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::BeginEnumeration(
	/* [in] */ long lEnumFlags)
{
	m_pWrapped->BeginEnumeration(lEnumFlags);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Next(
	/* [in] */ long lFlags,
	/* [unique][in][out] */ BSTR *strName,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	m_pWrapped->Next(lFlags, strName, pVal, pType, plFlavor);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::EndEnumeration(void)
{
	m_pWrapped->EndEnumeration();
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetPropertyQualifierSet(
	/* [string][in] */ LPCWSTR wszProperty,
	/* [out] */ IWbemQualifierSet **ppQualSet)
{
	m_pWrapped->GetPropertyQualifierSet(wszProperty, ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::Clone(
	/* [out] */ IWbemClassObject **ppCopy)
{
	m_pWrapped->Clone(ppCopy);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetObjectText(
	/* [in] */ long lFlags,
	/* [out] */ BSTR *pstrObjectText)
{
	m_pWrapped->GetObjectText(lFlags, pstrObjectText);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::SpawnDerivedClass(
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppNewClass)
{
	m_pWrapped->SpawnDerivedClass(lFlags, ppNewClass);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::SpawnInstance(
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppNewInstance)
{
	m_pWrapped->SpawnInstance(lFlags, ppNewInstance);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::CompareTo(
	/* [in] */ long lFlags,
	/* [in] */ IWbemClassObject *pCompareTo)
{
	m_pWrapped->CompareTo(lFlags, pCompareTo);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetPropertyOrigin(
	/* [string][in] */ LPCWSTR wszName,
	/* [out] */ BSTR *pstrClassName)
{
	m_pWrapped->GetPropertyOrigin(wszName, pstrClassName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::InheritsFrom(
	/* [in] */ LPCWSTR strAncestor)
{
	m_pWrapped->InheritsFrom(strAncestor);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethod(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [out] */ IWbemClassObject **ppInSignature,
	/* [out] */ IWbemClassObject **ppOutSignature)
{
	m_pWrapped->GetMethod(wszName, lFlags, ppInSignature, ppOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::PutMethod(
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [in] */ IWbemClassObject *pInSignature,
	/* [in] */ IWbemClassObject *pOutSignature)
{
	m_pWrapped->PutMethod(wszName, lFlags, pInSignature, pOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::DeleteMethod(
	/* [string][in] */ LPCWSTR wszName)
{
	m_pWrapped->DeleteMethod(wszName);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::BeginMethodEnumeration(
	/* [in] */ long lEnumFlags)
{
	m_pWrapped->BeginMethodEnumeration(lEnumFlags);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::NextMethod(
	/* [in] */ long lFlags,
	/* [unique][in][out] */ BSTR *pstrName,
	/* [unique][in][out] */ IWbemClassObject **ppInSignature,
	/* [unique][in][out] */ IWbemClassObject **ppOutSignature)
{
	m_pWrapped->NextMethod(lFlags, pstrName, ppInSignature, ppOutSignature);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::EndMethodEnumeration(void)
{
	m_pWrapped->EndMethodEnumeration();
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethodQualifierSet(
	/* [string][in] */ LPCWSTR wszMethod,
	/* [out] */ IWbemQualifierSet **ppQualSet)
{
	m_pWrapped->GetMethodQualifierSet(wszMethod, ppQualSet);
}

HRESULT STDMETHODCALLTYPE hkIWbemClassObject::GetMethodOrigin(
	/* [string][in] */ LPCWSTR wszMethodName,
	/* [out] */ BSTR *pstrClassName)
{
	m_pWrapped->GetMethodOrigin(wszMethodName, pstrClassName);
}