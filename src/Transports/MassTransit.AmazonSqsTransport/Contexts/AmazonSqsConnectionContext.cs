namespace MassTransit.AmazonSqsTransport.Contexts
{
    using System;
    using System.Threading;
    using Configuration;
    using GreenPipes;
    using Topology;
    using Transport;


    public class AmazonSqsConnectionContext :
        BasePipeContext,
        ConnectionContext,
        IDisposable
    {
        readonly IAmazonSqsHostConfiguration _hostConfiguration;

        public AmazonSqsConnectionContext(IConnection connection, IAmazonSqsHostConfiguration hostConfiguration, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _hostConfiguration = hostConfiguration;
            Connection = connection;

            Topology = hostConfiguration.HostTopology;
        }

        public IConnection Connection { get; }
        public IAmazonSqsHostTopology Topology { get; }

        public Uri HostAddress => _hostConfiguration.HostAddress;

        public ClientContext CreateClientContext(CancellationToken cancellationToken)
        {
            return new AmazonSqsClientContext(this, Connection.SqsClient, Connection.SnsClient, cancellationToken);
        }

        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
}
