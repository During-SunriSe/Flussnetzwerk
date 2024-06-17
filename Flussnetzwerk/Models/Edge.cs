namespace FlowNetworkWPF.Models
{
    public class Edge
    {
        public Node From { get; set; }
        public Node To { get; set; }
        public int Capacity { get; set; }
        public int Flow { get; set; }

        public Edge(Node from, Node to, int capacity)
        {
            From = from;
            To = to;
            Capacity = capacity;
            Flow = 0;
        }

        public int ResidualCapacity()
        {
            return Capacity - Flow;
        }
    }
}
