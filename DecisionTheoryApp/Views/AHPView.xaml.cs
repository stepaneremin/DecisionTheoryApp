using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using DecisionTheoryApp.Services;
using DecisionTheoryApp.ViewModels;
using System.Data;
using System.Collections.Generic;

namespace DecisionTheoryApp.Views
{
    public partial class AHPView : Window
    {
        private readonly AHPViewModel _viewModel;

        // Отслеживаем ячейки с ошибками: ключ = (таблица, строка, столбец DataTable)
        private readonly HashSet<(DataTable, int, int)> _invalidCells = new();

        // Защита от рекурсивного входа при программной записи в DataTable
        private bool _isUpdatingMatrix = false;

        public AHPView(ProjectService projectService)
        {
            InitializeComponent();
            _viewModel = new AHPViewModel(projectService);
            DataContext = _viewModel;
            Closed += (s, e) => _viewModel.Cleanup();

            // Перестраиваем колонки истории когда добавляется первая запись
            _viewModel.CalculationHistory.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                    RebuildHistoryColumns();
            };
        }

        private void CriteriaMatrixGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "CriterionName")
            {
                e.Column.Header = "Критерии";
                if (e.Column is DataGridTextColumn textColumn)
                    textColumn.IsReadOnly = true;
            }
            else if (e.PropertyName.StartsWith("Col") && int.TryParse(e.PropertyName.Substring(3), out int colIdx))
            {
                // Заголовок = имя критерия по индексу
                if (colIdx < _viewModel.Criteria.Count)
                    e.Column.Header = _viewModel.Criteria[colIdx].Name;

                if (e.Column is DataGridTextColumn textColumn)
                {
                    textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                    {
                        Converter = new Converters.NullToEmptyConverter(),
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    };
                    textColumn.ElementStyle = (Style)FindResource("CenteredCellStyle");
                    textColumn.EditingElementStyle = (Style)FindResource("ValidatedEditingStyle");
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHelpFile("инструкция_МАИ.html");
        }

        private static void OpenHelpFile(string fileName)
        {
            try
            {
                // Ищем рядом с .exe
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(exeDir, fileName);
                if (!File.Exists(path))
                {
                    MessageBox.Show($"Файл справки не найден:\n{path}\n\nПоложите файл '{fileName}' рядом с .exe",
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

        // Когда фокус уходит из грида (например, пользователь кликает на кнопку добавить/удалить),
        // принудительно коммитим текущую ячейку — иначе первый клик поглощается передачей фокуса
        // и команда кнопки не выполняется, а значение не попадает в модель.
        private void MatrixGrid_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            // Коммитим только если новый фокус ушёл за пределы грида
            if (!grid.IsKeyboardFocusWithin)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
                grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
            }
        }

        // ─── Переименование критериев и альтернатив ───────────────────────

        // Двойной клик по ListBox критериев — находим TextBlock под курсором и переключаем в TextBox
        private void CriteriaListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ActivateRenameForClickedItem(e.OriginalSource, "CriterionTextBlock", "CriterionTextBox");
        }

        private void AlternativesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ActivateRenameForClickedItem(e.OriginalSource, "AlternativeTextBlock", "AlternativeTextBox");
        }

        private void ActivateRenameForClickedItem(object originalSource, string textBlockName, string textBoxName)
        {
            // Поднимаемся по визуальному дереву от места клика до Grid элемента списка
            var element = originalSource as DependencyObject;
            while (element != null)
            {
                if (element is Grid grid)
                {
                    var textBlock = grid.FindName(textBlockName) as TextBlock;
                    var textBox = grid.FindName(textBoxName) as TextBox;
                    if (textBlock != null && textBox != null)
                    {
                        textBlock.Visibility = Visibility.Collapsed;
                        textBox.Visibility = Visibility.Visible;
                        textBox.SelectAll();
                        textBox.Focus();
                        return;
                    }
                }
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
        }

        // Enter — применяем, Escape — отменяем
        private void RenameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CommitRename(textBox);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelRename(textBox);
                e.Handled = true;
            }
        }

        private void CriterionTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitRename(sender as TextBox);
        }

        private void AlternativeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitRename(sender as TextBox);
        }

        private void CommitRename(TextBox? textBox)
        {
            if (textBox == null) return;

            string newName = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                CancelRename(textBox);
                return;
            }

            // Определяем тип объекта по DataContext
            var dataContext = textBox.DataContext;
            if (dataContext is DecisionTheoryApp.Models.Criterion criterion)
            {
                if (newName != criterion.Name)
                {
                    var newList = _viewModel.Criteria
                        .Select(c => c.Id == criterion.Id ? newName : c.Name)
                        .ToList();
                    _viewModel.ProjectService.UpdateCriteria(newList);
                }
            }
            else if (dataContext is DecisionTheoryApp.Models.Alternative alternative)
            {
                if (newName != alternative.Name)
                {
                    var newList = _viewModel.Alternatives
                        .Select(a => a.Id == alternative.Id ? newName : a.Name)
                        .ToList();
                    _viewModel.ProjectService.UpdateAlternatives(newList);
                }
            }

            SwitchToViewMode(textBox);
        }

        private void CancelRename(TextBox? textBox)
        {
            if (textBox == null) return;
            // Сбрасываем текст к исходному значению
            var be = textBox.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateTarget();
            SwitchToViewMode(textBox);
        }

        private void SwitchToViewMode(TextBox textBox)
        {
            var parent = textBox.Parent as Grid;
            if (parent == null) return;
            var textBlock = parent.Children.OfType<TextBlock>().FirstOrDefault();
            if (textBlock != null) textBlock.Visibility = Visibility.Visible;
            textBox.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Динамически строим колонки таблицы истории по текущему набору альтернатив.
        /// Первые две колонки (#, Время) объявлены в XAML, добавляем колонки альтернатив.
        /// </summary>
        private void RebuildHistoryColumns()
        {
            // Оставляем первые две колонки (# и Время), убираем остальные
            while (HistoryGrid.Columns.Count > 2)
                HistoryGrid.Columns.RemoveAt(HistoryGrid.Columns.Count - 1);

            var first = _viewModel.CalculationHistory.FirstOrDefault();
            if (first == null) return;

            for (int i = 0; i < first.Priorities.Count; i++)
            {
                int idx = i; // захват для лямбды
                var col = new DataGridTemplateColumn
                {
                    Header = first.Priorities[idx].Name,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };

                // CellTemplate — отображаем приоритет, лидер жирным с ★
                var cellTemplate = new DataTemplate();
                var factory = new FrameworkElementFactory(typeof(TextBlock));
                factory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding($"Priorities[{idx}].DisplayText"));
                factory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                factory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                factory.SetValue(TextBlock.MarginProperty, new Thickness(4, 2, 4, 2));

                // Жирный если лидер
                var style = new Style(typeof(TextBlock));
                var trigger = new DataTrigger
                {
                    Binding = new System.Windows.Data.Binding($"Priorities[{idx}].IsLeader"),
                    Value = true
                };
                trigger.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x53, 0x72, 0x3c))));
                style.Triggers.Add(trigger);
                factory.SetValue(TextBlock.StyleProperty, style);

                cellTemplate.VisualTree = factory;
                col.CellTemplate = cellTemplate;
                HistoryGrid.Columns.Add(col);
            }
        }

        // ─── Принудительно коммитит все гриды ────────────────────────────
        private void CommitAllGrids()
        {
            CommitGrid(CriteriaMatrixGrid);
            foreach (var grid in FindVisualChildren<DataGrid>(AlternativesItemsControl))
                CommitGrid(grid);
        }

        private void CommitGrid(DataGrid grid)
        {
            if (grid == null) return;
            grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
            grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);
        }

        private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) yield break;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        // Вызывается перед любой командой изменения списка критериев/альтернатив
        // Вызывается по Click на кнопках добавления/удаления — ДО выполнения команды
        private void CriteriaAlternativesButton_Click(object sender, RoutedEventArgs e)
        {
            CommitAllGrids();
        }


        // Запрет редактирования диагональных ячеек (там всегда 1)
        private void MatrixGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;

            int rowIndex = e.Row.GetIndex();
            int colIndex = grid.Columns.IndexOf(e.Column);

            // colIndex 0 — столбец с именем (уже ReadOnly), colIndex-1 = индекс в матрице
            if (colIndex > 0 && (colIndex - 1) == rowIndex)
                e.Cancel = true;
        }

        // При вводе a[i,j]=c автоматически ставит a[j,i]=1/c
        // Допустимые значения по шкале Саати: целые от 1 до 9
        private void MatrixGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var grid = sender as DataGrid;
            if (grid == null) return;

            var table = (grid.ItemsSource as DataView)?.Table ?? grid.ItemsSource as DataTable;
            if (table == null) return;

            int rowIndex = e.Row.GetIndex();
            int colIndex = grid.Columns.IndexOf(e.Column);

            if (colIndex <= 0) return;

            int i = rowIndex;
            int j = colIndex - 1;

            if (i == j) return;

            var textBox = e.EditingElement as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            string input = textBox.Text.Trim().Replace(',', '.');

            // Проверка: значение должно быть целым числом от 1 до 9
            bool isValid = double.TryParse(input, System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out double value)
                           && value >= 1 && value <= 9
                           && value == Math.Floor(value);

            var cellKey = (table, i, j);
            var reciprocalKey = (table, j, i);

            if (!isValid)
            {
                // Подсвечиваем ячейку красным
                textBox.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                textBox.Foreground = Brushes.DarkRed;

                // Регистрируем ошибку
                _invalidCells.Add(cellKey);
                _viewModel.HasValidationErrors = true;

                MessageBox.Show(
                    "Значение должно быть целым числом от 1 до 9 (шкала Саати).\nВведите число от 1 до 9.",
                    "Некорректное значение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Значение корректное — снимаем ошибку для этой ячейки
            textBox.Background = Brushes.White;
            textBox.Foreground = Brushes.Black;
            _invalidCells.Remove(cellKey);
            _invalidCells.Remove(reciprocalKey); // симметричная ячейка тоже станет корректной
            _viewModel.HasValidationErrors = _invalidCells.Count > 0;

            double reciprocal = Math.Round(1.0 / value, 2);

            // Флаг предотвращает повторный вход в CellEditEnding при программной
            // записи в DataTable ниже
            if (_isUpdatingMatrix) return;
            _isUpdatingMatrix = true;
            try
            {
                // Пишем в модель — синхронно, чтобы значения были актуальны
                // даже если пользователь немедленно нажмёт добавить/удалить
                var projectService = _viewModel.ProjectService;
                if (grid.Name == "CriteriaMatrixGrid")
                {
                    projectService.UpdateCriteriaMatrixValue(i, j, value);
                }
                else
                {
                    var criterionName = grid.Tag as string;
                    var altMatrices = _viewModel.AlternativesMatricesTables;
                    for (int k = 0; k < altMatrices.Count; k++)
                    {
                        if (altMatrices[k].CriterionName == criterionName)
                        {
                            projectService.UpdateAlternativesMatrixValue(k, i, j, value);
                            break;
                        }
                    }
                }

                // Пишем симметричную ячейку в DataTable синхронно
                table.Rows[j][i + 1] = reciprocal;
            }
            finally
            {
                _isUpdatingMatrix = false;
            }
        }

        private void AlternativesMatrixGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var grid = sender as DataGrid;

            if (e.PropertyName == "AlternativeName")
            {
                var criterionName = grid?.Tag as string;
                e.Column.Header = !string.IsNullOrEmpty(criterionName) ? criterionName : "Альтернативы";
                if (e.Column is DataGridTextColumn textColumn)
                    textColumn.IsReadOnly = true;
            }
            else if (e.PropertyName.StartsWith("Col") && int.TryParse(e.PropertyName.Substring(3), out int colIdx))
            {
                // Заголовок = имя альтернативы по индексу
                if (colIdx < _viewModel.Alternatives.Count)
                    e.Column.Header = _viewModel.Alternatives[colIdx].Name;

                if (e.Column is DataGridTextColumn textColumn)
                {
                    textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                    {
                        Converter = new Converters.NullToEmptyConverter(),
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                    };
                    textColumn.ElementStyle = (Style)FindResource("CenteredCellStyle");
                    textColumn.EditingElementStyle = (Style)FindResource("ValidatedEditingStyle");
                }
            }
        }
    }
}