namespace System.Text.Json.Serialization
{
	/// <summary>Lets the shared JocysCom files compile here, where System.Text.Json is not referenced.</summary>
	/// <remarks>
	/// Those files mark members with this attribute for the programs that serialise them with
	/// System.Text.Json. This program does not, so nothing here ever reads it. Internal, so it can never
	/// meet the real attribute in another assembly.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	internal sealed class JsonIgnoreAttribute : Attribute
	{
	}
}
