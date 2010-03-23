using System;
using System.Collections.Generic;
using System.Text;

namespace x360ce.App
{
	/// <summary>
	/// Microsoft SDKs\Windows\v7.0\Include\Winnt.h
	/// </summary>
	public partial class WinNT
	{
		public const int ERROR_INSUFFICIENT_BUFFER = 122;

		////////////////////////////////////////////////////////////////////////
		//                                                                    //
		//                             ACCESS TYPES                           //
		//                                                                    //
		////////////////////////////////////////////////////////////////////////

		//  The following are masks for the predefined standard access types
		public const uint DELETE = 0x00010000;
		public const uint READ_CONTROL = 0x00020000;
		public const uint WRITE_DAC = 0x00040000;
		public const uint WRITE_OWNER = 0x00080000;
		public const uint SYNCHRONIZE = 0x00100000;

		// Standard rights
		public const uint STANDARD_RIGHTS_READ = READ_CONTROL;
		public const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
		public const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;
		public const uint STANDARD_RIGHTS_REQUIRED = DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER;
		public const uint STANDARD_RIGHTS_ALL = DELETE | READ_CONTROL | WRITE_DAC | WRITE_OWNER | SYNCHRONIZE;

		// AccessSystemAcl access type
		public const uint ACCESS_SYSTEM_SECURITY = 0x01000000;

		// MaximumAllowed access type
		public const uint MAXIMUM_ALLOWED = 0x02000000;

		//  These are the generic rights.
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint GENERIC_EXECUTE = 0x20000000;
		public const uint GENERIC_ALL = 0x10000000;


		////////////////////////////////////////////////////////////////////
		//                                                                //
		//           Token Object Definitions                             //
		//                                                                //
		//                                                                //
		////////////////////////////////////////////////////////////////////


		// Token Specific Access Rights.
		public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
		public const uint TOKEN_DUPLICATE = 0x0002;
		public const uint TOKEN_IMPERSONATE = 0x0004;
		public const uint TOKEN_QUERY = 0x0008;
		public const uint TOKEN_QUERY_SOURCE = 0x0010;
		public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
		public const uint TOKEN_ADJUST_GROUPS = 0x0040;
		public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
		public const uint TOKEN_ADJUST_SESSIONID = 0x0100;

		public const uint TOKEN_ALL_ACCESS_P = STANDARD_RIGHTS_REQUIRED |
								  TOKEN_ASSIGN_PRIMARY |
								  TOKEN_DUPLICATE |
								  TOKEN_IMPERSONATE |
								  TOKEN_QUERY |
								  TOKEN_QUERY_SOURCE |
								  TOKEN_ADJUST_PRIVILEGES |
								  TOKEN_ADJUST_GROUPS |
								  TOKEN_ADJUST_DEFAULT;

		public const uint TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID;

		public const uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;

		public const uint TOKEN_WRITE = STANDARD_RIGHTS_WRITE |
								  TOKEN_ADJUST_PRIVILEGES |
								  TOKEN_ADJUST_GROUPS |
								  TOKEN_ADJUST_DEFAULT;

		public const uint TOKEN_EXECUTE = STANDARD_RIGHTS_EXECUTE;
	}
}
