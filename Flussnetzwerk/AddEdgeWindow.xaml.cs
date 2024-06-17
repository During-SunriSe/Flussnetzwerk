using System.Collections.Generic;
using System.Windows;

namespace FlowNetworkWPF
{
    public partial class AddEdgeWindow : Window
    {
        public string FromNodeId { get; private set; }
        public string ToNodeId { get; private set; }
        public int Capacity { get; private set; }

        public AddEdgeWindow(List<string> nodeIds)
        {
            InitializeComponent();
            FromNodeComboBox.ItemsSource = nodeIds;
            ToNodeComboBox.ItemsSource = nodeIds;
        }

        private void AddEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            FromNodeId = FromNodeComboBox.SelectedItem as string;
            ToNodeId = ToNodeComboBox.SelectedItem as string;
            Capacity = int.Parse(CapacityTextBox.Text);
            DialogResult = true;
        }
    }
}
