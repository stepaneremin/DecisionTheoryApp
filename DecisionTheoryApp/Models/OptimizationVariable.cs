using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DecisionTheoryApp.Models
{
    public enum VariableType
    {
        Continuous,
        Discrete
    }

    public class OptimizationVariable : INotifyPropertyChanged
    {
        private string _name = "x";
        private VariableType _type = VariableType.Continuous;
        private string _minText = "0";
        private string _maxText = "10";
        private string _stepText = "1";
        private string _discretePoints = "1, 2, 3, 4, 5";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public VariableType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsContinuous)); OnPropertyChanged(nameof(IsDiscrete)); }
        }

        public bool IsContinuous => _type == VariableType.Continuous;
        public bool IsDiscrete => _type == VariableType.Discrete;

        // Текстовые поля — пользователь вводит что угодно, парсим при GetValues()
        public string MinText
        {
            get => _minText;
            set { _minText = value; OnPropertyChanged(); }
        }

        public string MaxText
        {
            get => _maxText;
            set { _maxText = value; OnPropertyChanged(); }
        }

        public string StepText
        {
            get => _stepText;
            set { _stepText = value; OnPropertyChanged(); }
        }

        public string DiscretePoints
        {
            get => _discretePoints;
            set { _discretePoints = value; OnPropertyChanged(); }
        }

        // Для совместимости с сериализацией и калькулятором
        public double Min
        {
            get => ParseDouble(_minText, 0);
            set { _minText = value.ToString(CultureInfo.InvariantCulture); OnPropertyChanged(nameof(MinText)); }
        }

        public double Max
        {
            get => ParseDouble(_maxText, 10);
            set { _maxText = value.ToString(CultureInfo.InvariantCulture); OnPropertyChanged(nameof(MaxText)); }
        }

        public double Step
        {
            get => ParseDouble(_stepText, 1);
            set { _stepText = value.ToString(CultureInfo.InvariantCulture); OnPropertyChanged(nameof(StepText)); }
        }

        private static double ParseDouble(string s, double fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            string norm = s.Trim().Replace(',', '.');
            return double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out double v)
                ? v : fallback;
        }

        public List<double> GetValues()
        {
            var result = new List<double>();
            if (_type == VariableType.Discrete)
            {
                foreach (var part in _discretePoints.Split(','))
                {
                    string norm = part.Trim().Replace(',', '.');
                    if (double.TryParse(norm, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double val))
                        result.Add(val);
                }
                if (result.Count == 0)
                    throw new InvalidOperationException($"Переменная '{_name}': не удалось распознать ни одной точки из '{_discretePoints}'");
            }
            else
            {
                double min  = ParseAndValidate(_minText,  _name, "Min");
                double max  = ParseAndValidate(_maxText,  _name, "Max");
                double step = ParseAndValidate(_stepText, _name, "Шаг");

                if (step <= 0)
                    throw new InvalidOperationException($"Переменная '{_name}': Шаг должен быть положительным числом (введено: '{_stepText}')");
                if (min > max)
                    throw new InvalidOperationException($"Переменная '{_name}': Min ({min}) не может быть больше Max ({max})");

                for (double v = min; v <= max + 1e-10; v += step)
                    result.Add(System.Math.Round(v, 10));
            }
            return result;
        }

        private static double ParseAndValidate(string s, string varName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new InvalidOperationException($"Переменная '{varName}': поле '{fieldName}' не заполнено");
            string norm = s.Trim().Replace(',', '.');
            if (!double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                throw new InvalidOperationException($"Переменная '{varName}': '{s}' не является корректным числом в поле '{fieldName}'");
            return v;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>Принудительно уведомляет UI обо всех свойствах — нужно после десериализации</summary>
        public void NotifyAllProperties()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(IsContinuous));
            OnPropertyChanged(nameof(IsDiscrete));
            OnPropertyChanged(nameof(MinText));
            OnPropertyChanged(nameof(MaxText));
            OnPropertyChanged(nameof(StepText));
            OnPropertyChanged(nameof(DiscretePoints));
        }
    }
}
