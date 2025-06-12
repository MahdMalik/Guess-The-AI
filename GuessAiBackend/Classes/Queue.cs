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

        public (bool success, String message) AddPlayer(Player plr)
        {
            //making sure it won't cause a buffer overflow by adding the player
            if (size == ushort.MaxValue)
            {
                return (false, "too many players, would overflow!");
            }

            //first, add them to the queue, that returns a node you add to the dictionary
            playerMapping.Add(plr.GetName(), queue.AddLast(plr));
            size++;
            return (true, "success");
        }

        //given a name, removes the player
        public (bool success, String message, Player removedPlayer) RemovePlayer(String name)
        {
            //gotta make sure there are actually players to remove
            if (size == 0)
            {
                return (false, "no players in queue!", null);
            }
            LinkedListNode<Player> removedPlayer = playerMapping[name];
            if (removedPlayer == null)
            {
                return (false, "player not found in dictionary!", null);
            }
            queue.Remove(removedPlayer);
            size--;
            playerMapping.Remove(name);
            return (true, "success", removedPlayer.Value);
        }

        public (bool success, String message, Player[] playersInMatch) GetPlayersForMatch()
        {
            
        }
    }
}