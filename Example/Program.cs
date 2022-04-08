using Teraa.Rcon;

using var client = new RconClient();
await client.ConnectAsync(
    host: "localhost",
    port: 25575,
    cancellationToken: default);

const int authId = 1;
Message response;

response = await client.SendAsync(
    id: authId,
    type: MessageType.Auth,
    body: "password",
    cancellationToken: default);

switch (response)
{
    case { Id: authId, Type: MessageType.AuthResponse }:
        Console.WriteLine("Authenticated");
        break;
    case { Id: -1, Type: MessageType.AuthResponse }:
        Console.WriteLine("Auth failed (invalid password)");
        return;
    default:
        Console.WriteLine($"Unknown response: {response}");
        return;
}

Console.WriteLine("Type commands or Q to quit.");

int id = authId + 1;
while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (input is null or "q" or "Q")
        break;

    id++;

    response = await client.SendAsync(
        id: id,
        type: MessageType.Command,
        body: input,
        cancellationToken: default);

    if (response.Id == id && response.Type == MessageType.CommandResponse)
        Console.WriteLine(response.Body);
    else
        Console.WriteLine($"Unknown response: {response}");
}
