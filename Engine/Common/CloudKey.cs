namespace x360ce.Engine
{
    public class CloudKey
    {
        public const string RsaPublicKey = "RsaPblicKey"; //nameof(RsaPublicKey);
        public const string RandomPassword = nameof(RandomPassword);
		public const string ComputerId = nameof(ComputerId);
		public const string ProfileId = nameof(ProfileId);
		public const string Username = nameof(Username);
        public const string Password = nameof(Password);
        public const string Cloud = nameof(Cloud);
        public const string User = nameof(User);
		public const string Checksum = nameof(Checksum);
        public const string ClientVersion = nameof(ClientVersion);
        /// <summary>
        /// The kinds of device a program reads, as <see cref="InputSourceType"/> flags in an <see cref="int"/>.
        /// The web service returns a program only devices of those kinds; a program that sends none reads DirectInput only.
        /// </summary>
        public const string InputSourceTypes = nameof(InputSourceTypes);
        public const string ServerVersion = nameof(ServerVersion);
        public const string UpdateUrl = nameof(UpdateUrl);
    }
}
