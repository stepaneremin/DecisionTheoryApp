using System;
using System.Collections.Generic;
using System.Linq;
using DecisionTheoryApp.Models;

namespace DecisionTheoryApp.Algorithms.AHP
{
    /// <summary>
    /// Калькулятор для метода анализа иерархий (МАИ)
    /// </summary>dd
    public class AHPCalculator
    {
        private readonly ProjectData _projectData;

        /// <summary>
        /// Отношение согласованности для матрицы критериев
        /// </summary>
        public double CriteriaConsistencyRatio { get; private set; }

        /// <summary>
        /// Список отношений согласованности для матриц альтернатив
        /// </summary>
        public List<double> AlternativesConsistencyRatios { get; private set; }

        public AHPCalculator(ProjectData projectData)
        {
            _projectData = projectData ?? throw new ArgumentNullException(nameof(projectData));
            AlternativesConsistencyRatios = new List<double>();
        }

        /// <summary>
        /// Выполнить расчет методом анализа иерархий
        /// </summary>
        public void Calculate()
        {
            ValidateData();

            // Шаг 1: Расчет весов критериев
            var criteriaWeights = CalculatePriorityVector(_projectData.CriteriaComparisonMatrix);
            for (int i = 0; i < _projectData.Criteria.Count; i++)
            {
                _projectData.Criteria[i].Weight = criteriaWeights[i];
            }

            // Расчет согласованности для критериев
            CriteriaConsistencyRatio = CalculateConsistencyRatio(_projectData.CriteriaComparisonMatrix, criteriaWeights);

            // Шаг 2: Расчет весов альтернатив по каждому критерию
            var alternativesWeightsByCriterion = new List<double[]>();
            AlternativesConsistencyRatios.Clear();

            foreach (var matrix in _projectData.AlternativesComparisonMatrices)
            {
                var weights = CalculatePriorityVector(matrix);
                alternativesWeightsByCriterion.Add(weights);

                // Расчет согласованности для каждой матрицы альтернатив
                var consistencyRatio = CalculateConsistencyRatio(matrix, weights);
                AlternativesConsistencyRatios.Add(consistencyRatio);
            }

            // Шаг 3: Синтез - расчет глобальных приоритетов альтернатив
            int altCount = _projectData.Alternatives.Count;
            double[] globalPriorities = new double[altCount];

            for (int i = 0; i < altCount; i++)
            {
                double sum = 0;
                for (int j = 0; j < _projectData.Criteria.Count; j++)
                {
                    sum += criteriaWeights[j] * alternativesWeightsByCriterion[j][i];
                }
                globalPriorities[i] = Math.Round(sum, 4);
                _projectData.Alternatives[i].Priority = globalPriorities[i];
            }

            _projectData.Results = globalPriorities;
            _projectData.IsCalculated = true;
            _projectData.ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Расчет вектора приоритетов методом среднего геометрического
        /// </summary>
        private double[] CalculatePriorityVector(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[] priorities = new double[n];
            double sum = 0;

            // Расчет среднего геометрического по строкам
            for (int i = 0; i < n; i++)
            {
                double product = 1.0;
                for (int j = 0; j < n; j++)
                {
                    product *= matrix[i, j];
                }
                priorities[i] = Math.Pow(product, 1.0 / n);
                sum += priorities[i];
            }

            // Нормировка
            for (int i = 0; i < n; i++)
            {
                priorities[i] = Math.Round(priorities[i] / sum, 4);
            }

            return priorities;
        }

        /// <summary>
        /// Расчет отношения согласованности
        /// </summary>
        private double CalculateConsistencyRatio(double[,] matrix, double[] priorities)
        {
            int n = matrix.GetLength(0);

            // Расчет λmax
            double lambdaMax = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    sum += matrix[i, j] * priorities[j];
                }
                lambdaMax += sum / priorities[i];
            }
            lambdaMax /= n;

            // Индекс согласованности
            double consistencyIndex = (lambdaMax - n) / (n - 1);

            // Случайный индекс (таблица Саати)
            double randomIndex = GetRandomIndex(n);

            // Отношение согласованности
            return randomIndex > 0 ? Math.Round(consistencyIndex / randomIndex, 4) : 0;
        }

        /// <summary>
        /// Получение случайного индекса по таблице Саати
        /// </summary>
        private double GetRandomIndex(int n)
        {
            // Таблица случайных индексов для n от 1 до 15
            double[] ri = { 0, 0, 0, 0.58, 0.9, 1.12, 1.24, 1.32, 1.41, 1.45, 1.49, 1.51, 1.48, 1.56, 1.57, 1.59 };

            if (n <= ri.Length)
                return ri[n - 1];

            return 1.6; // Для больших n
        }

        /// <summary>
        /// Проверка корректности данных
        /// </summary>
        private void ValidateData()
        {
            int criteriaCount = _projectData.Criteria.Count;
            int alternativesCount = _projectData.Alternatives.Count;

            if (criteriaCount < 2 || criteriaCount > 20)
                throw new InvalidOperationException("Количество критериев должно быть от 2 до 20");

            if (alternativesCount < 2 || alternativesCount > 20)
                throw new InvalidOperationException("Количество альтернатив должно быть от 2 до 20");

            if (_projectData.CriteriaComparisonMatrix == null)
                throw new InvalidOperationException("Матрица сравнения критериев не инициализирована");

            if (_projectData.AlternativesComparisonMatrices == null ||
                _projectData.AlternativesComparisonMatrices.Count != criteriaCount)
                throw new InvalidOperationException("Матрицы сравнения альтернатив не инициализированы");
        }

        /// <summary>
        /// Получить интерпретацию отношения согласованности
        /// </summary>
        public string GetConsistencyInterpretation(double consistencyRatio)
        {
            if (consistencyRatio < 0.1)
                return "Отличная согласованность";
            else if (consistencyRatio < 0.2)
                return "Приемлемая согласованность";
            else
                return "Плохая согласованность (рекомендуется пересмотреть оценки)";
        }
    }
}