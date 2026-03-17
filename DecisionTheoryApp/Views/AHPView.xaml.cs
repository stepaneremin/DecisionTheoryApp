using DecisionTheoryApp.Helpers;
using DecisionTheoryApp.Models;
using DecisionTheoryApp.Services;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace DecisionTheoryApp.Views
{
    public partial class AHPView : Window
    {
        private DataTable matrixTable = new DataTable();
        private ProjectData project = new ProjectData();

        public AHPView()
        {
            InitializeComponent();
        }

        private void AddCriterion(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите критерий");

            if (!string.IsNullOrWhiteSpace(name))
            {
                CriteriaList.Items.Add(name);
                project.Criteria.Add(new Criterion { Name = name });
            }
        }

        private void AddAlternative(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите альтернативу");

            if (!string.IsNullOrWhiteSpace(name))
            {
                AlternativeList.Items.Add(name);
                project.Alternatives.Add(new Alternative { Name = name });
            }
        }

        private void CreateMatrix(object sender, RoutedEventArgs e)
        {
            int n = project.Criteria.Count;

            if (n == 0)
            {
                MessageBox.Show("Добавьте критерии");
                return;
            }

            matrixTable = new DataTable();

            matrixTable.Columns.Add("Критерий");

            for (int i = 0; i < n; i++)
            {
                matrixTable.Columns.Add(project.Criteria[i].Name);
            }

            for (int i = 0; i < n; i++)
            {
                var row = matrixTable.NewRow();

                row[0] = project.Criteria[i].Name;

                for (int j = 0; j < n; j++)
                {
                    row[j + 1] = (i == j) ? 1 : "";
                }

                matrixTable.Rows.Add(row);
            }

            MatrixGrid.ItemsSource = matrixTable.DefaultView;

            MatrixGrid.CanUserAddRows = false;
            //MatrixGrid.CanUserAddColumns = false; хуйня какая-то такого правила нет вообще
            CreateAlternativeMatrix();
        }

        //private void CreateAlternativeMatrix()
        //{
        //    int n = project.Alternatives.Count;

        //    if (n == 0)
        //        return;

        //    DataTable table = new DataTable();

        //    table.Columns.Add("Альтернатива");

        //    for (int i = 0; i < n; i++)
        //    {
        //        table.Columns.Add(project.Alternatives[i].Name);
        //    }

        //    for (int i = 0; i < n; i++)
        //    {
        //        var row = table.NewRow();

        //        row[0] = project.Alternatives[i].Name;

        //        for (int j = 0; j < n; j++)
        //        {
        //            row[j + 1] = (i == j) ? 1 : 1;
        //        }

        //        table.Rows.Add(row);
        //    }

        //    AlternativeMatrixGrid.ItemsSource = table.DefaultView;
        //}

        private void CreateAlternativeMatrix()
        {
            int n = project.Alternatives.Count;

            if (n == 0)
                return;

            DataTable table = new DataTable();

            // Добавляем колонки
            table.Columns.Add("Альтернатива");

            for (int i = 0; i < n; i++)
            {
                table.Columns.Add(project.Alternatives[i].Name);
            }

            // Заполняем строки
            for (int i = 0; i < n; i++)
            {
                var row = table.NewRow();

                row[0] = project.Alternatives[i].Name;

                for (int j = 0; j < n; j++)
                {
                    row[j + 1] = (i == j) ? 1 : ""; // Пустые ячейки для заполнения
                }

                table.Rows.Add(row);
            }

            // Отключаем автоматическое создание пустых строк
            AlternativeMatrixGrid.CanUserAddRows = false;

            // Отключаем удаление строк (опционально)
            AlternativeMatrixGrid.CanUserDeleteRows = false;

            // Устанавливаем источник данных
            AlternativeMatrixGrid.ItemsSource = table.DefaultView;
        }

        //private void Calculate(object sender, RoutedEventArgs e)
        //{
        //    int n = project.Criteria.Count;

        //    if (n == 0)
        //    {
        //        MessageBox.Show("Нет критериев");
        //        return;
        //    }

        //    double[,] matrix = new double[n, n];

        //    for (int i = 0; i < n; i++)
        //    {
        //        for (int j = 0; j < n; j++)
        //        {
        //            matrix[i, j] = Convert.ToDouble(matrixTable.Rows[i][j + 1]);
        //        }
        //    }

        //    double[] weights = CalculateWeights(matrix);

        //    string result = "";

        //    for (int i = 0; i < weights.Length; i++)
        //    {
        //        result += project.Criteria[i].Name + " : " + weights[i].ToString("F3") + "\n";
        //    }

        //    MessageBox.Show(result, "Веса критериев");
        //}
        private void Calculate(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = project.Criteria.Count;

                if (n == 0)
                {
                    MessageBox.Show("Нет критериев", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double[,] matrix = new double[n, n];
                List<string> errors = new List<string>();

                // Заполняем матрицу с парсингом значений
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        string cellValue = matrixTable.Rows[i][j + 1].ToString();

                        try
                        {
                            matrix[i, j] = MatrixValueParser.ParseToDouble(cellValue);

                            // Проверяем, что значение положительное
                            if (matrix[i, j] <= 0)
                            {
                                errors.Add($"Ячейка [{i + 1}, {j + 1}]: значение должно быть положительным");
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Ячейка [{i + 1}, {j + 1}]: '{cellValue}' - {ex.Message}");
                        }
                    }
                }

                // Если есть ошибки, показываем их и прерываем расчет
                if (errors.Count > 0)
                {
                    string errorMessage = "Ошибки при чтении матрицы:\n" + string.Join("\n", errors);
                    MessageBox.Show(errorMessage, "Ошибка ввода",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем обратную симметричность (опционально)
                CheckMatrixSymmetry(matrix, n);

                double[] weights = CalculateWeights(matrix);

                string result = "Веса критериев:\n\n";
                for (int i = 0; i < weights.Length; i++)
                {
                    result += $"{project.Criteria[i].Name}: {weights[i]:F3} ({weights[i] * 100:F1}%)\n";
                }

                MessageBox.Show(result, "Результат расчета",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет обратную симметричность матрицы
        /// </summary>
        private void CheckMatrixSymmetry(double[,] matrix, int n)
        {
            const double tolerance = 0.001; // Допустимая погрешность

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double expected = 1.0 / matrix[i, j];
                    double actual = matrix[j, i];

                    if (Math.Abs(expected - actual) > tolerance)
                    {
                        MessageBox.Show(
                            $"Предупреждение: Матрица не является обратно-симметричной.\n" +
                            $"matrix[{i + 1},{j + 1}] = {matrix[i, j]:F3}, " +
                            $"matrix[{j + 1},{i + 1}] = {actual:F3}, ожидалось {expected:F3}",
                            "Предупреждение",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        break;
                    }
                }
            }
        }



        private double[] CalculateWeights(double[,] matrix)
        {
            int n = matrix.GetLength(0);

            double[] weights = new double[n];

            for (int i = 0; i < n; i++)
            {
                double product = 1;

                for (int j = 0; j < n; j++)
                {
                    product *= matrix[i, j];
                }

                weights[i] = Math.Pow(product, 1.0 / n);
            }

            double sum = 0;

            for (int i = 0; i < n; i++)
                sum += weights[i];

            for (int i = 0; i < n; i++)
                weights[i] /= sum;

            return weights;
        }

        private void SaveProject(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Сохранение проекта пока отключено");
        }

        private void LoadProject(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Загрузка проекта пока отключена");
        }

        private void CreateReport(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отладочное сообщение
                System.Diagnostics.Debug.WriteLine("CreateReport вызван!");
                MessageBox.Show("Метод CreateReport вызван!", "Отладка",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var viewModel = DataContext as ViewModels.AHPViewModel;
                if (viewModel == null)
                {
                    MessageBox.Show("ViewModel не найден!", "Ошибка");
                    return;
                }

                MessageBox.Show($"ViewModel найден. IsCalculated = {viewModel.IsCalculated}", "Отладка");

                if (!viewModel.IsCalculated)
                {
                    MessageBox.Show("Сначала выполните расчет!", "Предупреждение");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Сохранение отчета",
                    Filter = "Word документы (*.docx)|*.docx",
                    DefaultExt = ".docx",
                    FileName = $"{viewModel.ProjectName}_отчет_{DateTime.Now:yyyyMMdd_HHmm}.docx"
                };

                MessageBox.Show($"Диалог создан. FileName: {dialog.FileName}", "Отладка");

                if (dialog.ShowDialog() == true)
                {
                    MessageBox.Show($"Выбран файл: {dialog.FileName}", "Отладка");

                    viewModel.StatusMessage = "Формирование отчета...";

                    var projectService = ((App)Application.Current).ProjectService;
                    var reportService = new Services.ReportService(projectService);

                    MessageBox.Show("Начинаю генерацию отчета...", "Отладка");

                    reportService.GenerateAHPReport(dialog.FileName);

                    MessageBox.Show("Отчет успешно сгенерирован!", "Отладка");

                    viewModel.StatusMessage = $"Отчет сохранен: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
                else
                {
                    MessageBox.Show("Диалог отменен пользователем", "Отладка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\n\nStack: {ex.StackTrace}", "Ошибка");

                if (DataContext is ViewModels.AHPViewModel viewModel)
                    viewModel.StatusMessage = "Ошибка при создании отчета";
            }
        }


    }
}