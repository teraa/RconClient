namespace Teraa.Rcon;

public enum MessageType : int
{
    Auth = 3,
    AuthResponse = 2,
    Command = 2, // Intentional
    CommandResponse = 0,
}

public record struct Message(int Id, MessageType Type, string Body);
