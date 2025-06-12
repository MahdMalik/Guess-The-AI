using System.Collections.Generic;
namespace Objects
{
    public class GameQueue
    {
        //we use a linked list instead of a queue so when removing, just by getting the username, we can find the node in the list by key-value pair, and remove in O(1) time
        private LinkedList<Player> queue;
        private Dictionary<String, LinkedListNode<Player>> playerMapping;
        private String queueType;
        //using unsigned short as queue size can never be negative, and we likely won't have many in the queue at once.
        private ushort size;

        private byte playersPerMatch;
        public GameQueue(String type, byte perMatch)
        {
            queue = new LinkedList<Player>();
            playerMapping = new Dictionary<String, LinkedListNode<Player>>();
            queueType = type;
            size = 0;
            playersPerMatch = perMatch;
        }

        //adds player to queue. can add either at start or end if there's an issue.
        public (bool success, String message) AddPlayer(Player plr, bool atStart)
        {
            //making sure it won't cause a buffer overflow by adding the player
            if (size == ushort.MaxValue)
            {
                return (false, "too many players, would overflow!");
            }

            //if not at start (say for an error), add at end
            if (!atStart)
            {
                //first, add them to the queue, that returns a node you add to the dictionary
                playerMapping.Add(plr.GetName(), queue.AddLast(plr));
            }
            else
            {
                //first add to queue, that returns a node you add to dictionary
                playerMapping.Add(plr.GetName(), queue.AddFirst(plr));
            }
            size++;
            return (true, "success");
        }

        //remvoes player from queue, just given node that's passed in.
        public (bool success, String message, Player removedPlayer) RemovePlayer(LinkedListNode<Player> playerNode)
        {
            //first check if invalid player node was passed in
            if (playerNode == null)
            {
                return (false, "player not found in dictionary!", null);
            }
            //gotta make sure there are actually players to remove
            if (size == 0)
            {
                return (false, "no players in queue!", null);
            }
            //remove, decrement size, and remove from dcitionary too
            queue.Remove(playerNode);
            size--;
            playerMapping.Remove(playerNode.Value.GetName());
            return (true, "success", playerNode.Value);
        }

        //given a name, removes the player
        public (bool success, String message, Player removedPlayer) RemovePlayer(String name)
        {
            try
            {
                return RemovePlayer(playerMapping[name]);
            }
            catch (KeyNotFoundException error)
            {
                return (false, "Error removing player: " + error.Message, null);
            }
        }

        //removes the players from the start of the queue to start the game
        public (bool success, String message, Player[] playersInMatch) GetPlayersForMatch()
        {
            //set up returned array
            Player[] returnedPlayers = new Player[playersPerMatch];
            //use for loop to add each player
            for (byte i = 0; i < playersPerMatch; i++)
            {
                //pass in you want to remove teh first player, get results here
                var removeResult = RemovePlayer(queue.First);
                //if it worked, add them to the array
                if (removeResult.success)
                {
                    returnedPlayers[i] = removeResult.removedPlayer;
                }
                //if it didn't work, add them back to the start of the queue
                else
                {
                    //in reverse order of the outer for loop, add the remaining players back. Assume for current
                    //player, since there was an error returned, they weren't even removed
                    for (sbyte j = (sbyte)(i - 1); j >= 0; j--)
                    {
                        AddPlayer(returnedPlayers[j], true);
                    }
                    return (false, "failed to remove a player: " + removeResult.message, null);
                }
            }
            return (true, "success", returnedPlayers);
        }
    }
}