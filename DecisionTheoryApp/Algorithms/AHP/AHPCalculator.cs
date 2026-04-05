using System;
using System.Collections.Generic;
using System.Linq;
using DecisionTheoryApp.Models;

namespace DecisionTheoryApp.Algorithms.AHP
{
    /// <summary>
    /// Калькулятор для метода анализа иерархий (МАИ)
    /// </summary>
    public class AHPCalculator
    {
        private readonly ProjectData _projectData;

        /// <summary>
        /// Веса критериев
        /// </summary>
        public double[] CriteriaWeights { get; private set; } = Array.Empty<double>();

        /// <summary>
        /// Локальные веса альтернатив по каждому критерию
        /// </summary>
        public List<double[]> AlternativesWeightsByCriterion { get; private set; } = new();

        /// <summary>
        /// Итоговые приоритеты альтернатив
        /// </summary>
        public double[] FinalPriorities { get; private set; } = Array.Empty<double>();

        /// <summary>
        /// Отношение согласованности для матрицы критериев
        /// </summary>
        public double CriteriaConsistencyRatio { get; private set; }

        /// <summary>
        /// Список отношений согласованности для матриц альтернатив
        /// </summary>
        public List<double> AlternativesConsistencyRatios { get; private set; } = new();

        public AHPCalculator(ProjectData projectData)
        {
            _projectData = projectData ?? throw new ArgumentNullException(nameof(projectData));
        }

        /// <summary>
        /// Выполнить расчет методом анализа иерархий
        /// </summary>
        public void Calculate()
        {
            ValidateData();

            // ШАГ 1: Расчет весов критериев из матрицы критериев
            Console.WriteLine("1. Расчет весов критериев...");
            CriteriaWeights = CalculatePriorityVector(_projectData.CriteriaComparisonMatrix);
            CriteriaConsistencyRatio = CalculateConsistencyRatio(_projectData.CriteriaComparisonMatrix, CriteriaWeights);

            // Сохраняем веса в критерии
            for (int i = 0; i < _projectData.Criteria.Count; i++)
            {
                _projectData.Criteria[i].Weight = CriteriaWeights[i];
            }

            // ШАГ 2: Для каждого критерия рассчитываем веса альтернатив
            Console.WriteLine("2. Расчет весов альтернатив по каждому критерию...");
            AlternativesWeightsByCriterion.Clear();
            AlternativesConsistencyRatios.Clear();

            int criteriaCount = _projectData.Criteria.Count;
            int alternativesCount = _projectData.Alternatives.Count;

            for (int critIdx = 0; critIdx < criteriaCount; critIdx++)
            {
                var matrix = _projectData.AlternativesComparisonMatrices[critIdx];
                var criterionName = _projectData.Criteria[critIdx].Name;

                Console.WriteLine($"   Критерий: {criterionName}");

                // Проверяем, заполнена ли матрица
                if (!IsMatrixFilled(matrix))
                {
                    throw new InvalidOperationException(
                        $"Матрица для критерия '{criterionName}' не полностью заполнена");
                }

                // Рассчитываем веса альтернатив по этому критерию
                var weights = CalculatePriorityVector(matrix);
                AlternativesWeightsByCriterion.Add(weights);

                // Рассчитываем согласованность
                var consistency = CalculateConsistencyRatio(matrix, weights);
                AlternativesConsistencyRatios.Add(consistency);

                Console.WriteLine($"      Веса: {string.Join(", ", weights.Select(w => w.ToString("F3")))}");
            }

            // ШАГ 3: Формируем матрицу весов альтернатив (альтернативы × критерии)
            Console.WriteLine("3. Формирование матрицы весов альтернатив...");

            double[,] alternativesWeightsMatrix = new double[alternativesCount, criteriaCount];

            for (int critIdx = 0; critIdx < criteriaCount; critIdx++)
            {
                var altWeights = AlternativesWeightsByCriterion[critIdx];
                for (int altIdx = 0; altIdx < alternativesCount; altIdx++)
                {
                    alternativesWeightsMatrix[altIdx, critIdx] = altWeights[altIdx];
                }
            }

            // ШАГ 4: Умножаем матрицу весов альтернатив на вектор весов критериев
            Console.WriteLine("4. Умножение матрицы на вектор критериев...");
            FinalPriorities = new double[alternativesCount];

            for (int altIdx = 0; altIdx < alternativesCount; altIdx++)
            {
                double sum = 0;
                for (int critIdx = 0; critIdx < criteriaCount; critIdx++)
                {
                    sum += alternativesWeightsMatrix[altIdx, critIdx] * CriteriaWeights[critIdx];
                }
                FinalPriorities[altIdx] = Math.Round(sum, 4);

                // Сохраняем итоговый приоритет в альтернативу
                _projectData.Alternatives[altIdx].Priority = FinalPriorities[altIdx];
            }

            // Сохраняем результаты в проект
            _projectData.CriteriaWeights = CriteriaWeights;
            _projectData.AlternativesWeightsByCriterion = AlternativesWeightsByCriterion.ToArray();
            _projectData.FinalPriorities = FinalPriorities;
            _projectData.IsCalculated = true;
            _projectData.ModifiedDate = DateTime.Now;

            Console.WriteLine("5. Расчет завершен!");
            Console.WriteLine($"   Итоговые приоритеты: {string.Join(", ", FinalPriorities.Select(p => p.ToString("F3")))}");
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

            // Случайный индекс (таблица Саати для n от 1 до 15)
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
            double[] ri = { 0, 0, 0.58, 0.9, 1.12, 1.24, 1.32, 1.41, 1.45, 1.49, 1.51, 1.48, 1.56, 1.57, 1.59 };

            if (n <= ri.Length)
                return ri[n - 1];

            return 1.6; // Для больших n
        }

