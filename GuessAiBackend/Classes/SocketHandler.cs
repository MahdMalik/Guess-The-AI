using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace Classes
{
    public static class SocketHandler
    {
        async public static Task SendPacket(object message, WebSocket socket)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            var bufferMessage = Encoding.UTF8.GetBytes(jsonMessage);
            await socket.SendAsync(new ArraySegment<byte>(bufferMessage), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        async public static Task HandleSocket(WebSocket socket)
        {
            while (socket.State == WebSocketState.Open)
            {
                byte[] buffer = new byte[5000];
                //array segment is just way to specify where to write in the buffer, by default index 0 is start and count is end of array
                var firstConnectionData = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                //should be sending text as first message
                if (firstConnectionData.MessageType == WebSocketMessageType.Text)
                {
                    //first convert bytes to json
                    string jsonData = Encoding.UTF8.GetString(buffer, 0, firstConnectionData.Count);
                    //now convert it to a class
                    ClientMessage? messageData = JsonSerializer.Deserialize<ClientMessage>(jsonData);
                    if (messageData == null)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "error deseralizing json", CancellationToken.None);
                    }

                    Player ourPlayer = null;
                    bool success = false;
                    String message = "";
                    object sentMessage = null;
                    switch (messageData.messageType)
                    {
                        //if the message is to join a queue, do this:
                        case "Join Queue":
                            //first, create our player, then put them into the queue
                            ourPlayer = new Player(messageData.username);
                            switch (messageData.queueType)
                            {
                                case "One Bot Game":
                                    (success, message) = Globals.oneBotQueue.AddPlayer(ourPlayer, false);
                                    break;
                                //if try to join a quue that doesn't exist, call out their bs
                                default:
                                    await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "queue type didn't match", CancellationToken.None);
                                    break;
                            }
                            sentMessage = new
                            {
                                success = success,
                                message = message
                            };
                            await SendPacket(sentMessage, socket);
                            break;
                        case "Leave Queue":
                            switch (messageData.queueType)
                            {
                                case "One Bot Game":
                                    (success, message, ourPlayer) = Globals.oneBotQueue.RemovePlayer(messageData.username);
                                    break;
                                default:
                                    await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "queue type didn't match", CancellationToken.None);
                                    break;
                            }
                            if (success)
                            {
                                message += ", removed player: " + ourPlayer.GetName();
                            }
                            sentMessage = new
                            {
                                success = success,
                                message = message
                            };
                            await SendPacket(sentMessage, socket);
                            break;
                        default:
                            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "message type wasn't found", CancellationToken.None);
                            break;
                    }
                }
                //otherwise, want to close the connection and stop it
                else
                {
                    await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "expected first message with user info, didn't get it", CancellationToken.None);
                }
            }
        }
    }
}