namespace Classes
{
    public static class Globals
    {
        public static GameQueue oneBotQueue = new GameQueue("One Bot Game", 4);
        public static Dictionary<Player, SocketHandler> socketPlayerMapping = new Dictionary<Player, SocketHandler>();
        public static Dictionary<String, Match> matches = new Dictionary<string, Match>();

        public static Dictionary<String, Player> playerMapping = new Dictionary<string, Player>();
    }
}
