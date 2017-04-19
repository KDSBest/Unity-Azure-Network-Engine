namespace ReliableUdp
{
	using System;
	using System.Collections.Generic;

	public class UdpPeerCollection
	{
		public int MaxPeers { get; set; }

		private readonly Dictionary<UdpEndPoint, UdpPeer> peersDict;
		private readonly List<UdpPeer> peers;

		public int Count
		{
			get { return this.peers.Count; }
		}

		public UdpPeer this[int index]
		{
			get { return this.peers[index]; }
		}

		public UdpPeerCollection(int maxPeers)
		{
			this.MaxPeers = maxPeers;
			if (maxPeers < 100)
				this.peers = new List<UdpPeer>(maxPeers);
			else
				this.peers = new List<UdpPeer>();

			this.peersDict = new Dictionary<UdpEndPoint, UdpPeer>();
		}

		public bool TryGetValue(UdpEndPoint endPoint, out UdpPeer peer)
		{
			return this.peersDict.TryGetValue(endPoint, out peer);
		}

		public void Clear()
		{
			this.peers.Clear();
			this.peersDict.Clear();
		}

		public void Add(UdpEndPoint endPoint, UdpPeer peer)
		{
			this.peers.Add(peer);
			this.peersDict.Add(endPoint, peer);
		}

		public bool ContainsAddress(UdpEndPoint endPoint)
		{
			return this.peersDict.ContainsKey(endPoint);
		}

		public UdpPeer[] ToArray()
		{
			return this.peers.ToArray();
		}

		public void RemoveAt(int idx)
		{
			this.peersDict.Remove(this.peers[idx].EndPoint);
			this.peers.RemoveAt(idx);
		}

		public void Remove(UdpEndPoint endPoint)
		{
			for (int i = 0; i < this.peers.Count; i++)
			{
				if (this.peers[i].EndPoint.Equals(endPoint))
				{
					this.RemoveAt(i);
					break;
				}
			}
		}
	}
}