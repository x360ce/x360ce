using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JocysCom.WebSites.Engine
{
	/// <summary>
	/// Business Item Type
	/// </summary>
	/// <remarks>
	/// Convert the string to an enum object:
	/// MemberType mt = (MemberType)Enum.Parse(typeof(MemberType), "Friend", true);
	/// Convert the Int32 value to an enum object:
	/// MemberType mt2 = (MemberType)4;
	/// Convert an enum object to Int32 value:
	/// int mtValue = (int)mt;
	/// Convert an enum object to string:
	/// string mtString = mt.ToString();
	/// </remarks>
	[Serializable]
	public enum ItemType
	{
		None,
		Book,
		BookCategory, 
		Author,
		Member,
		Group,
		Event,
		Message,
		Tick,
		Game,
		Movie,
		Record,
		Forum,
		ForumGroup,
		ForumCategory,
		ForumThread,
		ForumPost,
        Email,
		// ASP.NET Mebership provider.
		User,
		Role,
		// Social.
		Quiz,
		Competition,
	}
}