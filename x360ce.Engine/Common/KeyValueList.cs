using System;
using System.Collections.Generic;

namespace x360ce.Engine
{
	[Serializable]
	public class KeyValueList : List<KeyValue>
	{
		public KeyValueList()
		{
		}

		public void Add<T>(object key, T value, bool encrypt = false)
		{
			var t = typeof(T);
			// If can be serialized automatically then...
			if (t.IsPrimitive && !encrypt)
			{
				base.Add(new KeyValue(key, value));
			}
			else
			{
				// Serialize if not string.
				var stringValue = (typeof(T) == typeof(string))
					? (string)(object)value
					: JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(value);
				// If encryption required then...
				if (encrypt)
				{
					// Encrypt with AES-256...
					stringValue = JocysCom.ClassLibrary.Security.AESHelper.EncryptString(_RandomPassword, stringValue);
				}
				base.Add(new KeyValue(key, stringValue));
			}
		}

		public void AddRandomPassword(string remoteRsaPublicKey)
		{
			// Prepare to encrypt data.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption();
			rsa.RsaPublicKeyValue = remoteRsaPublicKey;
			// Generate random password...
			_RandomPassword = Guid.NewGuid().ToString("N");
			// Encrypt and add random password with RSA...
			var randomPasswordEncrypted = rsa.RsaEncrypt(_RandomPassword);
			Add(CloudKey.RandomPassword, randomPasswordEncrypted);
		}

		public void DecryptRandomPassword(string localRsaPublicKey, string localRsaPrivateKey)
		{
			// Decrypt random password supplied by the user.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption();
			rsa.RsaPublicKeyValue = localRsaPublicKey;
			rsa.RsaPrivateKeyValue = localRsaPrivateKey;
			var randomPasswordEncrypted = GetValue<string>(CloudKey.RandomPassword);
			_RandomPassword = rsa.RsaDecrypt(randomPasswordEncrypted);
		}

		string _RandomPassword;

		public T GetValue<T>(string key, T defaultValue = default(T), bool decrypt = false)
		{
			object v = null;
			var found = false;
			foreach (var item in this)
			{
				if (Equals(item.Key, key))
				{
					v = item.Value;
					found = true;
					break;
				}
			}
			// If value not found or null then...
			if (!found || Equals(v, null))
			{
				// Return default value.
				return defaultValue;
			}
			// If decryption is required.
			if (decrypt)
			{
				// Decrypt value.
				v = JocysCom.ClassLibrary.Security.AESHelper.DecryptString(_RandomPassword, (string)v);
			}
			// If value is string but-non string is wanted then...
			if ((v is string) && typeof(T) != typeof(string))
			{
				// Deserialize.
				v = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<T>((string)v);
			}
			return (T)v;
		}

	}
}
