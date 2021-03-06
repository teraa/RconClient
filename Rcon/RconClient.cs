using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Teraa.Rcon;

public interface IRconClient
{
    ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    ValueTask<Message> SendAsync(Message message, CancellationToken cancellationToken = default);
}

public static class RconClientExtensions
{
    public static ValueTask<Message> SendAsync(this IRconClient client, int id, MessageType type, string body, CancellationToken cancellationToken = default)
        => client.SendAsync(new Message(id, type, body), cancellationToken);
}

public class RconClient : IRconClient, IDisposable
{
    private const int BaseSize = sizeof(int) * 2 + sizeof(byte) * 2;
    private const int MinimumBufferSize = sizeof(int) + BaseSize;

    private readonly IClient _client;
    private Stream? _stream;
    private PipeReader? _reader;

    public RconClient()
    {
        _client = new Client();
    }

    public RconClient(IClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    [MemberNotNullWhen(true, nameof(_reader), nameof(_stream))]
    public bool Connected { get; private set; }
    public Encoding Encoding { get; init; } = Encoding.Latin1;

    public void Dispose()
    {
        Connected = false;
        (_client as IDisposable)?.Dispose();
        _stream?.Dispose();
    }

    public async ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken)
            .ConfigureAwait(false);

        _stream = _client.GetStream();
        _reader = PipeReader.Create(_stream);

        Connected = true;
    }

    public async ValueTask<Message> SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!Connected)
            throw new InvalidOperationException("Client is not connected");

        byte[] buffer = WriteMessage(message);

        _stream.Write(buffer);

        await _stream.FlushAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = await _reader.ReadAtLeastAsync(MinimumBufferSize, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (result.IsCompleted || result.IsCanceled)
                return default;

            Debug.Assert(result.Buffer.Length >= MinimumBufferSize);

            var response = ReadMessage(result.Buffer);
            return response;
        }
        finally
        {
            _reader.AdvanceTo(result.Buffer.End);
        }
    }

    private byte[] WriteMessage(Message message)
    {
        int bodyLength = Encoding.GetByteCount(message.Body);
        byte[] buffer = new byte[MinimumBufferSize + bodyLength];

        using var stream = new MemoryStream(buffer);
        using var writer = new BinaryWriter(stream, Encoding);

        writer.Write(BaseSize + bodyLength); // Size of rest of the message
        writer.Write(message.Id); // ID
        writer.Write((int)message.Type); // Type

        // Manually writing message body to the byte array because BinaryWriter#Write(string value)
        // method prefixes the output with the string length which we do not want here.
        Encoding.GetBytes(message.Body, buffer.AsSpan()[(int)stream.Position..]);
        stream.Position += bodyLength;

        writer.Write((byte)0); // Null-terminated string
        writer.Write((byte)0); // Message Terminator

        return buffer;
    }

    private Message ReadMessage(ReadOnlySequence<byte> sequence)
    {
        SequenceReader<byte> reader = new(sequence);
        reader.TryReadLittleEndian(out int length);
        reader.TryReadLittleEndian(out int id);
        reader.TryReadLittleEndian(out int type);
        reader.TryReadTo(span: out var span, delimiter: (byte) 0, advancePastDelimiter: false);
        string body = Encoding.GetString(span);

        return new Message(id, (MessageType)type, body);
    }
}
