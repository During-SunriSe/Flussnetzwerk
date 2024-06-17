using System.Windows;

namespace FlowNetworkWPF
{
    public partial class AddNodeWindow : Window
    {
        public string NodeId { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }

        public AddNodeWindow(double x, double y)
        {
            InitializeComponent();
            X = x;
            Y = y;
            XTextBox.Text = X.ToString("F0");
            YTextBox.Text = Y.ToString("F0");
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            NodeId = NodeIdTextBox.Text;
            DialogResult = true;
        }
    }
}
