using System.Data;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualBasic;

namespace Classes
{
    public class SocketHandler
    {
        private WebSocket socket;
        private object plantedMessage;

        private Player? ourPlayer;

        public SocketHandler(WebSocket theSocket)
        {
            socket = theSocket;
        }

        async public Task SendPacket()
        {
            string jsonMessage = JsonSerializer.Serialize(plantedMessage);
            var bufferMessage = Encoding.UTF8.GetBytes(jsonMessage);
            await socket.SendAsync(new ArraySegment<byte>(bufferMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine("Message Sent!");
            plantedMessage = null;
        }

        async public Task ProcessReceivedData(byte[] buffer, WebSocketReceiveResult receivedData)
        {
            //should be sending text obviosuly
            if (receivedData.MessageType == WebSocketMessageType.Text)
            {
                //first convert bytes to json
                string jsonData = Encoding.UTF8.GetString(buffer, 0, receivedData.Count);
                //now convert it to a class
                ClientMessage? messageData = JsonSerializer.Deserialize<ClientMessage>(jsonData);
                if (messageData == null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "error deseralizing json", CancellationToken.None);
                    return;
                }
                bool success = false;
                String message = "";
                switch (messageData.messageType)
                {
                    //if the message is to join a queue, do this:
                    case "Join Queue":
                        //first, create our player, then put them into the queue
                        ourPlayer = new Player(messageData.username);
                        Globals.playerMapping.Add(ourPlayer.GetName(), ourPlayer);
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
                        if (success)
                        {
                            Globals.socketPlayerMapping.Add(ourPlayer, this);
                        }
                        plantedMessage = new
                        {
                            success = success,
                            message = message,
                            type = "Confirmation"
                        };
                        await SendPacket();
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
                            //have to remove our player from here as well
                            Globals.socketPlayerMapping.Remove(ourPlayer);
                            message += ", removed player: " + ourPlayer.GetName();
                        }
                        plantedMessage = new
                        {
                            success = success,
                            message = message,
                            type = "Confirmation"
                        };
                        await SendPacket();
                        break;
                    case "Join Match":
                        //since this is now a new socket handler object, gotta redo the ourPlayer
                        ourPlayer = Globals.playerMapping[messageData.username];
                        //and now, change the socket dictionary to reflect this too
                        Globals.socketPlayerMapping.Add(ourPlayer, this);

                        Match ourMatch = null;
                        if (messageData.server_id == null)
                        {
                            message = "No hash ID given!";
                        }
                        else
                        {
                            if (Globals.matches.TryGetValue(messageData.server_id, out ourMatch))
                            {
                                message = "Connected to the Match!";
                                success = true;
                            }
                            else
                            {
                                message = "The Hash ID was not found!";
                            }
                        }
                        plantedMessage = new
                        {
                            success = success,
                            message = message,
                            type = "Confirmation"
                        };
                        await SendPacket();
                        //this way, we first send the confirmation method first, even if say this is the last client to connect and thus the match should start
                        if (success)
                        {
                            ourMatch.AddConnection();
                        }
                        break;
                    case "New Message":
                        if (messageData.server_id == null)
                        {
                            message = "No hash ID given!";
                        }
                        else
                        {
                            if (Globals.matches.TryGetValue(messageData.server_id, out Match theMatch))
                            {
                                theMatch.SendOutNewMessage(messageData.username, messageData.saidMessage);
                                message = "Message Sent Successfully!";
                            }
                            else
                            {
                                message = "The Hash ID was not found!";
                            }
                        }
                        plantedMessage = new
                        {
                            success = success,
                            message = message,
                            type = "Confirmation"
                        };
                        await SendPacket();
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

        async public Task HandleSocket()
        {
            while (socket.State == WebSocketState.Open)
            {
                byte[] buffer = new byte[5000];
                //array segment is just way to specify where to write in the buffer, by default index 0 is start and count is end of array
                try
                {
                    WebSocketReceiveResult receivedData = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    await ProcessReceivedData(buffer, receivedData);
                }
                //when twaiting is interrupted by a command to send data 
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task GoToSendMessage(object message)
        {
            plantedMessage = message;
            SendPacket();
        }
    }
}