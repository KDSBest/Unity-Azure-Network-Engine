namespace ReliableUdp.Logging
{
	using System.Diagnostics;

	public class DebugLogger : IUdpLogger
	{
		public void Log(string str)
		{
			Debug.WriteLine(str);
		}
	}
}