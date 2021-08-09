using System.ComponentModel;

namespace JocysCom.ClassLibrary.Runtime
{
	public enum TlvSerializerError
	{
		None = 0,
		[Description("7-bit Decoder Error: Number is too large.")]
		Decoder7BitNumberIsTooLargeError = 1,
		[Description("7-bit Decoder Error: Stream is too short.")]
		Decoder7BitStreamIsTooShortError = 2,
		// Serialize errors.
		TypeIdNotFound = 100,
		NonSerializableObject = 101,
		UnknownType = 102,
		// Deserialize errors.
		EnumIdNotFound = 200,


	}
}
