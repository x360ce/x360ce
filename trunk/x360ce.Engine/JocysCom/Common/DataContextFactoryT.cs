using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading;

namespace JocysCom.WebSites.Engine
{
	/// <summary>
	/// 
	/// </summary>
	public class DataContextFactory<T>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <returns></returns>
		public static TDataContext Instance<TDataContext>()
			where TDataContext : T, new()
		{
			return (TDataContext)InternalInstance(typeof(TDataContext), null, null);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public static TDataContext Instance<TDataContext>(string connectionString)
			where TDataContext : T, new()
		{
			return (TDataContext)InternalInstance(typeof(TDataContext), null, connectionString);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <param name="key"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public static TDataContext Instance<TDataContext>(string key, string connectionString)
			where TDataContext : T, new()
		{
			return (TDataContext)InternalInstance(typeof(TDataContext), key, connectionString);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="key"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		private static object InternalInstance(Type type, string key, string connectionString)
		{
			object context;
			if (HttpContext.Current == null)
			{
				if (connectionString == null)
					context = Activator.CreateInstance(type);
				else
					context = Activator.CreateInstance(type, connectionString);

				return context;
			}
			if (key == null)
				key = String.Format("__BKAYDC_{0}{1}{2}", HttpContext.Current.GetHashCode().ToString("x"), Thread.CurrentContext.ContextID.ToString(), type.ToString());
			context = HttpContext.Current.Items[key];
			if (context == null)
			{
				if (connectionString == null)
					context = Activator.CreateInstance(type);
				else
					context = Activator.CreateInstance(type, connectionString);

				if (context != null)
					HttpContext.Current.Items[key] = context;
			}
			return context;
		}
	}
}
