namespace JocysCom.ClassLibrary.Win32
{
	public enum TOKEN_INFORMATION_CLASS
	{
		TokenUser = 1,
		TokenGroups = 2,
		TokenPrivileges = 3,
		TokenOwner = 4,
		TokenPrimaryGroup = 5,
		TokenDefaultDacl = 6,
		TokenSource = 7,
		TokenType = 8,
		TokenImpersonationLevel = 9,
		TokenStatistics = 10,
		TokenRestrictedSids = 11,
		TokenSessionId = 12,
		TokenGroupsAndPrivileges = 13,
		TokenSessionReference = 14,
		TokenSandBoxInert = 15,
		TokenAuditPolicy = 16,
		TokenOrigin = 17,
		TokenElevationType = 18,
		TokenLinkedToken = 19,
		TokenElevation = 20,
		TokenHasRestrictions = 21,
		TokenAccessInformation = 22,
		TokenVirtualizationAllowed = 23,
		TokenVirtualizationEnabled = 24,
		TokenIntegrityLevel = 25,
		TokenUIAccess = 26,
		TokenMandatoryPolicy = 27,
		TokenLogonSid = 28,
		MaxTokenInfoClass = 29
	}
}
