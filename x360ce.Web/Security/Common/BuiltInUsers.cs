﻿using JocysCom.WebSites.Engine;

namespace JocysCom.Web.Security
{
	public enum BuiltInUsers
	{
		[GuidValue("51f1a3a1-5e89-4b5f-9adc-3678c767ed16")]
		Administrators,
		[GuidValue("3af25f5c-09f9-4269-af39-332de274d5a5")]
		Guests,
		[GuidValue("e9427ed6-5abf-47f5-b91b-f40fd81c8e70")]
		PowerUsers,
		[GuidValue("855f0a2e-f38d-4e35-a080-8135acb41dd6")]
		Users,
		[GuidValue("9f204338-b04b-4678-8520-241a17ed8e6b")]
		SocialUsers,
		[GuidValue("6760b8ae-aebc-ade5-8e6c-51d8b7cf5170")]
		ForumAdmin,
	}
}
