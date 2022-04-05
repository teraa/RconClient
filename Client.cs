using System.Net.Sockets;

namespace Teraa.Rcon;

public interface IClient
{
    ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken);
    Stream GetStream();
}

public class Client : IClient, IDisposable
{
    private readonly TcpClient _client;

    public Client()
    {
        _client = new TcpClient();
    }

    public Client(TcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
        => _client.ConnectAsync(host, port, cancellationToken);

    public Stream GetStream()
        => _client.GetStream();

    public void Dispose()
        => _client.Dispose();
}
