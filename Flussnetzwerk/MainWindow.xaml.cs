using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FlowNetworkWPF.Models;

namespace FlowNetworkWPF
{
    public partial class MainWindow : Window
    {
        private Graph graph;
        private Dictionary<string, Node> nodesDict;
        private DispatcherTimer timer;
        private Ellipse flowPoint;
        private List<Edge> currentPath;
        private int currentPathIndex;
        private double stepSize;
        private double currentX;
        private double currentY;
        private int totalFlow;
        private bool isAddingNode = false;

        public MainWindow()
        {
            InitializeComponent();
            graph = new Graph();
            nodesDict = new Dictionary<string, Node>();
            InitializeDefaultNodes();
            DrawGraphs();
        }

        private void InitializeDefaultNodes()
        {
            var source = new Node("s", 50, 200);
            var sink = new Node("t", 500, 200);
            nodesDict["s"] = source;
            nodesDict["t"] = sink;
            graph.Nodes.Add(source);
            graph.Nodes.Add(sink);
        }

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            isAddingNode = true;
            InstructionTextBlock.Text = "Click on the canvas to place the node.";
            InstructionTextBlock.Visibility = Visibility.Visible;
        }
        private void Clear_Click(object sender, RoutedEventArgs e) { ClearGraph();  }

        private void FlowCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isAddingNode)
            {
                var position = e.GetPosition(FlowCanvas);
                var textBlock = new TextBlock
                {
                    Text = $"({position.X:F0}, {position.Y:F0})",
                    FontSize = 16,
                    Background = Brushes.White
                };

                FlowCanvas.Children.Clear();
                DrawFlowGraph();
                Canvas.SetLeft(textBlock, position.X + 10);
                Canvas.SetTop(textBlock, position.Y + 10);
                FlowCanvas.Children.Add(textBlock);
            }
        }

        private void FlowCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isAddingNode)
            {
                var position = e.GetPosition(FlowCanvas);
                var addNodeWindow = new AddNodeWindow(position.X, position.Y);
                if (addNodeWindow.ShowDialog() == true)
                {
                    string nodeId = addNodeWindow.NodeId;
                    double x = addNodeWindow.X;
                    double y = addNodeWindow.Y;
                    var node = new Node(nodeId, x, y);
                    nodesDict[nodeId] = node;
                    graph.Nodes.Add(node);
                    DrawGraphs();
                }
                isAddingNode = false;
                InstructionTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void AddEdge_Click(object sender, RoutedEventArgs e)
        {
            var addEdgeWindow = new AddEdgeWindow(nodesDict.Keys.ToList());
            if (addEdgeWindow.ShowDialog() == true)
            {
                string fromNodeId = addEdgeWindow.FromNodeId;
                string toNodeId = addEdgeWindow.ToNodeId;
                int capacity = addEdgeWindow.Capacity;

                // Validation: Check if fromNodeId and toNodeId are the same
                if (fromNodeId == toNodeId)
                {
                    MessageBox.Show("Cannot create an edge from a node to itself.", "Invalid Edge", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fromNode = nodesDict[fromNodeId];
                var toNode = nodesDict[toNodeId];
                graph.AddEdge(fromNode, toNode, capacity);
                DrawGraphs();
            }
        }

        private void DrawGraphs()
        {
            DrawFlowGraph();
            DrawResidualGraph(); // Always draw nodes and edges in the residual graph
        }

        private void DrawFlowGraph()
        {
            FlowCanvas.Children.Clear();

            foreach (var edge in graph.Edges)
            {
                DrawEdge(edge, FlowCanvas, Brushes.Gray);
            }

            foreach (var node in graph.Nodes)
            {
                DrawNode(node, FlowCanvas);
            }

            // Draw the flow point
            if (flowPoint != null)
            {
                FlowCanvas.Children.Add(flowPoint);
                Canvas.SetLeft(flowPoint, currentX - flowPoint.Width / 2);
                Canvas.SetTop(flowPoint, currentY - flowPoint.Height / 2);
            }
        }

        private void DrawResidualGraph()
        {
            ResidualCanvas.Children.Clear();
            var drawnEdges = new HashSet<Tuple<Node, Node>>();

            // Draw nodes first
            foreach (var node in graph.Nodes)
            {
                DrawNode(node, ResidualCanvas);
            }

            // Draw edges
            foreach (var edge in graph.Edges)
            {
                var forwardEdge = Tuple.Create(edge.From, edge.To);
                var reverseEdge = Tuple.Create(edge.To, edge.From);

                if (!drawnEdges.Contains(forwardEdge))
                {
                    DrawResidualEdge(edge, ResidualCanvas, Brushes.Red);
                    drawnEdges.Add(forwardEdge);
                }

                if (!drawnEdges.Contains(reverseEdge))
                {
                    var reverse = graph.GetEdge(edge.To, edge.From);
                    if (reverse != null)
                    {
                        DrawResidualEdge(reverse, ResidualCanvas, Brushes.Blue);
                        drawnEdges.Add(reverseEdge);
                    }
                }
            }
        }

        private void DrawNode(Node node, Canvas canvas)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            Canvas.SetLeft(ellipse, node.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, node.Y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);

            TextBlock textBlock = new TextBlock
            {
                Text = node.Id,
                FontSize = 16,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(textBlock, node.X - ellipse.Width / 8 );
            Canvas.SetTop(textBlock, node.Y - ellipse.Height / 2.5f );
            canvas.Children.Add(textBlock);
        }

        private void DrawEdge(Edge edge, Canvas canvas, Brush strokeColor)
        {
            if (edge.Capacity == 0)
                return; // Do not draw reverse edges with 0 capacity

            DrawDirectedEdge(edge.From, edge.To, edge.Flow, edge.Capacity, strokeColor, canvas, 0, true);
        }


        private void DrawResidualEdge(Edge edge, Canvas canvas, Brush color)
        {
            // Offset for separating forward and reverse edges
            double offset = 10;

            // Draw the edge with offset, without displaying capacity
            DrawDirectedEdge(edge.From, edge.To, edge.ResidualCapacity(), edge.Capacity, color, canvas, offset, false);
        }


        private void DrawDirectedEdge(Node from, Node to, int residualCapacity, int capacity, Brush color, Canvas canvas, double offset, bool displayCapacity)
        {
            if (residualCapacity == 0 && !displayCapacity) return;
            // Calculate the direction of the edge and apply offset
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            double offsetX = offset * dy / length;
            double offsetY = offset * -dx / length;

            // Adjust the start and end points to be on the edge of the nodes
            double nodeRadius = 15; // Half of the node diameter (30/2)
            double adjustedStartX = from.X + (dx / length) * nodeRadius + offsetX;
            double adjustedStartY = from.Y + (dy / length) * nodeRadius + offsetY;
            double adjustedEndX = to.X - (dx / length) * nodeRadius + offsetX;
            double adjustedEndY = to.Y - (dy / length) * nodeRadius + offsetY;

            Line line = new Line
            {
                X1 = adjustedStartX,
                Y1 = adjustedStartY,
                X2 = adjustedEndX,
                Y2 = adjustedEndY,
                Stroke = color,
                StrokeThickness = 2
            };

            canvas.Children.Add(line);

            if (displayCapacity)
            {
                // Display capacity and flow
                var capacityText = new TextBlock
                {
                    Text = $"{residualCapacity}/{capacity}",
                    Foreground = Brushes.Black,
                    Background = Brushes.White
                };

                var midX = (line.X1 + line.X2) / 2;
                var midY = (line.Y1 + line.Y2) / 2;

                Canvas.SetLeft(capacityText, midX);
                Canvas.SetTop(capacityText, midY);
                canvas.Children.Add(capacityText);
            } else
            {
                var capacityText = new TextBlock
                {
                    Text = $"{residualCapacity}",
                    Foreground = Brushes.Black,
                    Background = Brushes.LightGray
                };

                var midX = (line.X1 + line.X2) / 2;
                var midY = (line.Y1 + line.Y2) / 2;

                Canvas.SetLeft(capacityText, midX);
                Canvas.SetTop(capacityText, midY);
                canvas.Children.Add(capacityText);
            }

            // Draw arrowhead
            DrawArrowhead(canvas, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2), color);
        }


        private void DrawArrowhead(Canvas canvas, Point from, Point to, Brush color)
        {
            const double arrowLength = 10;
            const double arrowWidth = 5;

            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);

            Point arrowTip = new Point(to.X, to.Y);
            Point base1 = new Point(
                to.X - arrowLength * Math.Cos(angle - Math.PI / 6),
                to.Y - arrowLength * Math.Sin(angle - Math.PI / 6));
            Point base2 = new Point(
                to.X - arrowLength * Math.Cos(angle + Math.PI / 6),
                to.Y - arrowLength * Math.Sin(angle + Math.PI / 6));

            Polygon arrowHead = new Polygon
            {
                Points = new PointCollection { arrowTip, base1, base2 },
                Fill = color
            };

            canvas.Children.Add(arrowHead);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;
            GraphMenu.IsEnabled = false;
            DrawFlowGraph();
            DrawResidualGraph(); // Draw edges in the residual graph
            RunFordFulkerson();
        }

        private void RunFordFulkerson()
        {
            var source = nodesDict["s"];
            var sink = nodesDict["t"];
            totalFlow = 0;

            AnimateFlow(source, sink);
        }

        private async void AnimateFlow(Node source, Node sink)
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                if (totalFlow >= int.MaxValue) // Terminate if no more increments are possible
                {
                    MessageBox.Show("Animation complete");
                    FlowTextBox.Text = $"Max Flow: {totalFlow}";
                    StartButton.IsEnabled = true;
                    GraphMenu.IsEnabled = true;
                    return;
                }

                currentPath = graph.FindAugmentingPath(source, sink);
                if (currentPath == null)
                {
                    FlowCanvas.Children.Remove(flowPoint);
                    MessageBox.Show("No more augmenting paths");
                    FlowTextBox.Text = $"Max Flow: {totalFlow}";
                    StartButton.IsEnabled = true;
                    GraphMenu.IsEnabled = true;
                    return;
                }

                currentPathIndex = 0;
                totalFlow++;
                // Update the flow text box
                FlowTextBox.Text = $"Current Flow: {totalFlow}";

                // Initialize flow point
                if (flowPoint != null)
                {
                    FlowCanvas.Children.Remove(flowPoint);
                }

                flowPoint = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red
                };

                FlowCanvas.Children.Add(flowPoint);

                currentX = currentPath[0].From.X;
                currentY = currentPath[0].From.Y;

                Canvas.SetLeft(flowPoint, currentX - flowPoint.Width / 2);
                Canvas.SetTop(flowPoint, currentY - flowPoint.Height / 2);

                stepSize = 5; // Adjust step size for smooth movement
            }

            var edge = currentPath[currentPathIndex];
            double targetX = edge.To.X;
            double targetY = edge.To.Y;
            double dx = targetX - currentX;
            double dy = targetY - currentY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            while (distance >= stepSize)
            {
                currentX += (dx / distance) * stepSize;
                currentY += (dy / distance) * stepSize;

                // Move the flow point
                Canvas.SetLeft(flowPoint, currentX - flowPoint.Width / 2);
                Canvas.SetTop(flowPoint, currentY - flowPoint.Height / 2);

                // Redraw the residual graph with edges
                DrawResidualGraph();

                // Delay for smooth animation
                await Task.Delay(15); // Adjust the delay time as needed

                dx = targetX - currentX;
                dy = targetY - currentY;
                distance = Math.Sqrt(dx * dx + dy * dy);
            }

            currentX = targetX;
            currentY = targetY;

            // Increment flow by 1 on the edge
            edge.Flow++;
            var reverseEdge = graph.GetEdge(edge.To, edge.From);
            if (reverseEdge == null)
            {
                reverseEdge = new Edge(edge.To, edge.From, 0);
                graph.Edges.Add(reverseEdge);
            }
            reverseEdge.Flow--;

            DrawGraphs(); // Redraw both graphs to show updated flow and residual capacities

            // Update the flow text box
            FlowTextBox.Text = $"Current Flow: {totalFlow}";

            currentPathIndex++;

            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
            }

            // Continue the animation for the next edge or path
            AnimateFlow(source, sink);

        }


        /*private void RunFordFulkerson()
        {
            var source = nodesDict["s"];
            var sink = nodesDict["t"];
            totalFlow = 0;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2) 
            };
            timer.Tick += (s, e) => AnimateFlow(source, sink);
            timer.Start();
        }

        private async void AnimateFlow(Node source, Node sink)
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                if (totalFlow >= int.MaxValue) // Terminate if no more increments are possible
                {
                    timer.Stop();
                    MessageBox.Show("Animation complete");
                    FlowTextBox.Text = $"Max Flow: {totalFlow}";
                    return;
                }

                currentPath = graph.FindAugmentingPath(source, sink);
                if (currentPath == null)
                {
                    timer.Stop();
                    FlowCanvas.Children.Remove(flowPoint);
                    MessageBox.Show("No more augmenting paths");
                    FlowTextBox.Text = $"Max Flow: {totalFlow}";
                    return;
                }

                currentPathIndex = 0;
                totalFlow++;
                // Update the flow text box
                FlowTextBox.Text = $"Current Flow: {totalFlow}";

                // Initialize flow point
                if (flowPoint != null)
                {
                    FlowCanvas.Children.Remove(flowPoint);
                }

                flowPoint = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red
                };

                FlowCanvas.Children.Add(flowPoint);

                currentX = currentPath[0].From.X;
                currentY = currentPath[0].From.Y;

                Canvas.SetLeft(flowPoint, currentX - flowPoint.Width / 2);
                Canvas.SetTop(flowPoint, currentY - flowPoint.Height / 2);

                stepSize = 5; // Adjust step size for smooth movement
            }

            var edge = currentPath[currentPathIndex];
            double targetX = edge.To.X;
            double targetY = edge.To.Y;
            double dx = targetX - currentX;
            double dy = targetY - currentY;
            double distance = Math.Sqrt(dx * dx + dy * dy);


            if (distance <= stepSize)
            {
                currentX = targetX;
                currentY = targetY;

                // Increment flow by 1 on the edge
                edge.Flow++;
                var reverseEdge = graph.GetEdge(edge.To, edge.From);
                if (reverseEdge == null)
                {
                    reverseEdge = new Edge(edge.To, edge.From, 0);
                    graph.Edges.Add(reverseEdge);
                }
                reverseEdge.Flow--;

                DrawGraphs(); // Redraw both graphs to show updated flow and residual capacities


                currentPathIndex++;

                if (currentPathIndex >= currentPath.Count)
                {
                    currentPath = null;
                }
            }
            else
            {
                currentX += (dx / distance) * stepSize;
                currentY += (dy / distance) * stepSize;
            }

            // Move the flow point
            Canvas.SetLeft(flowPoint, currentX - flowPoint.Width / 2);
            Canvas.SetTop(flowPoint, currentY - flowPoint.Height / 2);

            // Redraw the residual graph with edges
            DrawResidualGraph();
        }*/


        private void ShowExample1_Click(object sender, RoutedEventArgs e)
        {
            ClearGraph();
            LoadExample1();
            DrawGraphs();
        }

        private void ShowExample2_Click(object sender, RoutedEventArgs e)
        {
            ClearGraph();
            LoadExample2();
            DrawGraphs();
        }

        private void ShowExample3_Click(object sender, RoutedEventArgs e)
        {
            ClearGraph();
            LoadExample3();
            DrawGraphs();
        }


        private void ClearGraph()
        {
            graph = new Graph();
            nodesDict.Clear();
            FlowCanvas.Children.Clear();
            ResidualCanvas.Children.Clear();
            FlowTextBox.Text = "";
            InitializeDefaultNodes();
            DrawGraphs();
        }

        private void LoadExample1()
        {
            var source = new Node("s", 50, 200);
            var sink = new Node("t", 500, 200);
            var a = new Node("A", 200, 100);
            var b = new Node("B", 200, 300);
            var c = new Node("C", 350, 100);
            var d = new Node("D", 350, 300);

            graph.Nodes.Add(source);
            graph.Nodes.Add(sink);
            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            graph.Nodes.Add(d);

            nodesDict["s"] = source;
            nodesDict["t"] = sink;
            nodesDict["A"] = a;
            nodesDict["B"] = b;
            nodesDict["C"] = c;
            nodesDict["D"] = d;

            graph.AddEdge(source, a, 10);
            graph.AddEdge(source, b, 5);
            graph.AddEdge(a, c, 5);
            graph.AddEdge(a, b, 15);
            graph.AddEdge(b, d, 10);
            graph.AddEdge(c, sink, 10);
            graph.AddEdge(d, sink, 10);
        }

        private void LoadExample2()
        {
            var source = new Node("s", 50, 200);
            var sink = new Node("t", 500, 200);
            var a = new Node("A", 150, 100);
            var b = new Node("B", 150, 300);
            var c = new Node("C", 300, 200);
            var d = new Node("D", 450, 100);
            var e = new Node("E", 450, 300);

            graph.Nodes.Add(source);
            graph.Nodes.Add(sink);
            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            graph.Nodes.Add(d);
            graph.Nodes.Add(e);

            nodesDict["s"] = source;
            nodesDict["t"] = sink;
            nodesDict["A"] = a;
            nodesDict["B"] = b;
            nodesDict["C"] = c;
            nodesDict["D"] = d;
            nodesDict["E"] = e;

            graph.AddEdge(source, a, 16);
            graph.AddEdge(source, b, 13);
            graph.AddEdge(a, c, 12);
            graph.AddEdge(b, c, 4);
            graph.AddEdge(b, e, 14);
            graph.AddEdge(c, d, 20);
            graph.AddEdge(d, sink, 7);
            graph.AddEdge(e, sink, 4);
        }

        private void LoadExample3()
        {
            var source = new Node("s", 50, 200);
            var sink = new Node("t", 600, 200);
            var a = new Node("A", 150, 100);
            var b = new Node("B", 150, 300);
            var c = new Node("C", 250, 200);
            var d = new Node("D", 350, 100);
            var e = new Node("E", 350, 300);
            var f = new Node("F", 450, 200);
            var g = new Node("G", 550, 100);
            var h = new Node("H", 550, 300);

            graph.Nodes.Add(source);
            graph.Nodes.Add(sink);
            graph.Nodes.Add(a);
            graph.Nodes.Add(b);
            graph.Nodes.Add(c);
            graph.Nodes.Add(d);
            graph.Nodes.Add(e);
            graph.Nodes.Add(f);
            graph.Nodes.Add(g);
            graph.Nodes.Add(h);

            nodesDict["s"] = source;
            nodesDict["t"] = sink;
            nodesDict["A"] = a;
            nodesDict["B"] = b;
            nodesDict["C"] = c;
            nodesDict["D"] = d;
            nodesDict["E"] = e;
            nodesDict["F"] = f;
            nodesDict["G"] = g;
            nodesDict["H"] = h;

            graph.AddEdge(source, a, 10);
            graph.AddEdge(source, b, 15);
            graph.AddEdge(a, c, 20);
            graph.AddEdge(b, c, 10);
            graph.AddEdge(c, d, 15);
            graph.AddEdge(c, e, 5);
            graph.AddEdge(d, f, 25);
            graph.AddEdge(e, f, 15);
            graph.AddEdge(f, g, 20);
            graph.AddEdge(f, h, 10);
            graph.AddEdge(g, sink, 15);
            graph.AddEdge(h, sink, 25);
        }



    }
}
