using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using DecisionTheoryApp.Algorithms.MultiCriteria;
using DecisionTheoryApp.Models;
using Microsoft.Win32;

namespace DecisionTheoryApp.ViewModels
{
    public class MultiCriteriaViewModel : INotifyPropertyChanged
    {
        private string _statusMessage = "Готов к работе";
        private ObservableCollection<OptimizationResult> _results = new();
        private ObservableCollection<ResultColumn> _resultColumns = new();
        private bool _isCalculated = false;
        private bool _hasValidationErrors = false;

        public ObservableCollection<CriterionFunction> Criteria { get; } = new();
        public ObservableCollection<OptimizationVariable> Variables { get; } = new();
        public ObservableCollection<OptimizationResult> Results
        {
            get => _results;
            private set { _results = value; OnPropertyChanged(); }
        }
        public ObservableCollection<ResultColumn> ResultColumns
        {
            get => _resultColumns;
            private set { _resultColumns = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsCalculated
        {
            get => _isCalculated;
            set { _isCalculated = value; OnPropertyChanged(); }
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set { _hasValidationErrors = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Команды
        public ICommand AddCriterionCommand { get; }
        public ICommand RemoveCriterionCommand { get; }
        public ICommand AddVariableCommand { get; }
        public ICommand RemoveVariableCommand { get; }
        public ICommand SetMainCriterionCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        // История расчётов
        public ObservableCollection<OptimumRecord> OptimumHistory { get; } = new();
        public ICommand ValidateFormulaCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand LoadProjectCommand { get; }
        public ICommand SaveReportCommand { get; }

        public MultiCriteriaViewModel()
        {
            AddCriterionCommand = new RelayCommand(AddCriterion, _ => Criteria.Count < 3);
            RemoveCriterionCommand = new RelayCommand(RemoveCriterion, p => Criteria.Count > 1 && p is CriterionFunction);
            AddVariableCommand = new RelayCommand(AddVariable, _ => Variables.Count < 5);
            RemoveVariableCommand = new RelayCommand(RemoveVariable, p => Variables.Count > 1 && p is OptimizationVariable);
            SetMainCriterionCommand = new RelayCommand(SetMainCriterion);
            CalculateCommand = new RelayCommand(Calculate, CanCalculate);
            ClearCommand = new RelayCommand(Clear);
            ClearHistoryCommand = new RelayCommand(
                _ => { OptimumHistory.Clear(); StatusMessage = "История очищена"; },
                _ => OptimumHistory.Count > 0);
            ValidateFormulaCommand = new RelayCommand(ValidateFormula);
            SaveProjectCommand = new RelayCommand(SaveProject);
            LoadProjectCommand = new RelayCommand(LoadProject);
            SaveReportCommand = new RelayCommand(SaveReport, _ => IsCalculated);

            // Инициализация по умолчанию
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Одна переменная x
            Variables.Add(new OptimizationVariable { Name = "x", Type = VariableType.Continuous, Min = 0, Max = 10, Step = 1 });

            // Два критерия
            var f1 = new CriterionFunction { Name = "f1", Formula = "x^2", Direction = OptimizationDirection.Maximize, IsMain = true };
            var f2 = new CriterionFunction { Name = "f2", Formula = "10 - x", Direction = OptimizationDirection.Maximize, IsMain = false, ConstraintType = ConstraintType.GreaterOrEqual, Threshold = 0 };
            Criteria.Add(f1);
            Criteria.Add(f2);
        }

        private void AddCriterion(object? _)
        {
            int n = Criteria.Count + 1;
            Criteria.Add(new CriterionFunction
            {
                Name = $"f{n}",
                Formula = "",
                Direction = OptimizationDirection.Maximize,
                IsMain = false,
                ConstraintType = ConstraintType.GreaterOrEqual,
                Threshold = 0
            });
            StatusMessage = $"Добавлен критерий f{n}";
        }

        private void RemoveCriterion(object? p)
        {
            if (p is not CriterionFunction c) return;
            bool wasMain = c.IsMain;
            Criteria.Remove(c);
            // Если удалили главный — назначаем первый
            if (wasMain && Criteria.Any())
                Criteria[0].IsMain = true;
            StatusMessage = $"Критерий {c.Name} удалён";
        }

        private void AddVariable(object? _)
        {
            int n = Variables.Count + 1;
            Variables.Add(new OptimizationVariable { Name = $"x{n}", Type = VariableType.Continuous, Min = 0, Max = 10, Step = 1 });
            StatusMessage = $"Добавлена переменная x{n}";
        }

        private void RemoveVariable(object? p)
        {
            if (p is OptimizationVariable v)
            {
                Variables.Remove(v);
                StatusMessage = $"Переменная {v.Name} удалена";
            }
        }

        private void SetMainCriterion(object? p)
        {
            if (p is not CriterionFunction target) return;
            foreach (var c in Criteria)
                c.IsMain = c == target;

            // Принудительно обновляем коллекцию чтобы DataTemplate перерисовал IsConstraint
            var snapshot = Criteria.ToList();
            Criteria.Clear();
            foreach (var c in snapshot)
                Criteria.Add(c);

            StatusMessage = $"Главный критерий: {target.Name}";
        }

        private void ValidateFormula(object? p)
        {
            if (p is not CriterionFunction criterion) return;
            var varNames = Variables.Select(v => v.Name).ToList();
            var error = MainCriterionCalculator.ValidateFormula(criterion.Formula, varNames);
            criterion.FormulaError = error ?? "";
            HasValidationErrors = Criteria.Any(c => c.HasError);
        }

        private bool CanCalculate(object? _)
        {
            return Criteria.Count >= 1
                && Variables.Count >= 1
                && Criteria.Any(c => c.IsMain)
                && !HasValidationErrors;
        }

        private void Calculate(object? _)
        {
            // Валидируем все формулы перед расчётом
            var varNames = Variables.Select(v => v.Name).ToList();
            bool hasErrors = false;
            foreach (var c in Criteria)
            {
                var error = MainCriterionCalculator.ValidateFormula(c.Formula, varNames);
                c.FormulaError = error ?? "";
                if (error != null) hasErrors = true;
            }

            if (hasErrors)
            {
                HasValidationErrors = true;
                MessageBox.Show(
                    "Исправьте ошибки в формулах перед расчётом.",
                    "Ошибки в формулах",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Выполняется расчёт...";

                // Проверяем размер сетки
                long gridSize = 1;
                foreach (var v in Variables)
                {
                    var vals = v.GetValues();
                    if (vals.Count == 0) throw new InvalidOperationException($"Переменная {v.Name}: нет значений. Проверьте границы и шаг.");
                    gridSize *= vals.Count;
                    if (gridSize > 100_000)
                        throw new InvalidOperationException($"Слишком много точек ({gridSize:N0}). Увеличьте шаг или сузьте границы.");
                }

                var results = MainCriterionCalculator.Calculate(
                    Criteria.ToList(),
                    Variables.ToList());

                Results = new ObservableCollection<OptimizationResult>(results);
                BuildResultColumns();

                IsCalculated = true;
                int feasible = results.Count(r => r.IsFeasible);
                StatusMessage = $"Расчёт выполнен. Допустимых точек: {feasible} из {results.Count}.";

                var optimal = results.FirstOrDefault(r => r.IsOptimal);
                if (optimal != null)
                {
                    var main = Criteria.First(c => c.IsMain);

                    // Форматируем переменные — G4 и защита от -0
                    var vars = string.Join(", ", optimal.Variables
                        .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));

                    // Значения всех критериев
                    var criteriaValues = string.Join("\n", Criteria
                        .Select(c =>
                        {
                            string val = optimal.CriteriaValues.TryGetValue(c.Name, out var cv)
                                ? FormatValue(cv) : "—";
                            string mark = c.IsMain ? " ★" : "";
                            return $"  {c.Name}{mark} = {val}";
                        }));

                    // Добавляем запись в историю
                    var dirStr = main.Direction == OptimizationDirection.Maximize ? "max" : "min";
                    OptimumHistory.Add(new OptimumRecord
                    {
                        Number = OptimumHistory.Count + 1,
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        MainCriterionDisplay = $"{main.Name} ({dirStr}) = {FormatValue(optimal.MainCriterionValue)}",
                        OptimalPoint = vars,
                        FeasibleCount = $"{feasible}/{results.Count}"
                    });
                    CommandManager.InvalidateRequerySuggested();

                    MessageBox.Show(
                        $"Расчёт выполнен успешно!\n\n" +
                        $"Оптимальная точка: {vars}\n\n" +
                        $"Значения критериев:\n{criteriaValues}\n\n" +
                        $"Допустимых точек: {feasible} из {results.Count}",
                        "Результат",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Не найдено ни одной допустимой точки.\nПопробуйте изменить ограничения или границы переменных.",
                        "Нет решения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show(ex.Message, "Ошибка расчёта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProject(object? _)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Сохранить проект",
                Filter = "JSON файлы (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "проект_главный_критерий.json"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var data = new ProjectSnapshot
                {
                    Variables = Variables.ToList(),
                    Criteria = Criteria.ToList()
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(dlg.FileName, json);
                StatusMessage = $"Проект сохранён: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProject(object? _)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Открыть проект",
                Filter = "JSON файлы (*.json)|*.json",
                DefaultExt = ".json"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var options = new JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                var data = JsonSerializer.Deserialize<ProjectSnapshot>(json, options);
                if (data == null) throw new InvalidOperationException("Файл повреждён или имеет неверный формат.");

                Variables.Clear();
                Criteria.Clear();
                Results.Clear();
                ResultColumns.Clear();
                IsCalculated = false;
                HasValidationErrors = false;

                foreach (var v in data.Variables ?? [])
                    Variables.Add(v);
                foreach (var c in data.Criteria ?? [])
                    Criteria.Add(c);

                // Если ни один не помечен главным — назначаем первый
                if (Criteria.Any() && !Criteria.Any(c => c.IsMain))
                    Criteria[0].IsMain = true;

                // Откладываем уведомления — DataTemplate должен быть создан до того
                // как мы уведомим о значениях enum (иначе ComboBox не подхватит)
                var loadedVars = Variables.ToList();
                var loadedCriteria = Criteria.ToList();
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background, () =>
                    {
                        foreach (var v in loadedVars) v.NotifyAllProperties();
                        foreach (var c in loadedCriteria) c.NotifyAllProperties();
                    });

                StatusMessage = $"Проект загружен: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveReport(object? _)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Сохранить отчёт",
                Filter = "Word документы (*.docx)|*.docx",
                DefaultExt = ".docx",
                FileName = $"отчёт_главный_критерий_{DateTime.Now:yyyyMMdd_HHmm}.docx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                MultiCriteriaReportService.Generate(dlg.FileName, Criteria.ToList(),
                    Results.ToList());
                StatusMessage = $"Отчёт сохранён: {Path.GetFileName(dlg.FileName)}";
                MessageBox.Show("Отчёт успешно сохранён!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Строим описание колонок для DataGrid результатов</summary>
        /// <summary>Форматирует double — G4, защита от -0</summary>
        private static string FormatValue(double v)
        {
            if (v == 0) return "0";
            return v.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void BuildResultColumns()
        {
            var cols = new ObservableCollection<ResultColumn>();
            foreach (var v in Variables)
                cols.Add(new ResultColumn { Header = v.Name, Path = $"Variables[{v.Name}]" });
            foreach (var c in Criteria)
            {
                string header = c.IsMain ? $"{c.Name} ★" : c.Name;
                cols.Add(new ResultColumn { Header = header, Path = $"CriteriaValues[{c.Name}]" });
            }
            ResultColumns = cols;
        }

        private void Clear(object? _)
        {
            var r = MessageBox.Show("Очистить все данные?", "Очистить",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            Criteria.Clear();
            Variables.Clear();
            Results.Clear();
            ResultColumns.Clear();
            IsCalculated = false;
            HasValidationErrors = false;
            InitializeDefaults();
            StatusMessage = "Данные очищены";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Описание колонки в таблице результатов</summary>
    public class ResultColumn
    {
        public string Header { get; set; } = "";
        public string Path { get; set; } = "";
    }

    /// <summary>Одна запись в истории расчётов — оптимальная точка</summary>
    public class OptimumRecord
    {
        public int Number { get; set; }
        public string Time { get; set; } = "";
        public string MainCriterionDisplay { get; set; } = "";
        public string OptimalPoint { get; set; } = "";
        public string FeasibleCount { get; set; } = "";
    }

    /// <summary>Снимок проекта для сохранения/загрузки</summary>
    public class ProjectSnapshot
    {
        public List<OptimizationVariable> Variables { get; set; } = new();
        public List<CriterionFunction> Criteria { get; set; } = new();
    }

    /// <summary>Генератор отчёта для метода главного критерия</summary>
    public static class MultiCriteriaReportService
    {
        public static void Generate(string filePath,
            List<CriterionFunction> criteria,
            List<OptimizationResult> results)
        {
            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument
                .Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);

            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

            void AddH(string text, int level)
            {
                var p = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                p.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run())
                 .AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
                var props = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
                props.ParagraphStyleId = new DocumentFormat.OpenXml.Wordprocessing.ParagraphStyleId { Val = $"Heading{level}" };
                p.ParagraphProperties = props;
            }

            void AddP(string text)
            {
                var p = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                p.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run())
                 .AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            }

            void AddBoldP(string text)
            {
                var p = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var run = p.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                run.RunProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.Bold());
                run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            }

            DocumentFormat.OpenXml.Wordprocessing.TableCell Cell(string text, bool bold = false, bool shaded = false)
            {
                var cell = new DocumentFormat.OpenXml.Wordprocessing.TableCell();
                var run = new DocumentFormat.OpenXml.Wordprocessing.Run();
                if (bold) run.RunProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.Bold());
                run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
                cell.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(run));
                if (shaded)
                {
                    cell.TableCellProperties = new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties
                    {
                        Shading = new DocumentFormat.OpenXml.Wordprocessing.Shading
                        {
                            Fill = "D3D3D3",
                            Val = DocumentFormat.OpenXml.Wordprocessing.ShadingPatternValues.Clear
                        }
                    };
                }
                return cell;
            }

            string FmtVal(double v) => v == 0 ? "0" : v.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);

            string ConstraintDesc(CriterionFunction c) => c.ConstraintType switch
            {
                ConstraintType.GreaterOrEqual => $">= {FmtVal(c.Threshold)}",
                ConstraintType.LessOrEqual    => $"<= {FmtVal(c.Threshold)}",
                ConstraintType.Range          => $"от {FmtVal(c.Threshold)} до {FmtVal(c.ThresholdMax)}",
                _                             => ""
            };

            var main = criteria.FirstOrDefault(c => c.IsMain);
            var variables = results.FirstOrDefault()?.Variables.Keys.ToList() ?? new List<string>();

            // Заголовок
            AddH("Отчёт: Метод главного критерия", 1);
            AddP($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");

            // Постановка задачи
            AddH("Постановка задачи", 2);

            AddBoldP("Переменные:");
            foreach (var v in variables)
            {
                var allVals = results.Select(r => r.Variables.TryGetValue(v, out var vv) ? vv : double.NaN)
                                     .Where(x => !double.IsNaN(x)).Distinct().OrderBy(x => x).ToList();
                string range = allVals.Count > 0
                    ? $"[{FmtVal(allVals.First())}; {FmtVal(allVals.Last())}]"
                    : "—";
                AddP($"  {v}: область значений {range}");
            }

            AddBoldP("Критерии:");
            foreach (var c in criteria)
            {
                if (c.IsMain)
                    AddP($"  {c.Name} = {c.Formula}  [ГЛАВНЫЙ, {(c.Direction == OptimizationDirection.Maximize ? "максимизировать" : "минимизировать")}]");
                else
                    AddP($"  {c.Name} = {c.Formula}  [ограничение: {ConstraintDesc(c)}]");
            }

            // Результат оптимизации
            AddH("Результат оптимизации", 2);
            var optimal = results.FirstOrDefault(r => r.IsOptimal);
            int feasibleCount = results.Count(r => r.IsFeasible);

            AddP($"Всего проверено точек: {results.Count}");
            AddP($"Допустимых точек: {feasibleCount}");
            AddP($"Недопустимых точек: {results.Count - feasibleCount}");

            if (optimal != null)
            {
                AddP("");
                var pt = string.Join(", ", optimal.Variables.Select(kv => $"{kv.Key} = {FmtVal(kv.Value)}"));
                AddBoldP($"Оптимальная точка: {pt}");
                AddP("");
                AddBoldP("Значения критериев в оптимальной точке:");
                foreach (var c in criteria)
                {
                    double val = optimal.CriteriaValues.TryGetValue(c.Name, out var cv) ? cv : double.NaN;
                    string valStr = double.IsNaN(val) ? "—" : FmtVal(val);
                    if (c.IsMain)
                        AddP($"  {c.Name} = {valStr}  (главный критерий)");
                    else
                    {
                        string status = CheckConstraint(c, val) ? "выполнено" : "нарушено";
                        AddP($"  {c.Name} = {valStr}  [{ConstraintDesc(c)}] — {status}");
                    }
                }
            }
            else
            {
                AddP("Допустимых точек не найдено. Попробуйте изменить ограничения или границы переменных.");
            }

            // Таблица допустимых точек
            AddH("Таблица допустимых точек", 2);
            var feasibleRows = results.Where(r => r.IsFeasible).Take(500).ToList();
            if (feasibleRows.Any())
            {
                var table = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Table());

                var hRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                foreach (var v in variables) hRow.AppendChild(Cell(v, bold: true, shaded: true));
                foreach (var c in criteria) hRow.AppendChild(Cell(c.IsMain ? $"{c.Name} *" : c.Name, bold: true, shaded: true));
                table.AppendChild(hRow);

                foreach (var r in feasibleRows)
                {
                    var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                    foreach (var v in variables)
                        row.AppendChild(Cell(r.Variables.TryGetValue(v, out var vv) ? FmtVal(vv) : ""));
                    foreach (var c in criteria)
                        row.AppendChild(Cell(
                            r.CriteriaValues.TryGetValue(c.Name, out var cv) ? FmtVal(cv) : "",
                            bold: r.IsOptimal));
                    table.AppendChild(row);
                }

                if (feasibleCount > 500)
                    AddP($"(показаны первые 500 из {feasibleCount} допустимых точек)");
            }
            else
            {
                AddP("Нет допустимых точек.");
            }

            mainPart.Document.Save();
        }

        private static bool CheckConstraint(CriterionFunction c, double val)
        {
            if (double.IsNaN(val)) return false;
            return c.ConstraintType switch
            {
                ConstraintType.GreaterOrEqual => val >= c.Threshold,
                ConstraintType.LessOrEqual    => val <= c.Threshold,
                ConstraintType.Range          => val >= c.Threshold && val <= c.ThresholdMax,
                _                             => true
            };
        }
    }
}