        /// <summary>
        /// Проверка, заполнена ли матрица
        /// </summary>
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

        /// <summary>
        /// Получить сводку результатов
        /// </summary>
        public string GetResultsSummary()
        {
            if (!_projectData.IsCalculated || FinalPriorities.Length == 0)
                return "Расчет еще не выполнен";

            var results = new System.Text.StringBuilder();
            results.AppendLine("РЕЗУЛЬТАТЫ РАСЧЕТА МЕТОДОМ АНАЛИЗА ИЕРАРХИЙ");
            results.AppendLine("=============================================");
            results.AppendLine();

            results.AppendLine("ВЕСА КРИТЕРИЕВ:");
            for (int i = 0; i < _projectData.Criteria.Count; i++)
            {
                results.AppendLine($"  {_projectData.Criteria[i].Name}: {CriteriaWeights[i] * 100:F1}%");
            }
            results.AppendLine($"Согласованность: {GetConsistencyInterpretation(CriteriaConsistencyRatio)} ({CriteriaConsistencyRatio * 100:F1}%)");
            results.AppendLine();

            results.AppendLine("ЛОКАЛЬНЫЕ ПРИОРИТЕТЫ АЛЬТЕРНАТИВ ПО КРИТЕРИЯМ:");
            for (int critIdx = 0; critIdx < _projectData.Criteria.Count; critIdx++)
            {
                results.AppendLine($"  По критерию '{_projectData.Criteria[critIdx].Name}':");
                for (int altIdx = 0; altIdx < _projectData.Alternatives.Count; altIdx++)
                {
                    results.AppendLine($"    {_projectData.Alternatives[altIdx].Name}: {AlternativesWeightsByCriterion[critIdx][altIdx] * 100:F1}%");
                }
                results.AppendLine($"    Согласованность: {GetConsistencyInterpretation(AlternativesConsistencyRatios[critIdx])} ({AlternativesConsistencyRatios[critIdx] * 100:F1}%)");
            }
            results.AppendLine();

            results.AppendLine("ИТОГОВЫЕ ПРИОРИТЕТЫ АЛЬТЕРНАТИВ:");
            var sortedAlts = _projectData.Alternatives
                .Select((alt, idx) => new { Alt = alt, Priority = FinalPriorities[idx] })
                .OrderByDescending(x => x.Priority)
                .ToList();

            for (int i = 0; i < sortedAlts.Count; i++)
            {
                string marker = (i == 0) ? "★ " : "  ";
                results.AppendLine($"{marker}{sortedAlts[i].Alt.Name}: {sortedAlts[i].Priority * 100:F1}%");
            }
            results.AppendLine();
            results.AppendLine($"Лучшая альтернатива: {sortedAlts[0].Alt.Name}");

            return results.ToString();
        }
    }
}