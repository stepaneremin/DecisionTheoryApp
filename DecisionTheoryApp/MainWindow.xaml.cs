using System.Windows;
using DecisionTheoryApp.Views;
using DecisionTheoryApp.Algorithms.MultiCriteria;

namespace DecisionTheoryApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AHPButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var projectService = ((App)Application.Current).ProjectService;
                var ahpView = new AHPView(projectService); // Добавлен параметр
                ahpView.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainCriterionButton_Click(object sender, RoutedEventArgs e)
        {
            var multiCriteriaView = new MultiCriteriaView();
            multiCriteriaView.ShowDialog();
        }
    }
}