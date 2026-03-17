using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DecisionTheoryApp.Models;
using DecisionTheoryApp.Algorithms.AHP;
using DecisionTheoryApp.Services;

namespace DecisionTheoryApp.ViewModels
{
    public class AHPViewModel : INotifyPropertyChanged
    {
        private readonly ProjectService _projectService;
        private readonly ReportService _reportService;
        private AHPCalculator? _calculator;

        private string _projectName = "Новый проект";
        private int _criteriaCount = 3;
        private int _alternativesCount = 3;
        private string _newCriterionName = "";
        private string _newAlternativeName = "";
        private ObservableCollection<Criterion> _criteria;
        private ObservableCollection<Alternative> _alternatives;
        private object? _selectedSection;
        private string _statusMessage = "Готов к работе";
        private double[,]? _criteriaMatrix;
        private ObservableCollection<MatrixData> _alternativesMatrices;
        private bool _isCalculated;
        private string _consistencyMessage = "";

        // Команды
        public ICommand AddCriterionCommand { get; }
        public ICommand RemoveCriterionCommand { get; }
        public ICommand AddAlternativeCommand { get; }
        public ICommand RemoveAlternativeCommand { get; }
        public ICommand CalculateCommand { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand LoadProjectCommand { get; }

        public ICommand SelectSectionCommand { get; }

        public AHPViewModel(ProjectService projectService)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _reportService = new ReportService(projectService);

            _criteria = new ObservableCollection<Criterion>();
            _alternatives = new ObservableCollection<Alternative>();
            _alternativesMatrices = new ObservableCollection<MatrixData>();

            // Инициализация команд
            AddCriterionCommand = new RelayCommand(AddCriterion, CanAddCriterion);
            RemoveCriterionCommand = new RelayCommand(RemoveCriterion, CanRemoveCriterion);
            AddAlternativeCommand = new RelayCommand(AddAlternative, CanAddAlternative);
            RemoveAlternativeCommand = new RelayCommand(RemoveAlternative, CanRemoveAlternative);
            CalculateCommand = new RelayCommand(Calculate, CanCalculate);
            GenerateReportCommand = new RelayCommand(GenerateReport, CanGenerateReport);
            SaveProjectCommand = new RelayCommand(SaveProject, CanSaveProject);
            LoadProjectCommand = new RelayCommand(LoadProject);
            SelectSectionCommand = new RelayCommand(SelectSection);

            // Подписка на изменения проекта
            _projectService.ProjectChanged += OnProjectChanged;

            // Инициализация с пустым проектом
            InitializeEmptyProject();
        }

        private void InitializeEmptyProject()
        {
            _projectService.CreateNewProject("Новый проект", "AHP");

            // Добавляем критерии по умолчанию
            _projectService.UpdateCriteria(new[] { "Критерий 1", "Критерий 2", "Критерий 3" }.ToList());

            // Добавляем альтернативы по умолчанию
            _projectService.UpdateAlternatives(new[] { "Альтернатива 1", "Альтернатива 2", "Альтернатива 3" }.ToList());

            UpdateFromProject();
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            UpdateFromProject();
        }

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

            _alternativesMatrices.Clear();
            if (project.AlternativesComparisonMatrices != null)
            {
                for (int i = 0; i < project.AlternativesComparisonMatrices.Count; i++)
                {
                    _alternativesMatrices.Add(new MatrixData
                    {
                        Matrix = project.AlternativesComparisonMatrices[i],
                        CriterionName = project.Criteria[i]?.Name ?? $"Критерий {i + 1}"
                    });
                }
            }

