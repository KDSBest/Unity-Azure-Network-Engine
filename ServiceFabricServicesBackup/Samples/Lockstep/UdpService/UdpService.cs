using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace UdpService
{
	/// <summary>
	/// The FabricRuntime creates an instance of this class for each service type instance. 
	/// </summary>
	internal sealed class UdpService : StatelessService
	{
		private UdpManagerListener listener;

		public UdpService(StatelessServiceContext context)
			 : base(context)
		{ }

		/// <summary>
		/// Optional override to create listeners (like tcp, http) for this service instance.
		/// </summary>
		/// <returns>The collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[]
			{
					 new ServiceInstanceListener(serviceContext =>
						 {
							 listener = new UdpManagerListener(serviceContext, ServiceEventSource.Current, "ServiceEndpoint");
							 return this.listener;
						 })
			};
		}

		#region Overrides of StatelessService
		protected override Task RunAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				this.listener.Update();
				Thread.Sleep(50);
			}

			return base.RunAsync(cancellationToken);
		}
		#endregion
	}
}
