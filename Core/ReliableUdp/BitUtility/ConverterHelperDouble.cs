namespace ReliableUdp.BitUtility
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public struct ConverterHelperDouble
	{
		[FieldOffset(0)]
		public ulong Along;

		[FieldOffset(0)]
		public double Adouble;
	}
}