using Channels.Networking.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Channels.Samples.Http
{
    public class HttpServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();

        private SocketListener _listener;
        
        public HttpServer()
        {
            Features.Set<IServerAddressesFeature>(new ServerAddressesFeature());
        }

        public void Start<TContext>(IHttpApplication<TContext> application)
        {
            var feature = Features.Get<IServerAddressesFeature>();
            var address = feature.Addresses.FirstOrDefault();
            IPAddress ip;
            int port;
            GetIp(address, out ip, out port);
            Task.Run(() => StartAcceptingManagedSocketConnections(application, ip, port));
        }

        private void StartAcceptingManagedSocketConnections<TContext>(IHttpApplication<TContext> application, IPAddress ip, int port)
        {
            _listener = new SocketListener();
            _listener.OnConnection(async connection =>
            {
                await ProcessClient(application, connection);
            });

            _listener.Start(new IPEndPoint(ip, port));
        }

        
        public void Dispose()
        {
            _listener?.Stop();
            _listener?.Dispose();
            _listener = null;
        }

        private static void GetIp(string url, out IPAddress ip, out int port)
        {
            ip = null;

            var address = ServerAddress.FromUrl(url);
            switch (address.Host)
            {
                case "localhost":
                    ip = IPAddress.Loopback;
                    break;
                case "*":
                    ip = IPAddress.Any;
                    break;
                default:
                    break;
            }
            ip = ip ?? IPAddress.Parse(address.Host);
            port = address.Port;
        }

        private static async Task ProcessConnection<TContext>(IHttpApplication<TContext> application, ChannelFactory channelFactory, Socket socket)
        {
            using (var ns = new NetworkStream(socket))
            {
                var channel = channelFactory.MakeChannel(ns);

                await ProcessClient(application, channel);
            }
        }

        private static async Task ProcessClient<TContext>(IHttpApplication<TContext> application, IChannel channel)
        {
            var connection = new HttpConnection<TContext>(application, channel.Input, channel.Output);

            await connection.ProcessAllRequests();

            channel.Output.Complete();
            channel.Input.Complete();
        }
    }
}
