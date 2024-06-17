using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowNetworkWPF.Models
{
    public class Graph
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Edge> Edges { get; set; } = new List<Edge>();

        public void AddEdge(Node from, Node to, int capacity)
        {
            var edge = new Edge(from, to, capacity);
            Edges.Add(edge);
            Edges.Add(new Edge(to, from, 0)); // Add a reverse edge with 0 capacity
        }

        public List<Edge> GetEdgesFromNode(Node node)
        {
            return Edges.Where(e => e.From == node).ToList();
        }

        public Edge GetEdge(Node from, Node to)
        {
            return Edges.FirstOrDefault(e => e.From == from && e.To == to);
        }

        public int FordFulkerson(Node source, Node sink)
        {
            int maxFlow = 0;

            while (true)
            {
                var path = FindAugmentingPath(source, sink);
                if (path == null)
                    break;

                int pathFlow = int.MaxValue;
                foreach (var edge in path)
                {
                    pathFlow = Math.Min(pathFlow, edge.ResidualCapacity());
                }

                foreach (var edge in path)
                {
                    edge.Flow += pathFlow;
                    var reverseEdge = GetEdge(edge.To, edge.From);
                    reverseEdge.Flow -= pathFlow; // Properly update reverse edge flow
                }

                maxFlow += pathFlow;
            }

            return maxFlow;
        }

        public List<Edge> FindAugmentingPath(Node source, Node sink)
        {
            var parentMap = new Dictionary<Node, Edge>();
            var visited = new HashSet<Node>();
            var queue = new Queue<Node>();

            queue.Enqueue(source);
            visited.Add(source);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node == sink)
                    break;

                foreach (var edge in GetEdgesFromNode(node))
                {
                    if (edge.ResidualCapacity() > 0 && !visited.Contains(edge.To))
                    {
                        visited.Add(edge.To);
                        parentMap[edge.To] = edge;
                        queue.Enqueue(edge.To);
                    }
                }
            }

            if (!parentMap.ContainsKey(sink))
                return null;

            var path = new List<Edge>();
            var currentNode = sink;

            while (currentNode != source)
            {
                var edge = parentMap[currentNode];
                path.Add(edge);
                currentNode = edge.From;
            }

            path.Reverse();
            return path;
        }
    }
}
