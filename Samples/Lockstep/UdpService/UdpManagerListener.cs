using System;
using System.Fabric;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ServiceFabric.Services.Communication.Runtime;

using ReliableUdp;

namespace UdpService
{
	internal class UdpManagerListener : ICommunicationListener
	{
		private readonly ServiceEventSource eventSource;
		private readonly ServiceContext serviceContext;
		private readonly string endpointName;

		private UdpManager udp;
		private string publishAddress;
		private string listeningAddress;

		private UdpListener udpListener;

		public UdpManagerListener(ServiceContext serviceContext, ServiceEventSource eventSource, string endpointName)
		{
			if (serviceContext == null)
			{
				throw new ArgumentNullException(nameof(serviceContext));
			}

			if (endpointName == null)
			{
				throw new ArgumentNullException(nameof(endpointName));
			}

			if (eventSource == null)
			{
				throw new ArgumentNullException(nameof(eventSource));
			}

			this.serviceContext = serviceContext;
			this.endpointName = endpointName;
			this.eventSource = eventSource;
		}

		public bool ListenOnSecondary { get; set; }

		public void Update()
		{
			if (this.udpListener != null)
				this.udpListener.Update();
		}

		public Task<string> OpenAsync(CancellationToken cancellationToken)
		{
			var serviceEndpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(this.endpointName);
			int port = serviceEndpoint.Port;

			if (this.serviceContext is StatefulServiceContext)
			{
				StatefulServiceContext statefulServiceContext = this.serviceContext as StatefulServiceContext;

				this.listeningAddress = string.Format(
					 CultureInfo.InvariantCulture,
					 "udp://+:{0}/{1}/{2}/{3}",
					 port,
					 statefulServiceContext.PartitionId,
					 statefulServiceContext.ReplicaId,
					 Guid.NewGuid());
			}
			else if (this.serviceContext is StatelessServiceContext)
			{
				this.listeningAddress = string.Format(
					 CultureInfo.InvariantCulture,
					 "udp://+:{0}",
					 port);
			}
			else
			{
				throw new InvalidOperationException();
			}

			this.publishAddress = this.listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

			try
			{
				this.eventSource.ServiceMessage(this.serviceContext, "Starting server on " + this.listeningAddress);

				this.udpListener = new UdpListener();
				this.udp = new UdpManager(this.udpListener, "kds");

				if (this.udp.Start(port))
				{
					this.eventSource.ServiceMessage(this.serviceContext, "Listening on " + this.publishAddress);
				}
				else
				{
					throw new Exception("Udp Server couldn't start.");
				}
				return Task.FromResult(this.publishAddress);
			}
			catch (Exception ex)
			{
				this.eventSource.ServiceMessage(this.serviceContext, "Udp server failed to open. " + ex.ToString());

				this.StopUdpServer();

				throw;
			}
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			this.eventSource.ServiceMessage(this.serviceContext, "Closing udp server");

			this.StopUdpServer();

			return Task.FromResult(true);
		}

		public void Abort()
		{
			this.eventSource.ServiceMessage(this.serviceContext, "Aborting udp server");

			this.StopUdpServer();
		}

		private void StopUdpServer()
		{
			if (this.udp != null)
			{
				this.udpListener = null;
				this.udp.Stop();
			}
		}
	}
}
