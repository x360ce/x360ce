using System;

namespace x360ce.Engine.Data
{
	public partial class UserProfile : IChecksum, IDateTime
	{
		public UserProfile()
		{
			DateCreated = DateTime.Now;
			DateUpdated = DateCreated;
		}

		public static Guid GenerateProfileId(Guid computerId, string profilePath)
		{
			var guidBytes = computerId.ToByteArray();
			// Use Unicode to make sure it is compatible with SQL database.
			var pathBytes = System.Text.Encoding.Unicode.GetBytes(profilePath.ToUpper());
			var bytes = new byte[guidBytes.Length + pathBytes.Length];
			Array.Copy(guidBytes, 0, bytes, 0, guidBytes.Length);
			Array.Copy(pathBytes, 0, bytes, guidBytes.Length, pathBytes.Length);
			return JocysCom.ClassLibrary.Security.MD5Helper.GetGuid(bytes);
		}

	}
}
