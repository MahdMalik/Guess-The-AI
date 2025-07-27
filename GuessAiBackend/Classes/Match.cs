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
        private const byte SEC_PER_INTERMISSION = 10;

        Stopwatch timer;

        private byte numConnections;

        //how it works is each index corresponds to one more  than the current votes, so index 0 means 1 vote, at least
        //in the array. Then, each element in the array is a hashset of players who have that # of votes.
        private HashSet<String>[] votes;

        private Dictionary<String, byte> playerVoteMapping;

        private HashSet<String> playersWhoNotVote;

        private byte playersVoted;

        private Random rng;
        //simply creates the match
        public Match(LinkedList<Player> thePlayers, String theGamemode)
        {
            players = thePlayers;
            ResetVotes();
            roundNumber = 0;
            numConnections = 0;
            //the game alwas must end with 2 players. So, if one is voted out each round, logically for n players takes n - 2 rounds to get to 2 players left.
            //For now we're putting it as n - 1 so we can play a game with just 2 players.
            MAX_ROUND_NUM = (byte)(thePlayers.Count - 1);
            gamemode = theGamemode;
            //crerate the array of lists containing the messages
            messages = new List<MatchMessage>[MAX_ROUND_NUM];
            //for each list, initialize it
            for (byte i = 0; i < messages.Length; i++)
            {
                messages[i] = new List<MatchMessage>();
            }
            //set start phase
            phase = "talking";
            timer = new Stopwatch();
            timer.Start();
            //get a hashcode to send to the players
            hashCode = Guid.NewGuid().ToString();

            //need this just for when a client asks for it
            initialPlayerNames = new String[players.Count];
            byte index = 0;
            //now storing the player names
            foreach (Player plr in players)
            {
                initialPlayerNames[index] = plr.GetName();
                index++;
            }
            rng = new Random();
        }

        public async Task RunTalk()
        {
            //spin waiting for discussion timee to end
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_ROUND)
            {
                //yield back task to other places that may need it
                await Task.Yield();
            }
            phase = "voting";
            //for each player, send them the message that it's time to vote
            foreach (Player plr in players)
            {
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];

                ServerMessage packet = new ServerMessage();
                packet.message = "Voting Time";
                packet.server_id = hashCode;
                packet.success = true;
                
                associatedSocket.SendPacket(packet);
            }
            //now go run the voting
            RunVoting();
        }

        public void ReceiveMessage(String username, String message)
        {
            //don't want to update messages when voting starts. If you send a message last minute then that's a skill issue gg
            if (phase == "voting")
            {
                return;
            }
            //otherwise, add it to the messages and send it out to the rest of the clients
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

                ServerMessage packet = new ServerMessage();
                packet.sentMessage = message;
                packet.server_id = hashCode;
                packet.message = "Message Arrived";
                packet.sender = sendingUser;
                packet.success = true;

                associatedSocket.SendPacket(packet);
            }
        }

        private async void AddVoteToUser(String chosenUser)
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
            //in this case, an index of 0 means 1 vote, don't be fooled.
            else
            {
                playersVoted++;
                votes[0].Add(chosenUser);
                playerVoteMapping.Add(chosenUser, 0);
            }
        }

        public async void ReceiveVote(String chosenUser, String sendingUser)
        {
            //remove them cause now they have voted
            playersWhoNotVote.Remove(sendingUser);

            AddVoteToUser(chosenUser);

            Console.WriteLine("Vote Added!");
        }

        public async Task RunVoting()
        {
            //reset the stopwatch usied during discussion time, and wait until voting phase is over
            timer.Restart();
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_VOTE)
            {
                await Task.Yield();
            }
            phase = "talking";

            //we need to find the players who have yet to vote, as stored in "playersWhoNotVote". Now, we need to
            //give them an extra vote since they hadn't voted.
            foreach (String plr in playersWhoNotVote)
            {
                Console.WriteLine($"Since {plr} didn't vote, give them a vote!");
                AddVoteToUser(plr);
            }

            String votedUser = "";

            bool fair = true;

            //now, do the actual voting someone out part.
            for (sbyte i = (sbyte)(votes.Length - 1); i >= 0; i--)
            {

                //for now we do this, but we'll have to have some handling for ties.
                if (votes[i].Count > 1)
                {
                    //means we have a tie, and now we need to break it!
                    HashSet<String> tiedPlayers = votes[i];
                    //if it's a full tie, we need to break it randomly.
                    if (votes[i].Count == players.Count)
                    {
                        //only losers will try to exploit this by always being the first ones to join the game ngl. Not even
                        //sure if it's possible to exploit if you don't know your position in the matc. Also ties are extremely rare,
                        //such that i don't think it's worth the extra runtime to iterate through the set to a random index.
                        votedUser = votes[i].Last<String>();
                        fair = false;
                    }
                }
                else if (votes[i].Count == 1)
                {
                    votedUser = votes[i].First<String>();
                    break;
                }
            }

            ServerMessage packet;
            bool gameOver = false;
            roundNumber++;

            ServerMessage nonVotedPacket = new ServerMessage();
            nonVotedPacket.voted_person = votedUser;
            nonVotedPacket.votes = votes;
            nonVotedPacket.server_id = hashCode;
            nonVotedPacket.success = true;
            nonVotedPacket.message = "Person Voted Out";
            nonVotedPacket.num_voted = playersVoted;
            nonVotedPacket.fair_voted_out = fair;

            //iterate through all the nodes with this iterator way, because in a foreach or for loop we can't just remove it
            //and be done with it
            LinkedListNode<Player> plrNode = players.First;
            while (plrNode != null)
            {
                LinkedListNode<Player> nextNode = plrNode.Next;
                //if it's the user being voted out, wipe it out 
                if (plrNode.Value.GetName() == votedUser)
                {
                    //kick them out
                    packet = new ServerMessage();
                    packet.message = "Voted Out";
                    packet.server_id = hashCode;
                    packet.success = true;
                    packet.fair_voted_out = fair;

                    players.Remove(plrNode);
                }
                else
                {
                    packet = nonVotedPacket;
                }

                SocketHandler associatedSocket = Globals.socketPlayerMapping[plrNode.Value];
                associatedSocket.SendPacket(packet);

                plrNode = nextNode;
            }

            //reset our voting data
            ResetVotes();

            timer.Restart();
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_INTERMISSION)
            {
                await Task.Yield();
            }

            ServerMessage nextPacket = new ServerMessage();
            nextPacket.server_id = hashCode;
            nextPacket.success = true;

            //means it should be the final round, we over
            if (roundNumber == MAX_ROUND_NUM)
            {
                //end game!
                Console.WriteLine("Game has ended now!");
                gameOver = true;
                nextPacket.message = "Game Over";
                nextPacket.winner = "Players Win!";
            }
            //otherwise, create the packet that will be sent to the players that weren't voted
            else
            {
                nextPacket.message = "Discussion Time";
            }

            //iterate through all the nodes with this iterator way, because in a foreach or for loop we can't just remove it
            //and be done with it
            plrNode = players.First;
            while (plrNode != null)
            {
                LinkedListNode<Player> nextNode = plrNode.Next;
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plrNode.Value];
                associatedSocket.SendPacket(nextPacket);

                plrNode = nextNode;
            }


            //if the game's not over, go back to discussion method
            if (!gameOver)
            {
                RunTalk();
            }
        }

        public void ResetVotes()
        {
            //creates the empty array of hashsets
            votes = new HashSet<String>[players.Count];

            //gotta reset it.
            playersWhoNotVote = new HashSet<string>();
            foreach (Player plr in players)
            {
                playersWhoNotVote.Add(plr.GetName());
            }
            //for each hashet, initialize it
                for (byte i = 0; i < votes.Length; i++)
                {
                    votes[i] = new HashSet<string>();
                }
            playerVoteMapping = new Dictionary<String, byte>();
            playersVoted = 0;
        }

        public String GetHash()
        {
            return hashCode;
        }

        public void AddConnection()
        {
            Console.WriteLine("Match added connection!");
            numConnections++;
            //once everybody's connected, send the message to them to start the game.
            if (numConnections == players.Count)
            {
                Console.WriteLine("We are Ready!");

                ServerMessage packet = new ServerMessage();
                packet.message = "Game Start! Discussion First";
                packet.server_id = hashCode;
                packet.success = true;

                foreach (Player plr in players)
                {
                    SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];


                    associatedSocket.SendPacket(packet);
                }
                RunTalk();

                //free the memory since we no longer need it
                initialPlayerNames = null;
            }
        }

        public String[] ReturnPlayers()
        {
            return initialPlayerNames;
        }
    }
}