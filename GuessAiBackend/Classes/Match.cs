using System.Diagnostics;

namespace Classes
{
    public class Match
    {
        LinkedList<Player> players;
        String[] initialPlayerNames;
        private byte roundNumber;
        private readonly byte MAX_ROUND_NUM;
        private String gamemode;
        //2D list where each outer one is for each round, and each inner one is the messages sent at that round
        List<MatchMessage>[] messages;
        private String phase;

        private String hashCode;

        private const byte SEC_PER_ROUND = 15;
        private const byte SEC_PER_VOTE = 15;

        Stopwatch timer;

        private byte numConnections;

        //how it works is each index corresponds to one more  than the current votes, so index 0 means 1 vote, at least
        //in the array. Then, each element in the array is a hashset of players who have that # of votes.
        private HashSet<String>[] votes;

        private Dictionary<String, byte> playerVoteMapping;
        public Match(LinkedList<Player> thePlayers, String theGamemode)
        {
            players = thePlayers;
            votes = new HashSet<String>[thePlayers.Count];
            for (byte i = 0; i < votes.Length; i++)
            {
                votes[i] = new HashSet<string>();
            }
            playerVoteMapping = new Dictionary<String, byte>();
            roundNumber = 0;
            numConnections = 0;
            MAX_ROUND_NUM = (byte)(thePlayers.Count - 1);
            gamemode = theGamemode;
            messages = new List<MatchMessage>[MAX_ROUND_NUM];
            for (byte i = 0; i < messages.Length; i++)
            {
                messages[i] = new List<MatchMessage>();
            }
            phase = "talking";
            timer = new Stopwatch();
            timer.Start();
            hashCode = Guid.NewGuid().ToString();

            initialPlayerNames = new String[players.Count];
            byte index = 0;
            foreach (Player plr in players)
            {
                initialPlayerNames[index] = plr.GetName();
                index++;
            }
        }

        //we want the user to hold onto something that tells them which match they're in, and then using that have them be able to add the message very quickly.
        //through finding the match very quickly given the id

        public async Task RunTalk()
        {
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_ROUND)
            {
                //yield back task to other places that may need it
                await Task.Yield();
            }
            phase = "voting";
            foreach (Player plr in players)
            {
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];
                object packet = new
                {
                    message = "Voting Time",
                    server_id = hashCode,
                    success = true
                };
                associatedSocket.GoToSendMessage(packet);
            }
            RunVoting();
        }

        public void ReceiveMessage(String username, String message)
        {
            if (phase == "voting")
            {
                return;
            }
            MatchMessage newMessage = new MatchMessage(message, username);
            messages[roundNumber].Add(newMessage);
            SendOutNewMessage(username, message);
        }

        //this actually sends out the message to all users such that it's added
        public async void SendOutNewMessage(String sendingUser, String message)
        {
            foreach (Player plr in players)
            {
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];
                object packet = new
                {
                    sentMessage = message,
                    server_id = hashCode,
                    message = "Message Arrived",
                    sender = sendingUser,
                    success = true
                };
                associatedSocket.GoToSendMessage(packet);
            }
        }

        public async void ReceiveVote(String chosenUser)
        {
            //check if it's already been put into the hashmap. If not, means it currently has 0 votes.
            if (playerVoteMapping.ContainsKey(chosenUser))
            {
                //if it does have the key, remove them from the current hashset and put them in the nexct one above
                votes[playerVoteMapping[chosenUser]].Remove(chosenUser);
                votes[playerVoteMapping[chosenUser] + 1].Add(chosenUser);
                //then, update where they are found
                playerVoteMapping[chosenUser] = (byte)(playerVoteMapping[chosenUser] + 1);
            }
            else
            {
                votes[0].Add(chosenUser);
                playerVoteMapping.Add(chosenUser, 0);
            }
            Console.WriteLine("Vote Added!");
        }

        public async Task RunVoting()
        {
            timer.Restart();
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_VOTE)
            {
                await Task.Yield();
            }
            phase = "talking";

            String votedUser = "";
            //now, do the actual voting someone out part.
            for (sbyte i = (sbyte)(votes.Length - 1); i >= 0; i--)
            {
                if (votes[i].Count > 0)
                {
                    votedUser = votes[i].First<String>();
                    break;
                }
            }

            object packet;
            bool gameOver = false;
            roundNumber++;

            object nonVotedPacket;

            if (roundNumber == MAX_ROUND_NUM)
            {
                //end game!
                Console.WriteLine("Game has ended now!");
                gameOver = true;
                nonVotedPacket = new
                {
                    message = "Game Over",
                    voted_person = votedUser,
                    winner = "Players Win!",
                    type = "Server Event",
                    server_id = hashCode,
                    success = true
                };
            }
            else
            {
                nonVotedPacket = new
                {
                    message = "Discussion Time",
                    voted_person = votedUser,
                    type = "Server Event",
                    server_id = hashCode,
                    success = true
                };
            }

            LinkedListNode<Player> plrNode = players.First;
            while (plrNode != null)
            {
                LinkedListNode<Player> nextNode = plrNode.Next;
                if (plrNode.Value.GetName() == votedUser)
                {
                    //kick them out
                    packet = new
                    {
                        message = "Voted Out",
                        type = "Server Event",
                        server_id = hashCode,
                        success = true
                    };
                    players.Remove(plrNode);
                }
                else
                {
                    packet = nonVotedPacket;
                }

                SocketHandler associatedSocket = Globals.socketPlayerMapping[plrNode.Value];
                associatedSocket.GoToSendMessage(packet);
                plrNode = nextNode;
            }

            //reset our voting objects
            votes = new HashSet<String>[players.Count];
            playerVoteMapping = new Dictionary<String, byte>();
            for (byte i = 0; i < votes.Length; i++)
            {
                votes[i] = new HashSet<string>();
            }

            //if the game's not over, go back to discussion method
            if (!gameOver)
            {
                RunTalk();
            }
        }

        public String GetHash()
        {
            return hashCode;
        }

        public void AddConnection()
        {
            Console.WriteLine("Match added connection!");
            numConnections++;
            if (numConnections == players.Count)
            {
                Console.WriteLine("We are Ready!");
                foreach (Player plr in players)
                {
                    SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];
                    object packet = new
                    {
                        message = "Game Start! Discussion First",
                        server_id = hashCode,
                        success = true
                    };
                    associatedSocket.GoToSendMessage(packet);
                }
                RunTalk();
            }
        }

        public String[] ReturnPlayers()
        {
            return initialPlayerNames;
        }
    }
}