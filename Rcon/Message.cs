namespace Teraa.Rcon;

public enum MessageType : int
{
    Auth = 3,
    AuthResponse = 2,
#pragma warning disable CA1069
    Command = 2, // Intentional
#pragma warning restore CA1069
    CommandResponse = 0,
}

public record struct Message(int Id, MessageType Type, string Body);
