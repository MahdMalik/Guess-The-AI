namespace Classes
{
    public class Player
    {
        private bool inQueue;
        private bool inGame;
        private String username;

        //every time we create a new player, that's just meaning they've been added to the queue. We get rid of them at the end of a match or if they leave the queue.
        public Player(String name)
        {
            inQueue = true;
            inGame = false;
            username = name;
        }

        public String GetName()
        {
            return username;
        }

        public void SetInQueue(bool newStatus)
        {
            inQueue = newStatus;
        }

        public void SetInMatch(bool newStatus)
        {
            inGame = newStatus;
        }
    }
}