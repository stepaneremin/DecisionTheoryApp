using System;
using System.Collections.Generic;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Класс для хранения данных проекта
    /// </summary>
    public class ProjectData
    {
        public string ProjectName { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string MethodType { get; set; }

        public List<Criterion> Criteria { get; set; }

        public List<Alternative> Alternatives { get; set; }

        /// <summary>
        /// Матрица попарных сравнений для критериев
        /// </summary>
        public double[,] CriteriaComparisonMatrix { get; set; }

        /// <summary>
        /// Список матриц попарных сравнений для альтернатив (по одной на каждый критерий)
        /// </summary>
        public List<double[,]> AlternativesComparisonMatrices { get; set; }

        /// <summary>
        /// Веса критериев (результат расчета)
        /// </summary>
        public double[]? CriteriaWeights { get; set; }

        /// <summary>
        /// Локальные веса альтернатив по каждому критерию
        /// Первый индекс - критерий, второй - альтернатива
        /// </summary>
        public double[][]? AlternativesWeightsByCriterion { get; set; }

        /// <summary>
        /// Итоговые приоритеты альтернатив (результат умножения)
        /// </summary>
        public double[]? FinalPriorities { get; set; }

        /// <summary>
        /// Результаты расчета (для обратной совместимости)
        /// </summary>
        [Obsolete("Используйте FinalPriorities вместо Results")]
        public double[]? Results
        {
            get => FinalPriorities;
            set => FinalPriorities = value;
        }

        /// <summary>
        /// Признак, были ли выполнены расчеты
        /// </summary>
        public bool IsCalculated { get; set; }

        public ProjectData()
        {
            ProjectName = "Новый проект";
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            MethodType = "AHP";
            Criteria = new List<Criterion>();
            Alternatives = new List<Alternative>();
            AlternativesComparisonMatrices = new List<double[,]>();
            IsCalculated = false;
        }

        /// <summary>
        /// Получить локальные веса альтернатив для конкретного критерия
        /// </summary>
        public double[]? GetAlternativesWeightsForCriterion(int criterionIndex)
        {
            if (AlternativesWeightsByCriterion != null &&
                criterionIndex >= 0 &&
                criterionIndex < AlternativesWeightsByCriterion.Length)
            {
                return AlternativesWeightsByCriterion[criterionIndex];
            }
            return null;
        }

        /// <summary>
        /// Получить итоговые приоритеты с сортировкой по убыванию
        /// </summary>
        public (Alternative Alternative, double Priority)[] GetSortedFinalPriorities()
        {
            if (FinalPriorities == null || Alternatives == null)
                return Array.Empty<(Alternative, double)>();

            var results = new List<(Alternative, double)>();
            for (int i = 0; i < Alternatives.Count && i < FinalPriorities.Length; i++)
            {
                results.Add((Alternatives[i], FinalPriorities[i]));
            }

            return results.OrderByDescending(x => x.Item2).ToArray();
        }

        /// <summary>
        /// Получить лучшую альтернативу
        /// </summary>
        public Alternative? GetBestAlternative()
        {
            var sorted = GetSortedFinalPriorities();
            return sorted.FirstOrDefault().Alternative;
        }

        /// <summary>
        /// Проверить, все ли матрицы заполнены
        /// </summary>
        public bool AreAllMatricesFilled()
        {
            // Проверка матрицы критериев
            if (CriteriaComparisonMatrix == null)
                return false;

            // Проверка матриц альтернатив
            if (AlternativesComparisonMatrices == null ||
                AlternativesComparisonMatrices.Count != Criteria.Count)
                return false;

            foreach (var matrix in AlternativesComparisonMatrices)
            {
                if (!IsMatrixFilled(matrix))
                    return false;
            }

            return true;
        }

        private bool IsMatrixFilled(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    // Проверяем, не NaN ли значение (незаполненные ячейки)
                    if (double.IsNaN(matrix[i, j]) && i != j)
                        return false;

                    // Проверяем, что значения положительные (если не NaN)
                    if (!double.IsNaN(matrix[i, j]) && matrix[i, j] <= 0)
                        return false;
                }
            }
            return true;
        }
    }
}