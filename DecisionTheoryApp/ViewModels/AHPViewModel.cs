using DecisionTheoryApp.Algorithms.AHP;
using DecisionTheoryApp.Models;
using DecisionTheoryApp.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DecisionTheoryApp.Converters; // для NullToEmptyConverter (если нужно)

namespace DecisionTheoryApp.ViewModels
{
    public class AHPViewModel : INotifyPropertyChanged
    {
        private readonly ProjectService _projectService;
        private readonly ReportService _reportService;
        private AHPCalculator? _calculator;

        private string _projectName = "Новый проект";
        private string _newCriterionName = "";
        private string _newAlternativeName = "";
        private ObservableCollection<Criterion> _criteria;
        private ObservableCollection<Alternative> _alternatives;
        private string _statusMessage = "Готов к работе";
        private double[,]? _criteriaMatrix;
        private ObservableCollection<MatrixData> _alternativesMatrices;
        private bool _isCalculated;
        private string _consistencyMessage = "";
        private bool _hasValidationErrors;

        // История расчётов для сравнения
        public ObservableCollection<CalculationRecord> CalculationHistory { get; } = new();

        // DataTable для отображения в DataGrid
        private DataTable? _criteriaMatrixTable;
        private ObservableCollection<NamedMatrixTable> _alternativesMatricesTables;

        // Команды
        public ICommand AddCriterionCommand { get; }
        public ICommand RemoveCriterionCommand { get; }
        public ICommand AddAlternativeCommand { get; }
        public ICommand RemoveAlternativeCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand CreateReportCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand LoadProjectCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand CreateMatrixCommand { get; }
        public ICommand CreateAlternativeMatricesCommand { get; }
        public ICommand ClearProjectCommand { get; }
        public ICommand ClearHistoryCommand { get; }


        public AHPViewModel(ProjectService projectService)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _reportService = new ReportService(projectService);

            _criteria = new ObservableCollection<Criterion>();
            _alternatives = new ObservableCollection<Alternative>();
            _alternativesMatrices = new ObservableCollection<MatrixData>();
            _alternativesMatricesTables = new ObservableCollection<NamedMatrixTable>();

            // Инициализация команд
            AddCriterionCommand = new RelayCommand(AddCriterion, CanAddCriterion);
            RemoveCriterionCommand = new RelayCommand(RemoveCriterion, CanRemoveCriterion);
            AddAlternativeCommand = new RelayCommand(AddAlternative, CanAddAlternative);
            RemoveAlternativeCommand = new RelayCommand(RemoveAlternative, CanRemoveAlternative);
            CalculateCommand = new RelayCommand(Calculate, CanCalculate);
            CreateReportCommand = new RelayCommand(CreateReport, CanCreateReport);
            SaveProjectCommand = new RelayCommand(SaveProject, CanSaveProject);
            LoadProjectCommand = new RelayCommand(LoadProject);
            SelectSectionCommand = new RelayCommand(SelectSection);
            CreateMatrixCommand = new RelayCommand(CreateMatrix);
            CreateAlternativeMatricesCommand = new RelayCommand(CreateAlternativeMatrices);
            ClearProjectCommand = new RelayCommand(ClearProject);
            ClearHistoryCommand = new RelayCommand(_ =>
            {
                CalculationHistory.Clear();
                StatusMessage = "История расчётов очищена";
            }, _ => CalculationHistory.Count > 0);

            // Подписка на изменения проекта
            _projectService.ProjectChanged += OnProjectChanged;

            // Инициализация с пустым проектом
            InitializeEmptyProject();
        }

        private void InitializeEmptyProject()
        {
            _projectService.CreateNewProject("Новый проект", "AHP");
            _projectService.UpdateCriteria(new[] { "Критерий 1", "Критерий 2", "Критерий 3" }.ToList());
            _projectService.UpdateAlternatives(new[] { "Альтернатива 1", "Альтернатива 2", "Альтернатива 3" }.ToList());
            UpdateFromProject();
        }

        private void OnProjectChanged(object? sender, EventArgs e) => UpdateFromProject();

