using System;
using System.ComponentModel;

/* Unmerged change from project 'Dac.Volante.Mdt.UsbAutorun'
Before:
using System.Text;
using System.Linq;
After:
using System.Linq;
using System.Security.Cryptography;
*/
using System.Security.Cryptography;
using System.Text;

namespace JocysCom.ClassLibrary.Security
{
	/// <summary>
	/// 32-bit CRC hash algorithm.
	/// </summary>
	public class CRC32 : HashAlgorithm
	{
		public readonly uint _Polynomial = 0xedb88320u;
		public readonly uint _Seed = 0xffffffffu;

		uint[] _table;
		uint hash;

		public CRC32()
		{
			Init();
		}

		public CRC32(uint polynomial, uint seed)
		{
			_Polynomial = polynomial;
			_Seed = seed;
			Init();
		}

		void Init()
		{
			hash = _Seed;
			// Initialize table.
			_table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint v = i;
				for (uint j = 0; j < 8; j++)
					v = (v & 1) == 1
					 ? (v >> 1) ^ _Polynomial
					 : v >> 1;
				_table[i] = v;
			}
		}

		public override void Initialize()
		{
			hash = _Seed;
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize)
		{
			for (var i = ibStart; i < ibStart + cbSize; i++)
				hash = (hash >> 8) ^ _table[array[i] ^ hash & 0xff];
		}

		protected override byte[] HashFinal()
		{
			// Get bitwise NOT hash.
			var bytes = BitConverter.GetBytes(~hash);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			HashValue = bytes;
			return bytes;
		}

		public override int HashSize { get { return 32; } }

		public static byte[] GetHash(byte[] bytes)
		{
			var crc = new CRC32();
			crc.Initialize();
			var hash = crc.ComputeHash(bytes);
			crc.Dispose();
			return hash;
		}

	}

	public partial class CRC32Helper
	{

		/// <summary>
		/// var hex = string.Join("", hash.Select(x => x.ToString("X2")));
		/// var u32 = BitConverter.ToUInt32(hash, 0);
		/// </summary>
		public static byte[] ComputeHash(byte[] value)
		{
			var crc = new CRC32();
			crc.Initialize();
			var hash = crc.ComputeHash(value);
			crc.Dispose();
			return hash;
		}

		/// <summary>
		/// var hex = string.Join("", hash.Select(x => x.ToString("X2")));
		/// var u32 = BitConverter.ToUInt32(hash, 0);
		/// </summary>
		public static byte[] ComputeHash(string value, Encoding encoding = null)
		{
			if (encoding is null)
				encoding = Encoding.UTF8;
			var bytes = encoding.GetBytes(value);
			var hash = ComputeHash(bytes);
			return hash;
		}

		public static string GetHashAsString(byte[] bytes)
		{
			var hash = ComputeHash(bytes);
			// Reverse array so it will be compatible with SFV files.
			Array.Reverse(hash);
			var intHash = BitConverter.ToUInt32(hash, 0);
			return string.Format("{0:X8}", intHash);
		}

		public static byte[] GetHashFromFile(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = new CRC32();
			var hash = HashHelper.GetHashFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			return hash;
		}

		public static string GetHashFromFileAsString(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = new CRC32();
			var hash = HashHelper.GetHashFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			// Reverse array so it will be compatible with SFV files.
			Array.Reverse(hash);
			var intHash = BitConverter.ToUInt32(hash, 0);
			return string.Format("{0:X8}", intHash);
		}


	}
}
