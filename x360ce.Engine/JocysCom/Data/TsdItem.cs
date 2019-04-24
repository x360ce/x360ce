using System.IO;

namespace JocysCom.ClassLibrary.Data
{
	//  ValueType[0-5] + ValueSize[0-5] + ValueData[0-uint.Max]
	public class TsdItem
	{
		public int Type { get; set; }
		//public int Size { get; set; }
		public byte[] Data { get; set; }

		byte[] ToBytes()
		{
			var ms = new MemoryStream();
			//var br = new System.IO.BinaryReader(ms);
			//var bw = new System.IO.BinaryWriter(ms);
			return null;
		}

		/// <summary>
		/// Write an Int32, 7 bits at a time.
		/// </summary>
		/// <param name="value"></param>
		/// <remarks>
		/// Decimal: 1,259,551,277
		/// Remaining integer                 encoded bytes
		/// 1001011000100110011101000101101
		/// 100101100010011001110100          00101101
		/// 10010110001001100                 10101101 01110100
		/// 1001011000                        10101101 11110100 01001100
		/// 100                               10101101 11110100 11001100 01011000
		/// 0                                 10101101 11110100 11001100 11011000 00000100
		/// 
		/// </remarks>
		public static void Write7BitEncoded(Stream stream, uint value)
		{
			uint v = value;
			byte b = 0;
			do
			{
				// Store 7 bits.
				b = (byte)(v & 0x7F);
				// Shift by 7 bits.
				v >>= 7;
				// If more bits left then...
				if (v > 0)
					// set first bit to 1.
					b |= 0x80;
				stream.WriteByte(b);
			}
			// Continue if more bits left.
			while (v > 0);
		}

		/// <summary>
		/// Read an Int32, 7 bits at a time.
		/// </summary>
		public static TsdError Read7BitEncoded(Stream stream, out uint result)
		{
			result = 0;
			uint v = 0;
			var b = 0;
			var i = 0;
			do
			{
				// Read byte.
				b = stream.ReadByte();
				// if end of stream and no more bytes to read then...
				if (b == -1)
					return TsdError.Decoder7BitStreamIsTooShortError;
				if (i == 4 && b > 0xF)
					return TsdError.Decoder7BitNumberIsTooLargeError;
				// Add 7 bit value
				v |= (uint)(b & 0x7F) << (7 * i);
				i++;
			}
			// Continue if first bit is 1.
			while (b >> 7 == 1);
			result = v;
			return TsdError.None;
		}

	}
}
