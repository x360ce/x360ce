using System;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Security
{
	// Note: If computer is Federal Information Processing Standard (FIPS) compliant and
	// MD5 algorithm is blocked, but you have to use it in .NET for backwards compatibility then
	// you have to add enforceFIPSPolicy option to application/we configuration file.
	// 
	// <configuration>
	//	<runtime>
	//		<!-- Enable MD5 on FIPS-Compliant system for backwards compatibility. -->
	//		<enforceFIPSPolicy enabled="false"/>
	//	</runtime>
	// </configuration>

	/// <summary>
	/// UTF-8 is used as a default encoding (smaller Internet messaging size).
	/// </summary>
	public partial class MD5Helper
	{
		public static Guid GetGuid(string value)
		{
			var algorithm = System.Security.Cryptography.MD5.Create();
			Guid guid = HashHelper.GetGuid(algorithm, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(long value)
		{
			var algorithm = System.Security.Cryptography.MD5.Create();
			Guid guid = HashHelper.GetGuid(algorithm, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] bytes)
		{
			var algorithm = System.Security.Cryptography.MD5.Create();
			Guid guid = HashHelper.GetGuid(algorithm, bytes);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuidFromFile(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = System.Security.Cryptography.MD5.Create();
			Guid guid = HashHelper.GetGuidFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			return guid;
		}

		// HMAC

		public static Guid GetGuid(string key, string value)
		{
			var algorithm = System.Security.Cryptography.HMACMD5.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, long value)
		{
			var algorithm = System.Security.Cryptography.HMACMD5.Create();
			var guid = HashHelper.GetGuid(algorithm, key, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, byte[] value)
		{
			var algorithm = System.Security.Cryptography.HMACMD5.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] key, byte[] value)
		{
			var algorithm = System.Security.Cryptography.HMACMD5.Create();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

	}
}
