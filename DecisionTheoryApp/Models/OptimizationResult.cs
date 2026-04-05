using System.Collections.Generic;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Одна точка в пространстве переменных с вычисленными значениями критериев
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>Значения переменных: {"x1": 2.0, "x2": 3.5}</summary>
        public Dictionary<string, double> Variables { get; set; } = new();

        /// <summary>Значения критериев: {"f1": 4.5, "f2": 12.0}</summary>
        public Dictionary<string, double> CriteriaValues { get; set; } = new();

        /// <summary>Удовлетворяет ли точка всем ограничениям</summary>
        public bool IsFeasible { get; set; } = true;

        /// <summary>Является ли эта точка оптимальной</summary>
        public bool IsOptimal { get; set; } = false;

        /// <summary>Значение главного критерия (для удобства сортировки)</summary>
        public double MainCriterionValue { get; set; } = 0.0;
    }
}
