#nullable disable

using System;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.Security
{
	// Note: The HMAC methods below use .NET's own HMACMD5. On a computer with the Windows policy
	// "System cryptography: Use FIPS compliant algorithms" switched on, .NET Framework refuses it,
	// unless the application/web configuration file has the enforceFIPSPolicy option:
	//
	// <configuration>
	//	<runtime>
	//		<!-- Enable MD5 on FIPS-Compliant system for backwards compatibility. -->
	//		<enforceFIPSPolicy enabled="false"/>
	//	</runtime>
	// </configuration>
	//
	// The other methods use MD5Helper.Managed, which that policy does not block.

	/// <summary>
	/// UTF-8 is used as a default encoding (smaller Internet messaging size).
	/// </summary>
	public partial class MD5Helper
	{
		public static Guid GetGuid(string value)
		{
			var algorithm = new Managed();
			Guid guid = HashHelper.GetGuid(algorithm, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(long value)
		{
			var algorithm = new Managed();
			Guid guid = HashHelper.GetGuid(algorithm, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] bytes)
		{
			var algorithm = new Managed();
			Guid guid = HashHelper.GetGuid(algorithm, bytes);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuidFromFile(string path, object sender = null, ProgressChangedEventHandler progressHandler = null, RunWorkerCompletedEventHandler completedHandler = null)
		{
			var algorithm = new Managed();
			Guid guid = HashHelper.GetGuidFromFile(algorithm, path, sender, progressHandler, completedHandler);
			algorithm.Dispose();
			return guid;
		}

		/// <summary>
		/// MD5 (RFC 1321) computed in managed code, giving the same bytes as .NET's own MD5.
		/// </summary>
		/// <remarks>
		/// MD5 is kept for identifiers and checksums that are already stored, never to protect anything.
		/// With the Windows policy "System cryptography: Use FIPS compliant algorithms" switched on,
		/// .NET Framework refuses its own MD5 classes, and a program that makes such an identifier
		/// stops at start-up. This class is not one of them, so the policy does not block it, and every
		/// value stored before stays the same.
		/// </remarks>
		public sealed class Managed : System.Security.Cryptography.MD5
		{
			/// <summary>The sine-derived constant for each of the 64 steps.</summary>
			static readonly uint[] K =
			{
				0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee, 0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
				0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be, 0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
				0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa, 0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
				0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed, 0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
				0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c, 0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
				0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05, 0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
				0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039, 0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
				0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1, 0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391,
			};

			/// <summary>The left rotation of each of the 64 steps.</summary>
			static readonly int[] S =
			{
				7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
				5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
				4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
				6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21,
			};

			readonly uint[] _state = new uint[4];
			readonly uint[] _words = new uint[16];
			/// <summary>Bytes waiting for a full 64-byte block.</summary>
			readonly byte[] _block = new byte[64];
			int _blockLength;
			/// <summary>Bytes hashed so far, which the last block records.</summary>
			long _length;

			public Managed()
			{
				Initialize();
			}

			public override void Initialize()
			{
				_state[0] = 0x67452301;
				_state[1] = 0xefcdab89;
				_state[2] = 0x98badcfe;
				_state[3] = 0x10325476;
				_blockLength = 0;
				_length = 0;
			}

			protected override void HashCore(byte[] array, int ibStart, int cbSize)
			{
				_length += cbSize;
				while (cbSize > 0)
				{
					var count = Math.Min(64 - _blockLength, cbSize);
					Buffer.BlockCopy(array, ibStart, _block, _blockLength, count);
					_blockLength += count;
					ibStart += count;
					cbSize -= count;
					if (_blockLength == 64)
					{
						Transform();
						_blockLength = 0;
					}
				}
			}

			protected override byte[] HashFinal()
			{
				// One 0x80 byte, zeros up to 8 bytes short of a block, then the length in bits.
				var bits = (ulong)_length * 8;
				var padding = new byte[(_blockLength < 56 ? 56 : 120) - _blockLength + 8];
				padding[0] = 0x80;
				for (var i = 0; i < 8; i++)
					padding[padding.Length - 8 + i] = (byte)(bits >> (8 * i));
				HashCore(padding, 0, padding.Length);
				var hash = new byte[16];
				for (var i = 0; i < 16; i++)
					hash[i] = (byte)(_state[i / 4] >> (8 * (i % 4)));
				return hash;
			}

			/// <summary>Mixes the 64 bytes in <see cref="_block"/> into the state.</summary>
			void Transform()
			{
				unchecked
				{
					for (var i = 0; i < 16; i++)
						_words[i] = (uint)(_block[i * 4] | _block[i * 4 + 1] << 8 | _block[i * 4 + 2] << 16 | _block[i * 4 + 3] << 24);
					uint a = _state[0], b = _state[1], c = _state[2], d = _state[3];
					for (var i = 0; i < 64; i++)
					{
						uint f;
						int g;
						if (i < 16)
						{
							f = (b & c) | (~b & d);
							g = i;
						}
						else if (i < 32)
						{
							f = (d & b) | (~d & c);
							g = (5 * i + 1) & 15;
						}
						else if (i < 48)
						{
							f = b ^ c ^ d;
							g = (3 * i + 5) & 15;
						}
						else
						{
							f = c ^ (b | ~d);
							g = (7 * i) & 15;
						}
						var sum = a + f + K[i] + _words[g];
						a = d;
						d = c;
						c = b;
						b += (sum << S[i]) | (sum >> (32 - S[i]));
					}
					_state[0] += a;
					_state[1] += b;
					_state[2] += c;
					_state[3] += d;
				}
			}
		}

		// HMAC

		public static Guid GetGuid(string key, string value)
		{
			var algorithm = new System.Security.Cryptography.HMACMD5();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, long value)
		{
			var algorithm = new System.Security.Cryptography.HMACMD5();
			var guid = HashHelper.GetGuid(algorithm, key, BitConverter.GetBytes(value));
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(string key, byte[] value)
		{
			var algorithm = new System.Security.Cryptography.HMACMD5();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

		public static Guid GetGuid(byte[] key, byte[] value)
		{
			var algorithm = new System.Security.Cryptography.HMACMD5();
			var guid = HashHelper.GetGuid(algorithm, key, value);
			algorithm.Dispose();
			return guid;
		}

	}
}
