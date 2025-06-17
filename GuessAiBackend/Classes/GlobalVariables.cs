namespace Classes
{
    public static class Globals
    {
        public static GameQueue oneBotQueue = new GameQueue("One Bot Game", 2);
        public static Dictionary<Player, SocketHandler> socketPlayerMapping = new Dictionary<Player, SocketHandler>();
        public static Dictionary<String, Match> matches = new Dictionary<string, Match>();
    }
}
