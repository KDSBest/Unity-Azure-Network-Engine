using System;
using System.Linq;

namespace ReliableUdp
{
	using System.Threading;

	public sealed class UdpThread
	{
		private Thread thread;

		private readonly Action callback;

		public int SleepTime;
		private bool running;
		private readonly string name;

		public bool IsRunning
		{
			get { return this.running; }
		}

		public UdpThread(string name, int sleepTime, Action callback)
		{
			this.callback = callback;
			SleepTime = sleepTime;
			this.name = name;
		}

		public void Start()
		{
			if (this.running)
				return;
			this.running = true;
			this.thread = new Thread(ThreadLogic)
			{
				Name = this.name,
				IsBackground = true
			};
			this.thread.Start();
		}

		public void Stop()
		{
			if (!this.running)
				return;
			this.running = false;

			this.thread.Join();
		}

		private void ThreadLogic()
		{
			while (this.running)
			{
				this.callback();
				Thread.Sleep(SleepTime);
			}
		}
	}
}
