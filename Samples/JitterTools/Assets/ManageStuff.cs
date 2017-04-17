using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Assets;

using EventSystem;
using EventSystem.Events;

using Protocol;

using ReliableUdp;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using FactoryRegistrations = ReliableUdp.FactoryRegistrations;

public class ManageStuff : MonoBehaviour
{
	public GameObject Panel;

	public Button BtnHost;

	public Button BtnConnect;

	public Text TextHost;

	public Text Info;

	public static UdpManager Udp = new UdpManager(new EventSystem.ProtocolListener(), "blub");

	public GameObject Player;
	public GameObject Enemy;

	public GameObject ServerCamera;

	// Use this for initialization
	void Start()
	{
		FactoryRegistrations.Register();
		Protocol.FactoryRegistrations.Register();
		Udp.SimulationMinLatency = 50;
		Udp.SimulationMaxLatency = 100;
		Udp.SimulateLatency = true;

		this.BtnHost.onClick.AddListener(StartServer);
		this.BtnConnect.onClick.AddListener(this.StartClient);
		this.TextHost.text = "localhost";

		PubSub<NetworkLatencyEvent>.Subscribe("Lat", Latency);
	}

private	Dictionary<long, int> latencies = new Dictionary<long, int>();

	private void Latency(NetworkLatencyEvent networkLatencyEvent)
	{
		long id = networkLatencyEvent.Peer.ConnectId;
		if (!this.latencies.ContainsKey(id))
			this.latencies.Add(id, 0);

		this.latencies[id] = networkLatencyEvent.Latency;

		this.Info.text = string.Join(string.Empty, this.latencies.Select(x => string.Format("{0}: {1}ms\r\n", x.Key, x.Value)).ToArray());
	}

	private Server server;

	private void StartServer()
	{
		this.Panel.SetActive(false);
		if (!Udp.Start(3333))
		{
			Debug.LogError("Server start failed");
			this.Panel.SetActive(true);
			return;
		}

		server = new Server();
	}

	private void StartClient()
	{
		PubSub<NetworkConnectedEvent>.Subscribe("Client",
															 ev =>
															 {
																 Debug.Log("Connected.");
																 Spawn();
															 });
		PubSub<NetworkDisconnectedEvent>.Subscribe("Client",
															 ev =>
															 {
																 Debug.Log("Disconnected.");
																 this.Panel.SetActive(true);
															 });
		this.ServerCamera.SetActive(false);
		this.Panel.SetActive(false);
		Udp.Connect(this.TextHost.text, 3333);
	}

	private Guid PlayerId;

	private Dictionary<Guid, GameObject> Enemies = new Dictionary<Guid, GameObject>();
	private void Spawn()
	{
		GameObject go = GameObject.Instantiate(Player);
		this.PlayerId = go.GetComponent<PlayerTest>().Id;

		PubSub<NetworkReceiveEvent<ServerUpdate>>.Subscribe("fas",
												 packet =>
													 {
														 foreach (var cu in packet.Packet.Clients)
														 {
															 UpdateEnemy(cu);
														 }
													 });
	}

	private void UpdateEnemy(ClientUpdate cu)
	{
		Guid id = new Guid(cu.Id.Id.ToArray());
		if (id == this.PlayerId)
			return;

		if (!this.Enemies.ContainsKey(id))
		{
			GameObject go = GameObject.Instantiate(this.Enemy);
			this.Enemies.Add(id, go);
		}

		this.Enemies[id].GetComponent<Enemy>().NewClientUpdate(cu);
	}

	public void FixedUpdate()
	{
		if (this.server != null)
			server.Update();

		Udp.PollEvents();
	}
}