            _isCalculated = project.IsCalculated;

            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(Criteria));
            OnPropertyChanged(nameof(Alternatives));
            OnPropertyChanged(nameof(CriteriaMatrix));
            OnPropertyChanged(nameof(AlternativesMatrices));
            OnPropertyChanged(nameof(IsCalculated));
            OnPropertyChanged(nameof(CanEdit));
        }

        private void AddCriterion(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(_newCriterionName))
                _newCriterionName = $"Критерий {Criteria.Count + 1}";

            var newList = Criteria.Select(c => c.Name).ToList();
            newList.Add(_newCriterionName);
            _projectService.UpdateCriteria(newList);

            _newCriterionName = "";
            OnPropertyChanged(nameof(NewCriterionName));
            StatusMessage = $"Добавлен критерий: {_newCriterionName}";
        }

        private bool CanAddCriterion(object? parameter)
        {
            return Criteria.Count < 20;
        }

        private void RemoveCriterion(object? parameter)
        {
            if (parameter is Criterion criterion)
            {
                var newList = Criteria.Where(c => c.Id != criterion.Id)
                                     .Select(c => c.Name).ToList();
                _projectService.UpdateCriteria(newList);
                StatusMessage = $"Удален критерий: {criterion.Name}";
            }
        }

        private bool CanRemoveCriterion(object? parameter)
        {
            return Criteria.Count > 2 && parameter is Criterion;
        }

        private void AddAlternative(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(_newAlternativeName))
                _newAlternativeName = $"Альтернатива {Alternatives.Count + 1}";

            var newList = Alternatives.Select(a => a.Name).ToList();
            newList.Add(_newAlternativeName);
            _projectService.UpdateAlternatives(newList);

            _newAlternativeName = "";
            OnPropertyChanged(nameof(NewAlternativeName));
            StatusMessage = $"Добавлена альтернатива: {_newAlternativeName}";
        }

        private bool CanAddAlternative(object? parameter)
        {
            return Alternatives.Count < 20;
        }

        private void RemoveAlternative(object? parameter)
        {
            if (parameter is Alternative alternative)
            {
                var newList = Alternatives.Where(a => a.Id != alternative.Id)
                                         .Select(a => a.Name).ToList();
                _projectService.UpdateAlternatives(newList);
                StatusMessage = $"Удалена альтернатива: {alternative.Name}";
            }
        }

        private bool CanRemoveAlternative(object? parameter)
        {
            return Alternatives.Count > 2 && parameter is Alternative;
        }

        private void Calculate(object? parameter)
        {
            try
            {
                StatusMessage = "Выполняется расчет...";

                var project = _projectService.CurrentProject;
                if (project == null) return;

                _calculator = new AHPCalculator(project);
                _calculator.Calculate();

                _isCalculated = true;

                // Формируем сообщение о согласованности
                _consistencyMessage = $"Согласованность критериев: {_calculator.GetConsistencyInterpretation(_calculator.CriteriaConsistencyRatio)} " +
                                     $"({_calculator.CriteriaConsistencyRatio * 100:F1}%)\n";

                for (int i = 0; i < _calculator.AlternativesConsistencyRatios.Count; i++)
                {
                    _consistencyMessage += $"Согласованность по критерию '{project.Criteria[i].Name}': " +
                                          $"{_calculator.GetConsistencyInterpretation(_calculator.AlternativesConsistencyRatios[i])} " +
                                          $"({_calculator.AlternativesConsistencyRatios[i] * 100:F1}%)\n";
                }

                StatusMessage = "Расчет выполнен успешно";
                OnPropertyChanged(nameof(IsCalculated));
                OnPropertyChanged(nameof(ConsistencyMessage));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при расчете: {ex.Message}";
            }
        }

        private bool CanCalculate(object? parameter)
        {
            return Criteria.Count >= 2 && Alternatives.Count >= 2 &&
                   CriteriaMatrix != null && AlternativesMatrices.All(m => m.Matrix != null);
        }

        private void GenerateReport(object? parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word документы (*.docx)|*.docx",
                    DefaultExt = ".docx",
                    FileName = $"{ProjectName}_отчет.docx"
                };

                if (dialog.ShowDialog() == true)
                {
                    _reportService.GenerateAHPReport(dialog.FileName);
                    StatusMessage = $"Отчет сохранен: {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при создании отчета: {ex.Message}";
            }
        }

        private bool CanGenerateReport(object? parameter)
        {
            return IsCalculated;
        }

        private void SaveProject(object? parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
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

        private bool CanSaveProject(object? parameter)
        {
            return _projectService.CurrentProject != null;
        }

        private void LoadProject(object? parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
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

        private void SelectSection(object? parameter)
        {
            if (parameter is string section)
            {
                SelectedSection = section;
            }
        }

        // Свойства для привязки
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Criterion> Criteria
        {
            get => _criteria;
            set
            {
                _criteria = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Alternative> Alternatives
        {
            get => _alternatives;
            set
            {
                _alternatives = value;
                OnPropertyChanged();
            }
        }

        public string NewCriterionName
        {
            get => _newCriterionName;
            set
            {
                _newCriterionName = value;
                OnPropertyChanged();
            }
        }

        public string NewAlternativeName
        {
            get => _newAlternativeName;
            set
            {
                _newAlternativeName = value;
                OnPropertyChanged();
            }
        }

        public object? SelectedSection
        {
            get => _selectedSection;
            set
            {
                _selectedSection = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public double[,]? CriteriaMatrix
        {
            get => _criteriaMatrix;
            set
            {
                _criteriaMatrix = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MatrixData> AlternativesMatrices
        {
            get => _alternativesMatrices;
            set
            {
                _alternativesMatrices = value;
                OnPropertyChanged();
            }
        }

        public bool IsCalculated
        {
            get => _isCalculated;
            set
            {
                _isCalculated = value;
                OnPropertyChanged();
            }
        }

        public string ConsistencyMessage
        {
            get => _consistencyMessage;
            set
            {
                _consistencyMessage = value;
                OnPropertyChanged();
            }
        }

        public bool CanEdit => !IsCalculated;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Вспомогательный класс для хранения матрицы с именем критерия
    /// </summary>
    public class MatrixData
    {
        public double[,] Matrix { get; set; } = new double[0, 0];
        public string CriterionName { get; set; } = "";
    }

    /// <summary>
    /// Простая реализация ICommand для Relay команд
    /// </summary>
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