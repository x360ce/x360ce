using System;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Web.Services
{
	public class SoapHttpClientEventArgs : AsyncCompletedEventArgs
	{
		internal SoapHttpClientEventArgs(object[] results, Exception exception, bool cancelled, object userState) :
			base(exception, cancelled, userState)
		{ _results = results; }

		object[] _results;
		public object Result
		{
			get
			{
				RaiseExceptionIfNecessary();
				return _results[0];
			}
		}
	}

}
