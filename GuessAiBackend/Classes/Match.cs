namespace Classes
{
    public class Match
    {
        private Player[] players;
        private byte roundNumber;
        private readonly byte MAX_ROUND_NUM;
        private String gamemode;
        public Match(Player[] thePlayers, String theGamemode)
        {
            players = thePlayers;
            roundNumber = 1;
            MAX_ROUND_NUM = (byte)(thePlayers.Length - 1);
            gamemode = theGamemode;
        }

    }
}