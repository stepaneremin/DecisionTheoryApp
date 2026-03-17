using System.Windows;
using DecisionTheoryApp.Services;

namespace DecisionTheoryApp
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ProjectService? _projectService;

        public ProjectService ProjectService
        {
            get
            {
                if (_projectService == null)
                {
                    _projectService = new ProjectService();
                }
                return _projectService;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальная обработка необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Произошла ошибка: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}