namespace Classes
{
    public class MatchMessage
    {
        String message;
        String user;
        public MatchMessage(String theMessage, String username)
        {
            message = theMessage;
            user = username;
        }

        public String GetMessage()
        {
            return message;
        }
        public String GetUsername()
        {
            return user;
        }
    }
}