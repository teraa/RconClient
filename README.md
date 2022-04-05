# RCON Client

## Description

A lightweight wrapper around `TcpClient` to send and receive messages asynchronously from [Source RCON Protocol](https://developer.valvesoftware.com/wiki/Source_RCON_Protocol) compatible servers.  
This is a very simple/dumb implementation and concurrently sending multiple messages is not supported as the send method returns the first received message after sending as a response.

## Minecraft Example

```cs
using Teraa.Rcon;

using var client = new RconClient();
await client.ConnectAsync("localhost", 25575, cancellationToken: default);

Message? response;

response = await client.SendAsync(
    id: 1,
    type: MessageType.Auth,
    body: "password",
    cancellationToken: default);

if (response is null)
{
    // No response
}
else if (response.Value is { Id: -1, Type: MessageType.AuthResponse } )
{
    // Auth failed (invalid password)
}
else if (response.Value is not ({ Id: 1 } or { Type: MessageType.AuthResponse }))
{
    // Invalid response
}
else
{
    // Ok
}

response = await client.SendAsync(
    id: 2,
    type: MessageType.Command,
    body: "list",
    cancellationToken: default);

if (response is null)
{
    // No response
}
else if (response.Value is not ({ Id: 2 } or { Type: MessageType.CommandResponse }))
{
    // Invalid response
}
else
{
    // Ok
}
```

## References

- https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
- https://wiki.vg/RCON
