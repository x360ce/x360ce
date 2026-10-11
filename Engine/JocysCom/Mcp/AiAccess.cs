#nullable disable
namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>How much an AI assistant or a script may do through the program's tools, once access is switched on. Chosen by the person, never by the caller.</summary>
	public enum AiAccess
	{
		/// <summary>Read the interface and the help, and point at things. Changes no setting.</summary>
		Read = 1,
		/// <summary>Everything a person does on the program's pages.</summary>
		Configure = 2,
		/// <summary>Also the actions that administer the computer or act with the person's access, which each program names.</summary>
		Administer = 3,
	}
}
