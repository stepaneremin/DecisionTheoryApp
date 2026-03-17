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
            var projectService = ((App)Application.Current).ProjectService;
            var ahpView = new AHPView();
            ahpView.ShowDialog();
        }

        private void MainCriterionButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем сообщение-заглушку
            MessageBox.Show(
                MainCriterionMethod.GetPlaceholderMessage(),
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Или открываем окно с заглушкой
            // var multiCriteriaView = new MultiCriteriaView();
            // multiCriteriaView.ShowDialog();
        }
    }
}