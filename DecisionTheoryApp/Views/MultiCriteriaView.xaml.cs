using System.Windows;

namespace DecisionTheoryApp.Views
{
    public partial class MultiCriteriaView : Window
    {
        public MultiCriteriaView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}