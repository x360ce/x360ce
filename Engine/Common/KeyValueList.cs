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

		/// <summary>Add new key and value item to collection.</summary>
		/// <typeparam name="T">Type of the value.</typeparam>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		/// <param name="encrypt">Encrypt value (random password must be set)</param>
		/// <param name="replace">Remove old values with the same key.</param>
		public void Add<T>(object key, T value, bool encrypt = false, bool replace = false)
		{
			var t = typeof(T);
			if (replace)
				RemoveAll(x => Equals(x.Key, key));
			// If can be serialized automatically then...
			if (t.IsPrimitive && !encrypt)
			{
				Add(new KeyValue(key, value));
			}
			else
			{
				// Serialize if not string.
				var stringValue = (typeof(T) == typeof(string))
					? (string)(object)value
					: JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(value);
				// If encryption required then Encrypt with AES-256
				if (encrypt)
					stringValue = JocysCom.ClassLibrary.Security.AESHelper.EncryptString(_RandomPassword, stringValue);
				Add(new KeyValue(key, stringValue));
			}
		}

		// 
		public void UpsertRandomPassword(string remoteRsaPublicKey)
		{
			// Generate random password if not exist for the message.
			_RandomPassword = _RandomPassword ?? Guid.NewGuid().ToString("N");
			// Prepare to encrypt data.
			var rsa = new JocysCom.ClassLibrary.Security.Encryption();
			rsa.RsaPublicKeyValue = remoteRsaPublicKey;
			// Encrypt random password with RSA public key...
			var randomPasswordEncrypted = rsa.RsaEncrypt(_RandomPassword);
			// Reomove old random password if exist.
			RemoveAll(x => Equals(x.Key, CloudKey.RandomPassword));
			// Add new random password to the list.
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
			// If value not found or null then return default value.
			if (!found || Equals(v, null))
				return defaultValue;
			// If decryption is required then decrypt value.
			if (decrypt)
				v = JocysCom.ClassLibrary.Security.AESHelper.DecryptString(_RandomPassword, (string)v);
			// If value is string but-non string is wanted then deserialize.
			if ((v is string) && typeof(T) != typeof(string))
				v = JocysCom.ClassLibrary.Runtime.Serializer.DeserializeFromXmlString<T>((string)v);
			return (T)v;
		}

	}
}
