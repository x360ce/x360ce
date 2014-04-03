#pragma once

class hkIWbemServices : public IWbemServices {
	IWbemServices *m_pWrapped;
	
public:
	hkIWbemServices(IWbemServices **ppIWbemLocator);
	
	HRESULT STDMETHODCALLTYPE QueryInterface(
		/* [in] */ REFIID riid,
		/* [iid_is][out] */ __RPC__deref_out void __RPC_FAR *__RPC_FAR *ppvObject);

	ULONG STDMETHODCALLTYPE AddRef(void);

	ULONG STDMETHODCALLTYPE Release(void);

	HRESULT STDMETHODCALLTYPE OpenNamespace(
		/* [in] */ __RPC__in const BSTR strNamespace,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemServices **ppWorkingNamespace,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppResult);

	HRESULT STDMETHODCALLTYPE CancelAsyncCall(
		/* [in] */ __RPC__in_opt IWbemObjectSink *pSink);

	HRESULT STDMETHODCALLTYPE QueryObjectSink(
		/* [in] */ long lFlags,
		/* [out] */ __RPC__deref_out_opt IWbemObjectSink **ppResponseHandler);

	HRESULT STDMETHODCALLTYPE GetObject(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppObject,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE GetObjectAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE PutClass(
		/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE PutClassAsync(
		/* [in] */ __RPC__in_opt IWbemClassObject *pObject,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE DeleteClass(
		/* [in] */ __RPC__in const BSTR strClass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE DeleteClassAsync(
		/* [in] */ __RPC__in const BSTR strClass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE CreateClassEnum(
		/* [in] */ __RPC__in const BSTR strSuperclass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

	HRESULT STDMETHODCALLTYPE CreateClassEnumAsync(
		/* [in] */ __RPC__in const BSTR strSuperclass,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE PutInstance(
		/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE PutInstanceAsync(
		/* [in] */ __RPC__in_opt IWbemClassObject *pInst,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE DeleteInstance(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE DeleteInstanceAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE CreateInstanceEnum(
		/* [in] */ __RPC__in const BSTR strFilter,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

	HRESULT STDMETHODCALLTYPE CreateInstanceEnumAsync(
		/* [in] */ __RPC__in const BSTR strFilter,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE ExecQuery(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

	HRESULT STDMETHODCALLTYPE ExecQueryAsync(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE ExecNotificationQuery(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

	HRESULT STDMETHODCALLTYPE ExecNotificationQueryAsync(
		/* [in] */ __RPC__in const BSTR strQueryLanguage,
		/* [in] */ __RPC__in const BSTR strQuery,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);

	HRESULT STDMETHODCALLTYPE ExecMethod(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ __RPC__in const BSTR strMethodName,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemClassObject **ppOutParams,
		/* [unique][in][out] */ __RPC__deref_opt_inout_opt IWbemCallResult **ppCallResult);

	HRESULT STDMETHODCALLTYPE ExecMethodAsync(
		/* [in] */ __RPC__in const BSTR strObjectPath,
		/* [in] */ __RPC__in const BSTR strMethodName,
		/* [in] */ long lFlags,
		/* [in] */ __RPC__in_opt IWbemContext *pCtx,
		/* [in] */ __RPC__in_opt IWbemClassObject *pInParams,
		/* [in] */ __RPC__in_opt IWbemObjectSink *pResponseHandler);
        
};