        private void UpdateFromProject()
        {
            var project = _projectService.CurrentProject;
            if (project == null) return;

            _projectName = project.ProjectName;

            _criteria.Clear();
            foreach (var c in project.Criteria)
                _criteria.Add(c);

            _alternatives.Clear();
            foreach (var a in project.Alternatives)
                _alternatives.Add(a);

            _criteriaMatrix = project.CriteriaComparisonMatrix;
            CriteriaMatrixTable = CreateCriteriaMatrixTable(); // новая таблица

            _alternativesMatrices.Clear();
            _alternativesMatricesTables.Clear();
            if (project.AlternativesComparisonMatrices != null)
            {
                var altNames = _alternatives.Select(a => a.Name).ToList();
                for (int i = 0; i < project.AlternativesComparisonMatrices.Count; i++)
                {
                    var matrix = project.AlternativesComparisonMatrices[i];
                    // Защита: матриц может быть больше чем критериев при рассинхроне
                    var criterionName = (i < project.Criteria.Count)
                        ? project.Criteria[i].Name
                        : $"Критерий {i + 1}";
                    _alternativesMatrices.Add(new MatrixData
                    {
                        Matrix = matrix,
                        CriterionName = criterionName
                    });

                    var table = CreateAlternativesMatrixTable(matrix, altNames, criterionName);
                    _alternativesMatricesTables.Add(new NamedMatrixTable
                    {
                        CriterionName = criterionName,
                        Table = table
                    });
                }
            }

            _isCalculated = project.IsCalculated;

            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(Criteria));
            OnPropertyChanged(nameof(Alternatives));
            OnPropertyChanged(nameof(CriteriaMatrixTable));
            OnPropertyChanged(nameof(AlternativesMatricesTables));
            OnPropertyChanged(nameof(IsCalculated));
            OnPropertyChanged(nameof(CanEdit));
        }

        private DataTable CreateCriteriaMatrixTable()
        {
            var table = new DataTable();
            var matrix = _projectService.CurrentProject?.CriteriaComparisonMatrix;

            // Используем безопасные имена колонок (Col0, Col1...) вместо имён критериев,
            // потому что спецсимволы в именах критериев ломают WPF-биндинг
            table.Columns.Add("CriterionName", typeof(string));
            for (int k = 0; k < Criteria.Count; k++)
                table.Columns.Add($"Col{k}", typeof(double));

            for (int i = 0; i < Criteria.Count; i++)
            {
                var row = table.NewRow();
                row["CriterionName"] = Criteria[i].Name;
                for (int j = 0; j < Criteria.Count; j++)
                {
                    double val = matrix?[i, j] ?? double.NaN;
                    row[$"Col{j}"] = (i == j) ? 1.0
                                   : double.IsNaN(val) ? DBNull.Value
                                   : (object)val;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private DataTable CreateAlternativesMatrixTable(double[,] matrix, List<string> altNames, string criterionName)
        {
            var table = new DataTable();
            table.ExtendedProperties["CriterionName"] = criterionName;

            // Безопасные имена колонок — спецсимволы в именах альтернатив ломают WPF-биндинг
            table.Columns.Add("AlternativeName", typeof(string));
            for (int k = 0; k < altNames.Count; k++)
                table.Columns.Add($"Col{k}", typeof(double));

            for (int i = 0; i < altNames.Count; i++)
            {
                var row = table.NewRow();
                row["AlternativeName"] = altNames[i];
                for (int j = 0; j < altNames.Count; j++)
                {
                    double val = matrix[i, j];
                    row[$"Col{j}"] = (i == j) ? 1.0
                                   : double.IsNaN(val) ? DBNull.Value
                                   : (object)val;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private void UpdateCriteriaMatrixTable()
        {
            if (_criteriaMatrix == null || _criteria.Count == 0)
            {
                _criteriaMatrixTable = null;
                return;
            }
            _criteriaMatrixTable = CreateDataTableFromMatrix(_criteriaMatrix, _criteria.Select(c => c.Name).ToList());
            OnPropertyChanged(nameof(CriteriaMatrixTable));
        }

        private DataTable CreateDataTableFromMatrix(double[,] matrix, List<string> labels)
        {
            var table = new DataTable();
            table.Columns.Add(" "); // для заголовков строк
            foreach (var label in labels)
                table.Columns.Add(label);

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                var row = table.NewRow();
                row[0] = labels[i];
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    row[j + 1] = matrix[i, j];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private void AddCriterion(object? parameter)
        {
            string baseName;
            if (string.IsNullOrWhiteSpace(_newCriterionName))
                baseName = $"Критерий {Criteria.Count + 1}";
            else
                baseName = _newCriterionName;

            // Генерируем уникальное имя, если такое уже есть
            string newName = baseName;
            int counter = 1;
            while (Criteria.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                newName = $"{baseName} ({++counter})";
            }

            var newList = Criteria.Select(c => c.Name).ToList();
            newList.Add(newName);
            _projectService.UpdateCriteria(newList);

            _newCriterionName = "";
            OnPropertyChanged(nameof(NewCriterionName));
            StatusMessage = $"Добавлен критерий: {newName}";
        }

        private bool CanAddCriterion(object? parameter) => Criteria.Count < 20;

        private void RemoveCriterion(object? parameter)
        {
            if (parameter is Criterion criterion)
            {
                var newList = Criteria.Where(c => c.Id != criterion.Id).Select(c => c.Name).ToList();
                _projectService.UpdateCriteria(newList);
                StatusMessage = $"Удален критерий: {criterion.Name}";
            }
        }

        private bool CanRemoveCriterion(object? parameter) => Criteria.Count > 0 && parameter is Criterion;

        private void AddAlternative(object? parameter)
        {
            string baseName;
            if (string.IsNullOrWhiteSpace(_newAlternativeName))
                baseName = $"Альтернатива {Alternatives.Count + 1}";
            else
                baseName = _newAlternativeName;

            string newName = baseName;
            int counter = 1;
            while (Alternatives.Any(a => a.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                newName = $"{baseName} ({++counter})";
            }

            var newList = Alternatives.Select(a => a.Name).ToList();
            newList.Add(newName);
            _projectService.UpdateAlternatives(newList);

            _newAlternativeName = "";
            OnPropertyChanged(nameof(NewAlternativeName));
            StatusMessage = $"Добавлена альтернатива: {newName}";
        }

        private bool CanAddAlternative(object? parameter) => Alternatives.Count < 20;

        private void RemoveAlternative(object? parameter)
        {
            if (parameter is Alternative alternative)
            {
                var newList = Alternatives.Where(a => a.Id != alternative.Id).Select(a => a.Name).ToList();
                _projectService.UpdateAlternatives(newList);
                StatusMessage = $"Удалена альтернатива: {alternative.Name}";
            }
        }

        private bool CanRemoveAlternative(object? parameter) => Alternatives.Count > 0 && parameter is Alternative;

        private void Calculate(object? parameter)
        {
            // Баг 2: проверяем минимальное количество до расчёта
            if (Criteria.Count < 2)
            {
                MessageBox.Show(
                    "Для выполнения расчёта необходимо минимум 2 критерия.\nСейчас критериев: " + Criteria.Count,
                    "Недостаточно критериев",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            if (Alternatives.Count < 2)
            {
                MessageBox.Show(
                    "Для выполнения расчёта необходимо минимум 2 альтернативы.\nСейчас альтернатив: " + Alternatives.Count,
                    "Недостаточно альтернатив",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Выполняется расчет...";
                var project = _projectService.CurrentProject;
                if (project == null) return;

                _calculator = new AHPCalculator(project);
                _calculator.Calculate();
                _isCalculated = true;

                _consistencyMessage = $"Согласованность критериев: {_calculator.GetConsistencyInterpretation(_calculator.CriteriaConsistencyRatio)} ({_calculator.CriteriaConsistencyRatio * 100:F1}%)\n";
                for (int i = 0; i < _calculator.AlternativesConsistencyRatios.Count; i++)
                {
                    _consistencyMessage += $"Согласованность по критерию '{project.Criteria[i].Name}': {_calculator.GetConsistencyInterpretation(_calculator.AlternativesConsistencyRatios[i])} ({_calculator.AlternativesConsistencyRatios[i] * 100:F1}%)\n";
                }

                StatusMessage = "Расчет выполнен успешно";
                OnPropertyChanged(nameof(IsCalculated));
                OnPropertyChanged(nameof(ConsistencyMessage));

                // Принудительно обновляем коллекцию чтобы Priority отобразился в UI
                // (Alternative не реализует INotifyPropertyChanged)
                var updatedAlts = _alternatives.ToList();
                _alternatives.Clear();
                foreach (var a in updatedAlts)
                    _alternatives.Add(a);

                // Добавляем запись в историю расчётов
                var record = new CalculationRecord
                {
                    Number = CalculationHistory.Count + 1,
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    Priorities = updatedAlts
                        .OrderByDescending(a => a.Priority)
                        .Select(a => new AlternativePriority
                        {
                            Name = a.Name,
                            Priority = a.Priority,
                            IsLeader = false
                        }).ToList()
                };
                // Отмечаем лидера
                if (record.Priorities.Any())
                    record.Priorities[0].IsLeader = true;
                CalculationHistory.Add(record);
                CommandManager.InvalidateRequerySuggested();

                // Баг 1: уведомление об успешном расчёте
                MessageBox.Show(
                    "Расчёт выполнен успешно!\nРезультаты доступны на вкладке «Результаты».",
                    "Расчёт завершён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при расчете: {ex.Message}";
            }
        }

        private bool CanCalculate(object? parameter) => !_hasValidationErrors;

        private void CreateReport(object? parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Сохранение отчета",
                    Filter = "Word документы (*.docx)|*.docx",
                    DefaultExt = ".docx",
                    FileName = $"{ProjectName}_отчет_{DateTime.Now:yyyyMMdd_HHmm}.docx"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "Формирование отчета...";
                    _reportService.GenerateAHPReport(dialog.FileName);
                    StatusMessage = $"Отчет сохранен: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Ошибка при создании отчета";
            }
        }

        private bool CanCreateReport(object? parameter) => IsCalculated;

        private void SaveProject(object? parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON файлы (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"{ProjectName}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _projectService.SaveProject(dialog.FileName);
                    StatusMessage = $"Проект сохранен: {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сохранении: {ex.Message}";
            }
        }

        private bool CanSaveProject(object? parameter) => _projectService.CurrentProject != null;

        private void LoadProject(object? parameter)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON файлы (*.json)|*.json",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _projectService.LoadProject(dialog.FileName);
                    StatusMessage = $"Проект загружен: {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при загрузке: {ex.Message}";
            }
        }

        private void ClearProject(object? parameter)
        {
            var result = MessageBox.Show(
                "Все введённые данные будут удалены. Продолжить?",
                "Очистить проект",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _projectService.CreateNewProject("Новый проект", "AHP");
            _projectService.UpdateCriteria(new[] { "Критерий 1", "Критерий 2", "Критерий 3" }.ToList());
            _projectService.UpdateAlternatives(new[] { "Альтернатива 1", "Альтернатива 2", "Альтернатива 3" }.ToList());
            _consistencyMessage = "";
            OnPropertyChanged(nameof(ConsistencyMessage));
            StatusMessage = "Проект очищен";
        }

        private void CreateMatrix(object? parameter)
        {
            // логика создания матрицы критериев (если нужно)
            // можно вызвать _projectService.UpdateCriteria, который пересоздаст матрицу
            _projectService.UpdateCriteria(Criteria.Select(c => c.Name).ToList());
        }

        private void CreateAlternativeMatrices(object? parameter)
        {
            // логика создания матриц альтернатив
            _projectService.UpdateAlternatives(Alternatives.Select(a => a.Name).ToList());
        }

        private void SelectSection(object? parameter)
        {
            // Можно использовать для навигации, если нужно
        }

        public void Cleanup()
        {
            try
            {
                _projectService.ProjectChanged -= OnProjectChanged;
                _criteria?.Clear();
                _alternatives?.Clear();
                _alternativesMatrices?.Clear();
                _alternativesMatricesTables?.Clear();
                _calculator = null;
                _criteriaMatrix = null;
            }
            catch { }
        }

        // Свойства
        public string ProjectName { get => _projectName; set { _projectName = value; OnPropertyChanged(); } }
        public ObservableCollection<Criterion> Criteria { get => _criteria; set { _criteria = value; OnPropertyChanged(); } }
        public ObservableCollection<Alternative> Alternatives { get => _alternatives; set { _alternatives = value; OnPropertyChanged(); } }
        public string NewCriterionName { get => _newCriterionName; set { _newCriterionName = value; OnPropertyChanged(); } }
        public string NewAlternativeName { get => _newAlternativeName; set { _newAlternativeName = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public double[,]? CriteriaMatrix { get => _criteriaMatrix; set { _criteriaMatrix = value; UpdateCriteriaMatrixTable(); } }
        public ObservableCollection<MatrixData> AlternativesMatrices { get => _alternativesMatrices; set { _alternativesMatrices = value; OnPropertyChanged(); } }
        public bool IsCalculated { get => _isCalculated; set { _isCalculated = value; OnPropertyChanged(); } }
        public string ConsistencyMessage { get => _consistencyMessage; set { _consistencyMessage = value; OnPropertyChanged(); } }
        public bool CanEdit => !IsCalculated;
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set { _hasValidationErrors = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // DataTable для отображения
        public DataTable? CriteriaMatrixTable { get => _criteriaMatrixTable; private set { _criteriaMatrixTable = value; OnPropertyChanged(); } }
        public ObservableCollection<NamedMatrixTable> AlternativesMatricesTables => _alternativesMatricesTables;

        // Доступ к сервису для code-behind (запись обратных значений без перестройки UI)
        public ProjectService ProjectService => _projectService;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MatrixData
    {
        public double[,] Matrix { get; set; } = new double[0, 0];
        public string CriterionName { get; set; } = "";
    }

    public class NamedMatrixTable
    {
        public string CriterionName { get; set; } = "";
        public DataTable Table { get; set; } = new DataTable();
    }

    /// <summary>Одна запись в истории расчётов</summary>
    public class CalculationRecord
    {
        public int Number { get; set; }
        public string Time { get; set; } = "";
        public List<AlternativePriority> Priorities { get; set; } = new();
    }

    /// <summary>Приоритет одной альтернативы в записи истории</summary>
    public class AlternativePriority
    {
        public string Name { get; set; } = "";
        public double Priority { get; set; }
        public bool IsLeader { get; set; }
        public string DisplayText => IsLeader
            ? $"★ {Priority:P1}"
            : $"{Priority:P1}";
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}