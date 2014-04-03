#include "stdafx.h"

#undef GetObject

#include <wbemidl.h>
#include "IEnumWbemClassObject.h"
#include "IWbemServices.h"
#include "IClientSecurity.h"

hkIWbemServices::hkIWbemServices(IWbemServices **ppIWbemLocator) {
	m_pWrapped = *ppIWbemLocator;
	*ppIWbemLocator = this;
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::QueryInterface(
	/* [in] */ REFIID riid,
	/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject)
{
	HRESULT hr = m_pWrapped->QueryInterface(riid, ppvObject);

	if (IsEqualIID(riid, IID_IClientSecurity))
	{
		LogPrint("IID_IClientSecurity");
		IClientSecurity** ppIClientSecurity = reinterpret_cast<IClientSecurity**>(ppvObject);
		new hkIClientSecurity(ppIClientSecurity, &m_pWrapped);
	}
	return hr;
}

ULONG STDMETHODCALLTYPE hkIWbemServices::AddRef(void)
{
	return m_pWrapped->AddRef();
}

ULONG STDMETHODCALLTYPE hkIWbemServices::Release(void)
{
	ULONG count = m_pWrapped->Release();
	if (count == 0) delete this;
	return count;
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::OpenNamespace(
	/* [in] */ __RPC__in const BSTR strNamespace,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemServices **ppWorkingNamespace,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppResult)
{
	return m_pWrapped->OpenNamespace(strNamespace, lFlags, pCtx, ppWorkingNamespace, ppResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::CancelAsyncCall(
	/* [in] */ __RPC__in_opt IWbemObjectSink *pSink)
{
	return m_pWrapped->CancelAsyncCall(pSink);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::QueryObjectSink(
	/* [in] */ long lFlags,
	/* [out] */ __RPC__deref_out_opt IWbemObjectSink **ppResponseHandler)
{
	return m_pWrapped->QueryObjectSink(lFlags, ppResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::GetObject(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppObject,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->GetObject(strObjectPath, lFlags, pCtx, ppObject, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::GetObjectAsync(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->GetObjectAsync(strObjectPath, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::PutClass(
	/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->PutClass(pObject, lFlags, pCtx, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::PutClassAsync(
	/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->PutClassAsync(pObject, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::DeleteClass(
	/* [in] */ __RPC__in const BSTR strClass,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->DeleteClass(strClass, lFlags, pCtx, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::DeleteClassAsync(
	/* [in] */ __RPC__in const BSTR strClass,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->DeleteClassAsync(strClass, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::CreateClassEnum(
	/* [in] */ __RPC__in const BSTR strSuperclass,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	return m_pWrapped->CreateClassEnum(strSuperclass, lFlags, pCtx, ppEnum);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::CreateClassEnumAsync(
	/* [in] */ __RPC__in const BSTR strSuperclass,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->CreateClassEnumAsync(strSuperclass, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::PutInstance(
	/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->PutInstance(pInst, lFlags, pCtx, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::PutInstanceAsync(
	/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->PutInstanceAsync(pInst, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::DeleteInstance(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->DeleteInstance(strObjectPath, lFlags, pCtx, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::DeleteInstanceAsync(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->DeleteInstanceAsync(strObjectPath, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::CreateInstanceEnum(
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	LogPrint("CreateInstanceEnum");
	HRESULT hr = m_pWrapped->CreateInstanceEnum(strFilter, lFlags, pCtx, ppEnum);

	// wrapp IEnumWbemClassObject
	if (SUCCEEDED(hr)) new hkIEnumWbemClassObject(ppEnum);
	else LogPrint("COMERROR: %X", hr);

	return hr;
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::CreateInstanceEnumAsync(
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->CreateInstanceEnumAsync(strFilter, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecQuery(
	/* [in] */ __RPC__in const BSTR strQueryLanguage,
	/* [in] */ __RPC__in const BSTR strQuery,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	return m_pWrapped->ExecQuery(strQueryLanguage, strQuery, lFlags, pCtx, ppEnum);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecQueryAsync(
	/* [in] */ __RPC__in const BSTR strQueryLanguage,
	/* [in] */ __RPC__in const BSTR strQuery,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->ExecQueryAsync(strQueryLanguage, strQuery, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecNotificationQuery(
	/* [in] */ __RPC__in const BSTR strQueryLanguage,
	/* [in] */ __RPC__in const BSTR strQuery,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	return m_pWrapped->ExecNotificationQuery(strQueryLanguage, strQuery, lFlags, pCtx, ppEnum);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecNotificationQueryAsync(
	/* [in] */ __RPC__in const BSTR strQueryLanguage,
	/* [in] */ __RPC__in const BSTR strQuery,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->ExecNotificationQueryAsync(strQueryLanguage, strQuery, lFlags, pCtx, pResponseHandler);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecMethod(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ __RPC__in const BSTR strMethodName,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppOutParams,
	/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult)
{
	return m_pWrapped->ExecMethod(strObjectPath, strMethodName, lFlags, pCtx, pInParams, ppOutParams, ppCallResult);
}

HRESULT STDMETHODCALLTYPE hkIWbemServices::ExecMethodAsync(
	/* [in] */ __RPC__in const BSTR strObjectPath,
	/* [in] */ __RPC__in const BSTR strMethodName,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
	/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler)
{
	return m_pWrapped->ExecMethodAsync(strObjectPath, strMethodName, lFlags, pCtx, pInParams, pResponseHandler);
}
