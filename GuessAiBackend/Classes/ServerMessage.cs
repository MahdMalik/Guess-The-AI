namespace Classes
{
    public class ServerMessage
    {
        public bool? success { get; set; }
        public String? message { get; set; }
        public String? server_id { get; set; }
        public String? sender { get; set; }
        public String? sentMessage { get; set; }
        public String? voted_person { get; set; }
        public String? winner { get; set; }
        public String? details { get; set; }
        public String[]? names { get; set; }
        public HashSet<String>[]? votes { get; set; }
        public byte? num_voted { get; set; }
        public bool? fair_voted_out { get; set; }
    }
}