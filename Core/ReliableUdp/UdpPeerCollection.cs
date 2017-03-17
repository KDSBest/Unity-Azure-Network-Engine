namespace ReliableUdp
{
	using System;
	using System.Collections.Generic;

	public class UdpPeerCollection
	{
		private readonly Dictionary<UdpEndPoint, UdpPeer> peersDict;
		private readonly UdpPeer[] peersArray;
		private int count;

		public int Count
		{
			get { return this.count; }
		}

		public UdpPeer this[int index]
		{
			get { return this.peersArray[index]; }
		}

		public UdpPeerCollection(int maxPeers)
		{
			this.peersArray = new UdpPeer[maxPeers];
			this.peersDict = new Dictionary<UdpEndPoint, UdpPeer>();
		}

		public bool TryGetValue(UdpEndPoint endPoint, out UdpPeer peer)
		{
			return this.peersDict.TryGetValue(endPoint, out peer);
		}

		public void Clear()
		{
			Array.Clear(this.peersArray, 0, this.count);
			this.peersDict.Clear();
			this.count = 0;
		}

		public void Add(UdpEndPoint endPoint, UdpPeer peer)
		{
			this.peersArray[this.count] = peer;
			this.peersDict.Add(endPoint, peer);
			this.count++;
		}

		public bool ContainsAddress(UdpEndPoint endPoint)
		{
			return this.peersDict.ContainsKey(endPoint);
		}

		public UdpPeer[] ToArray()
		{
			UdpPeer[] result = new UdpPeer[this.count];
			Array.Copy(this.peersArray, 0, result, 0, this.count);
			return result;
		}

		public void RemoveAt(int idx)
		{
			this.peersDict.Remove(this.peersArray[idx].EndPoint);
			this.peersArray[idx] = this.peersArray[this.count - 1];
			this.peersArray[this.count - 1] = null;
			this.count--;
		}

		public void Remove(UdpEndPoint endPoint)
		{
			for (int i = 0; i < this.count; i++)
			{
				if (this.peersArray[i].EndPoint.Equals(endPoint))
				{
					this.RemoveAt(i);
					break;
				}
			}
		}
	}
}