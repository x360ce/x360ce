using System;
using System.ComponentModel;

namespace x360ce.Engine.Data
{
	public interface IProgram : INotifyPropertyChanged
	{
		string FileName { get; set; }
		string FileProductName { get; set; }
		int XInputMask { get; set; }
		string XInputPath { get; set; }
		int HookMask { get; set; }
		int DInputMask { get; set; }
		string DInputFile { get; set; }
		int FakeVID { get; set; }
		int FakePID { get; set; }
		int Timeout { get; set; }
		int AutoMapMask { get; set; }
		int EmulationType { get; set; }
		int ProcessorArchitecture { get; set; }
		string Comment { get; set; }
		DateTime DateCreated { get; set; }
		DateTime DateUpdated { get; set; }
	}
}
