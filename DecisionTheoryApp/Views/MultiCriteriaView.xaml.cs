using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DecisionTheoryApp.Models;
using DecisionTheoryApp.ViewModels;

namespace DecisionTheoryApp.Views
{
    public partial class MultiCriteriaView : Window
    {
        private readonly MultiCriteriaViewModel _viewModel;

        public MultiCriteriaView()
        {
            InitializeComponent();
            _viewModel = new MultiCriteriaViewModel();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MultiCriteriaViewModel.ResultColumns))
                    RebuildResultColumns();
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = System.IO.Path.Combine(exeDir, "инструкция_метод_главного_критерия.html");
                if (!System.IO.File.Exists(path))
                {
                    MessageBox.Show($"Файл справки не найден:\n{path}\n\nПоложите файл 'инструкция_метод_главного_критерия.html' рядом с .exe",
                        "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть справку: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FormulaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.DataContext is CriterionFunction criterion)
                _viewModel.ValidateFormulaCommand.Execute(criterion);
        }

        /// <summary>
        /// Разрешаем вводить только цифры, точку и запятую в числовые поля
        /// </summary>
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            string incoming = e.Text;

            if (incoming == "-")
            {
                e.Handled = tb != null && tb.CaretIndex != 0;
                return;
            }

            e.Handled = !Regex.IsMatch(incoming, @"[\d.,]");
        }

        /// <summary>
        /// Блокируем пробел, явно разрешаем точку и запятую через клавиатуру
        /// </summary>
        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            // Явно вставляем точку/запятую — обходим перехват WPF
            if (e.Key == Key.OemComma || e.Key == Key.OemPeriod ||
                e.Key == Key.Decimal)
            {
                var tb = sender as TextBox;
                if (tb != null)
                {
                    // Не вставляем если разделитель уже есть
                    if (tb.Text.Contains('.') || tb.Text.Contains(','))
                    {
                        e.Handled = true;
                        return;
                    }
                    int caret = tb.CaretIndex;
                    tb.Text = tb.Text.Substring(0, caret) + "," + tb.Text.Substring(caret);
                    tb.CaretIndex = caret + 1;
                }
                e.Handled = true;
            }
        }

        private void RebuildResultColumns()
        {
            ResultsGrid.Columns.Clear();

            ResultsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Статус",
                Binding = new Binding("IsFeasible") { Converter = new FeasibilityConverter() },
                Width = 80
            });

            foreach (var col in _viewModel.ResultColumns)
            {
                ResultsGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = col.Header,
                    Binding = new Binding(col.Path) { StringFormat = "G4" },
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }
        }
    }

    public class FeasibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is bool b && b ? "✓" : "✗";
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new System.NotImplementedException();
    }
}
