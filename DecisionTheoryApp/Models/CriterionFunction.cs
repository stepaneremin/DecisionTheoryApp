using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Направление оптимизации критерия
    /// </summary>
    public enum OptimizationDirection
    {
        Maximize,
        Minimize
    }

    /// <summary>
    /// Тип ограничения для не-главного критерия
    /// </summary>
    public enum ConstraintType
    {
        GreaterOrEqual, // f(x) >= Min
        LessOrEqual,    // f(x) <= Max
        Range           // Min <= f(x) <= Max
    }

    /// <summary>
    /// Критерий — математическая функция с параметрами оптимизации
    /// </summary>
    public class CriterionFunction : INotifyPropertyChanged
    {
        private string _name = "";
        private string _formula = "";
        private OptimizationDirection _direction = OptimizationDirection.Maximize;
        private bool _isMain = false;
        private ConstraintType _constraintType = ConstraintType.GreaterOrEqual;
        private string _thresholdText = "0";
        private string _thresholdMaxText = "0";
        private string _formulaError = "";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Formula
        {
            get => _formula;
            set { _formula = value; FormulaError = ""; OnPropertyChanged(); }
        }

        public OptimizationDirection Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(); }
        }

        /// <summary>Является ли этот критерий главным</summary>
        public bool IsMain
        {
            get => _isMain;
            set { _isMain = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsConstraint)); }
        }

        public bool IsConstraint => !_isMain;

        /// <summary>Тип ограничения (для не-главных критериев)</summary>
        public ConstraintType ConstraintType
        {
            get => _constraintType;
            set { _constraintType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRange)); OnPropertyChanged(nameof(IsNotRange)); }
        }

        /// <summary>Нижний порог (текстовое поле, парсится при расчёте)</summary>
        public string ThresholdText
        {
            get => _thresholdText;
            set { _thresholdText = value; OnPropertyChanged(); }
        }

        /// <summary>Верхний порог для диапазона</summary>
        public string ThresholdMaxText
        {
            get => _thresholdMaxText;
            set { _thresholdMaxText = value; OnPropertyChanged(); }
        }

        // Числовые значения для калькулятора
        public double Threshold
        {
            get => ParseDouble(_thresholdText, 0);
            set { _thresholdText = value.ToString(System.Globalization.CultureInfo.InvariantCulture); OnPropertyChanged(nameof(ThresholdText)); }
        }

        public double ThresholdMax
        {
            get => ParseDouble(_thresholdMaxText, 0);
            set { _thresholdMaxText = value.ToString(System.Globalization.CultureInfo.InvariantCulture); OnPropertyChanged(nameof(ThresholdMaxText)); }
        }

        private static double ParseDouble(string s, double fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            string norm = s.Trim().Replace(',', '.');
            return double.TryParse(norm, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : fallback;
        }

        public bool IsRange => _constraintType == ConstraintType.Range;
        public bool IsNotRange => _constraintType != ConstraintType.Range;

        /// <summary>Ошибка парсинга формулы — не сохраняется в JSON</summary>
        [JsonIgnore]
        public string FormulaError
        {
            get => _formulaError;
            set { _formulaError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
        }

        [JsonIgnore]
        public bool HasError => !string.IsNullOrEmpty(_formulaError);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>Принудительно уведомляет UI обо всех свойствах — нужно после десериализации</summary>
        public void NotifyAllProperties()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Formula));
            OnPropertyChanged(nameof(Direction));
            OnPropertyChanged(nameof(IsMain));
            OnPropertyChanged(nameof(IsConstraint));
            OnPropertyChanged(nameof(ConstraintType));
            OnPropertyChanged(nameof(IsRange));
            OnPropertyChanged(nameof(IsNotRange));
            OnPropertyChanged(nameof(ThresholdText));
            OnPropertyChanged(nameof(ThresholdMaxText));
            OnPropertyChanged(nameof(FormulaError));
            OnPropertyChanged(nameof(HasError));
        }
    }
}
