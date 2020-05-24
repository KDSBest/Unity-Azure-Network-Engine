using System;
using System.Threading;

namespace ReliableUdp
{
    public sealed class UdpThread
	{
		private Thread thread;

		private readonly Action callback;

		public int SleepTime;
        private readonly string name;

        public bool IsRunning { get; private set; }

        public UdpThread(string name, int sleepTime, Action callback)
		{
			this.callback = callback;
			SleepTime = sleepTime;
			this.name = name;
		}

		public void Start()
		{
			if (this.IsRunning)
				return;
			this.IsRunning = true;
			this.thread = new Thread(ThreadLogic)
			{
				Name = this.name,
				IsBackground = true
			};
			this.thread.Start();
		}

		public void Stop()
		{
			if (!this.IsRunning)
				return;
			this.IsRunning = false;

			this.thread.Join();
		}

		private void ThreadLogic()
		{
			while (this.IsRunning)
			{
				this.callback();
				Thread.Sleep(SleepTime);
			}
		}
	}
}
