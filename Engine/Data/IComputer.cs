using System;

namespace x360ce.Engine.Data
{
	public interface IComputer: IChecksum
	{
		Guid ComputerId { get; set; }
	}
}
