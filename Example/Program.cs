using Teraa.Rcon;

using var client = new RconClient();
await client.ConnectAsync("localhost", 25575, cancellationToken: default);

const int authId = 1;
Message? response;

response = await client.SendAsync(
    id: authId,
    type: MessageType.Auth,
    body: "your RCON password here",
    cancellationToken: default);

switch (response)
{
    case null:
        Console.WriteLine("Auth failed (no response)");
        return;
    case { Id: -1, Type: MessageType.AuthResponse }:
        Console.WriteLine("Auth failed (invalid password)");
        return;
    case not { Id: authId, Type: MessageType.AuthResponse }:
        Console.WriteLine($"Unknown response: {response}");
        return;
}

Console.WriteLine("Authenticated");
Console.WriteLine("Type commands or Q to quit.");

int id = authId + 1;
while (true)
{
    Console.Write(">");
    string? input = Console.ReadLine();
    if (input is null or "q" or "Q")
        break;

    id++;

    response = await client.SendAsync(
        id: id,
        type: MessageType.Command,
        body: input,
        cancellationToken: default);

    if (response is null)
    {
        Console.WriteLine("No response received");
        break;
    }

    if (response.Value.Id != id || response.Value.Type != MessageType.CommandResponse)
        Console.WriteLine($"Unknown response: {response}");
    else
        Console.WriteLine(response.Value.Body);
}
