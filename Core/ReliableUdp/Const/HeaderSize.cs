namespace ReliableUdp.Const
{
	public static class HeaderSize
	{
		public const int DEFAULT = 1;
		public const int SEQUENCED = 3;
		public const int FRAGMENT = 6;
		// 68 + encrypted flag + aes blocksize
		public const int MAX_UDP = 68 + 1 + 16;
	}
}
