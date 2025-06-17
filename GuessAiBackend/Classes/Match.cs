using System.Diagnostics;

namespace Classes
{
    public class Match
    {
        private Player[] players;
        private byte roundNumber;
        private readonly byte MAX_ROUND_NUM;
        private String gamemode;
        //2D list where each outer one is for each round, and each inner one is the messages sent at that round
        List<String>[] messages;
        private String phase;

        private String hashCode;

        private const byte SEC_PER_ROUND = 5;
        private const byte SEC_PER_VOTE = 15;

        Stopwatch timer;
        public Match(Player[] thePlayers, String theGamemode)
        {
            players = thePlayers;
            roundNumber = 0;
            MAX_ROUND_NUM = (byte)(thePlayers.Length - 1);
            gamemode = theGamemode;
            messages = new List<String>[MAX_ROUND_NUM];
            phase = "talking";
            timer = new Stopwatch();
            timer.Start();
            hashCode = Guid.NewGuid().ToString();
        }

        //we want the user to hold onto something that tells them which match they're in, and then using that have them be able to add the message very quickly.
        //through finding the match very quickly given the id

        public async Task RunTalk()
        {
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_ROUND)
            {
                Task.Yield();
            }
            phase = "voting";
            foreach (Player plr in players)
            {
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];
                object packet = new
                {
                    message = "Voting Time",
                    type = "Server Event",
                    server_id = hashCode
                };
                associatedSocket.GoToSendMessage(packet);
            }
            RunVoting();
        }

        public async Task RunVoting()
        {
            timer.Restart();
            while (timer.ElapsedMilliseconds < 1000 * SEC_PER_VOTE)
            {
                Task.Yield();
            }
            phase = "talking";
            foreach (Player plr in players)
            {
                SocketHandler associatedSocket = Globals.socketPlayerMapping[plr];
                object packet = new
                {
                    message = "Voting Time",
                    type = "Server Event",
                    server_id = hashCode
                };
                associatedSocket.GoToSendMessage(packet);
            }
            roundNumber++;
            if (roundNumber == MAX_ROUND_NUM)
            {
                //end game!
                return;
            }
            RunTalk();
        }

        public String GetHash()
        {
            return hashCode;
        }
    }
}