namespace FlowNetworkWPF.Models
{
    public class Node
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Node(string id, double x, double y)
        {
            Id = id;
            X = x;
            Y = y;
        }
    }
}
