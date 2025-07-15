using System.Data;
using System.Diagnostics.Eventing.Reader;
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
        private Player? ourPlayer;

        public static GameQueue GetQueueFromName(String queueName)
        {
            switch (queueName)
            {
                case "One Bot Game":
                    return Globals.oneBotQueue;
                default:
                    return null;
            }
        }

        public static Match GetMatchFromId(String id, ref String details)
        {
            Match returnedMatch = null;
            if (id == null)
            {
                details = "No hash ID given!";
            }
            else
            {
                if (Globals.matches.TryGetValue(id, out returnedMatch))
                {
                    details = "Getting The Match And Doing the Command Worked!";
                }
                else
                {
                    details = "The Hash ID was not found!";
                }
            }
            return returnedMatch;
        }

        public SocketHandler(WebSocket theSocket)
        {
            socket = theSocket;
        }

        //sends the packet after decoding it according to the correct process.
        async public Task SendPacket(ServerMessage sendingMessage)
        {
            string jsonMessage = JsonSerializer.Serialize(sendingMessage);
            byte[] bufferMessage = Encoding.UTF8.GetBytes(jsonMessage);
            await socket.SendAsync(new ArraySegment<byte>(bufferMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine("Message Sent!");
        }

        //processes whatever it's received from the user
        async public Task ProcessReceivedData(byte[] buffer, WebSocketReceiveResult receivedData)
        {
            //if got a request to close the connection instead, should do it
            if (receivedData.CloseStatus != null && receivedData.CloseStatusDescription == "Game Over")
            {
                Console.WriteLine("Client closed connection this time!");
                Globals.socketPlayerMapping.Remove(ourPlayer);
                Globals.playerMapping.Remove(ourPlayer.GetName());
            }
            //otherwise, make sure we got one related to text
            else if (receivedData.MessageType == WebSocketMessageType.Text)
            {
                //first convert bytes to json
                string jsonData = Encoding.UTF8.GetString(buffer, 0, receivedData.Count);
                //now convert it to a class
                ClientMessage? messageData = JsonSerializer.Deserialize<ClientMessage>(jsonData);

                //if actually no message, close the socket and tell the client it failed
                if (messageData == null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "error deseralizing json", CancellationToken.None);
                    return;
                }

                bool success = false;
                String details = "";
                Match ourMatch = null;
                ServerMessage sendingMessage = new ServerMessage();

                GameQueue theQueue;

                //after sending the confirmation message, we have to also send in-order something else, so we make thing string
                //to tell us what to do afterwards.
                String commandAfterwards = "";

                switch (messageData.messageType)
                {
                    //if the message is to join a queue, do this:
                    case "Join Queue":
                        //first, create our player, then put them into the queue
                        ourPlayer = new Player(messageData.username);
                        Globals.playerMapping.Add(ourPlayer.GetName(), ourPlayer);
                        //get the right queue from the name passed in
                        theQueue = GetQueueFromName(messageData.queueType);
                        //if queue is null, we should end the connection with the error
                        if (theQueue == null)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "queue type didn't match", CancellationToken.None);
                        }
                        //otherwise, add the player, and if they're added, now add them to the socket queue too
                        (success, details) = Globals.oneBotQueue.AddPlayer(ourPlayer, false);
                        if (success)
                        {
                            Globals.socketPlayerMapping.Add(ourPlayer, this);
                        }
                        else
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "player couldnt' be added due to reason: " + details, CancellationToken.None);
                        }
                        break;
                    case "Leave Queue":
                        theQueue = GetQueueFromName(messageData.queueType);
                        //if queue is null, we should end the connection with the error
                        if (theQueue == null)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "queue type didn't match", CancellationToken.None);
                        }

                        (success, details, ourPlayer) = Globals.oneBotQueue.RemovePlayer(messageData.username);

                        if (success)
                        {
                            //have to remove our player from here as well
                            Globals.socketPlayerMapping.Remove(ourPlayer);
                            Globals.playerMapping.Remove(ourPlayer.GetName());
                            details += ", removed player: " + ourPlayer.GetName();
                        }
                        else
                        {
                            Console.WriteLine("UHHH WE COULDN'T REMOVE PLAYER, I THINK THAT'S BAD??");
                        }
                        break;
                    case "Join Match":
                        //since this is now a new socket handler object, gotta redo the ourPlayer
                        ourPlayer = Globals.playerMapping[messageData.username];
                        //and now, change the socket dictionary to reflect this too
                        Globals.socketPlayerMapping.Add(ourPlayer, this);
                        String[] playerNames = null;
                        ourMatch = GetMatchFromId(messageData.server_id, ref details);
                        if (ourMatch != null)
                        {
                            success = true;
                            playerNames = ourMatch.ReturnPlayers();
                            sendingMessage.names = playerNames;
                            commandAfterwards = "AddConnection";
                        }
                        else
                        {
                            Console.WriteLine("Error was: " + details);
                        }
                        break;
                    case "New Message":
                        ourMatch = GetMatchFromId(messageData.server_id, ref details);
                        if (ourMatch != null)
                        {
                            success = true;
                            commandAfterwards = "SendNewMessage";
                        }
                        else
                        {
                            Console.WriteLine("Error was: " + details);
                        }
                        break;
                    case "Add Vote":
                        ourMatch = GetMatchFromId(messageData.server_id, ref details);
                        if (ourMatch != null)
                        {
                            success = true;
                            commandAfterwards = "ReceiveVote";
                        }
                        else
                        {
                            Console.WriteLine("Error was: " + details);
                        }
                        break;
                    default:
                        await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "message type wasn't found", CancellationToken.None);
                        break;
                }
                sendingMessage.success = success;
                sendingMessage.details = details;
                sendingMessage.message = "Confirmation";
                await SendPacket(sendingMessage);

                //if we have something else to do afterwards, then we need to do it
                if (success && commandAfterwards != "")
                {
                    //go through all possible commands and do the one that is requierd to be done
                    switch (commandAfterwards)
                    {
                        case "AddConnection":
                            ourMatch.AddConnection();
                            break;
                        case "SendNewMessage":
                            ourMatch.SendOutNewMessage(messageData.username, messageData.saidMessage);
                            break;
                        case "ReceiveVote":
                            ourMatch.ReceiveVote(messageData.votedPerson);
                            break;
                        default:
                            break;
                    }
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
            //while it's open, continue to listen
            while (socket.State == WebSocketState.Open)
            {
                byte[] buffer = new byte[5000];
                //array segment is just way to specify where to write in the buffer, by default index 0 is start and count is end of array

                WebSocketReceiveResult receivedData = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                await ProcessReceivedData(buffer, receivedData);
            }
        }
    }
}