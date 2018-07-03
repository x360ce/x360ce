using System;

namespace x360ce.Engine.Data
{

	/// <summary>
	/// Represents unique database record of the user. Columns:
	/// [Id] - Unique record Id (Primary Key);
	/// 3 Columns which must make unique index.
	/// [ComputerId]
	/// [ProfileId]
	/// [ItemId]
	/// </summary>
	public interface IUserRecord : IComputer, IDateTime
	{
		Guid ProfileId { get; set; }
	}
}
