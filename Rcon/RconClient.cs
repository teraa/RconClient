using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Teraa.Rcon;

public interface IRconClient
{
    ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    ValueTask<Message?> SendAsync(int id, MessageType type, string body, CancellationToken cancellationToken = default);
    ValueTask<Message?> SendAsync(Message message, CancellationToken cancellationToken = default);
}

public class RconClient : IRconClient, IDisposable
{
    private readonly IClient _client;
    private PipeReader? _reader;
    private BinaryWriter? _writer;

    public RconClient()
    {
        _client = new Client();
    }

    public RconClient(IClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    [MemberNotNullWhen(true, nameof(_reader), nameof(_writer))]
    public bool Connected { get; private set; }
    public Encoding Encoding { get; init; } = Encoding.Latin1;

    public void Dispose()
    {
        Connected = false;
        (_client as IDisposable)?.Dispose();
        _writer?.Dispose();
    }

    public async ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken);
        var stream = _client.GetStream();
        _reader = PipeReader.Create(stream, new StreamPipeReaderOptions(useZeroByteReads: true));
        _writer = new BinaryWriter(stream, Encoding);

        Connected = true;
    }

    public ValueTask<Message?> SendAsync(int id, MessageType type, string body, CancellationToken cancellationToken = default)
        => SendAsync(new Message(id, type, body), cancellationToken);

    public async ValueTask<Message?> SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!Connected)
            throw new InvalidOperationException("Client is not connected");

        // Manually allocating the byte array because BinaryWriter#Write(string value)
        // method prefixes the output with the string length which we do not want here.
        byte[] body = Encoding.GetBytes(message.Body);
        _writer.Write(4 + 4 + body.Length + 1); // Size
        _writer.Write(message.Id); // ID
        _writer.Write((int)message.Type); // Type
        _writer.Write(body); // Body
        _writer.Write((byte)0); // Terminator

        while (true)
        {
            var result = await _reader.ReadAtLeastAsync(4, cancellationToken);
            try
            {
                if (result.IsCompleted || result.IsCanceled)
                    return null;

                Debug.Assert(result.Buffer.Length >= 4);

                var response = ReadMessage(result.Buffer);
                return response;
            }
            finally
            {
                _reader.AdvanceTo(result.Buffer.End);
            }
        }
    }

    private Message ReadMessage(ReadOnlySequence<byte> sequence)
    {
        SequenceReader<byte> reader = new(sequence);
        reader.TryReadLittleEndian(out int length);
        reader.TryReadLittleEndian(out int id);
        reader.TryReadLittleEndian(out int type);

        byte[] body = new byte[length - 1 - 4 - 4 - 1];
        ReadOnlySpan<byte> span = body.AsSpan();

        reader.TryReadTo(span: out span, delimiter: (byte) 0, advancePastDelimiter: false);
        string bodyText = Encoding.GetString(span);

        return new Message(id, (MessageType) type, bodyText);
    }
}
