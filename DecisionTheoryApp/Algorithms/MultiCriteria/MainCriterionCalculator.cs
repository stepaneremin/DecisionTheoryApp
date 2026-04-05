using System;
using System.Collections.Generic;
using System.Linq;
using DecisionTheoryApp.Models;
using NCalc;

namespace DecisionTheoryApp.Algorithms.MultiCriteria
{
    public class MainCriterionCalculator
    {
        /// <summary>
        /// Валидировать формулу — возвращает null если OK, иначе текст ошибки
        /// </summary>
        public static string? ValidateFormula(string formula, List<string> variableNames)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return "Формула не может быть пустой";
            try
            {
                var expr = BuildExpression(formula);
                foreach (var name in variableNames)
                    expr.Parameters[name] = 1.0;
                var result = expr.Evaluate();
                if (expr.Error != null)
                    return expr.Error;
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static double Evaluate(string formula, Dictionary<string, double> point)
        {
            var expr = BuildExpression(formula);
            foreach (var kv in point)
                expr.Parameters[kv.Key] = kv.Value;
            var result = expr.Evaluate();
            return Convert.ToDouble(result);
        }

        /// <summary>
        /// Создаём Expression с правильными настройками
        /// </summary>
        private static Expression BuildExpression(string formula)
        {
            // EvaluationOptions.IgnoreCase — имена функций нечувствительны к регистру
            var expr = new Expression(PrepareFormula(formula), EvaluateOptions.IgnoreCase);
            return expr;
        }

        /// <summary>
        /// Основной метод: перебирает все точки, фильтрует по ограничениям, 
        /// находит оптимум по главному критерию
        /// </summary>
        public static List<OptimizationResult> Calculate(
            List<CriterionFunction> criteria,
            List<OptimizationVariable> variables)
        {
            var mainCriterion = criteria.FirstOrDefault(c => c.IsMain)
                ?? throw new InvalidOperationException("Главный критерий не выбран");

            // Строим декартово произведение всех значений переменных
            var allPoints = BuildGrid(variables);

            var results = new List<OptimizationResult>();

            foreach (var point in allPoints)
            {
                var result = new OptimizationResult { Variables = point };

                // Вычисляем все критерии
                bool feasible = true;
                foreach (var criterion in criteria)
                {
                    double val;
                    try
                    {
                        val = Evaluate(criterion.Formula, point);
                        // Infinity и NaN — математически невалидный результат
                        if (double.IsNaN(val) || double.IsInfinity(val))
                        {
                            val = double.NaN;
                            feasible = false;
                        }
                    }
                    catch { val = double.NaN; feasible = false; }

                    result.CriteriaValues[criterion.Name] = val;

                    // Проверяем ограничение для не-главных критериев
                    if (!criterion.IsMain && !double.IsNaN(val))
                    {
                        bool ok = criterion.ConstraintType switch
                        {
                            ConstraintType.GreaterOrEqual => val >= criterion.Threshold,
                            ConstraintType.LessOrEqual => val <= criterion.Threshold,
                            ConstraintType.Range => val >= criterion.Threshold && val <= criterion.ThresholdMax,
                            _ => true
                        };
                        if (!ok) feasible = false;
                    }
                }

                result.IsFeasible = feasible;
                result.MainCriterionValue = result.CriteriaValues.ContainsKey(mainCriterion.Name)
                    ? result.CriteriaValues[mainCriterion.Name]
                    : double.NaN;

                results.Add(result);
            }

            // Сортируем допустимые точки по главному критерию
            var feasibleResults = results.Where(r => r.IsFeasible && !double.IsNaN(r.MainCriterionValue)).ToList();

            if (feasibleResults.Any())
            {
                var optimal = mainCriterion.Direction == OptimizationDirection.Maximize
                    ? feasibleResults.MaxBy(r => r.MainCriterionValue)
                    : feasibleResults.MinBy(r => r.MainCriterionValue);
                if (optimal != null) optimal.IsOptimal = true;
            }

            // Сортировка: сначала допустимые по главному критерию, потом недопустимые
            bool maximize = mainCriterion.Direction == OptimizationDirection.Maximize;
            return results
                .OrderByDescending(r => r.IsFeasible)
                .ThenBy(r =>
                {
                    if (!r.IsFeasible || double.IsNaN(r.MainCriterionValue))
                        return maximize ? double.MinValue : double.MaxValue;
                    return maximize ? -r.MainCriterionValue : r.MainCriterionValue;
                })
                .ToList();
        }

        /// <summary>
        /// Строим декартово произведение значений всех переменных
        /// </summary>
        private static List<Dictionary<string, double>> BuildGrid(List<OptimizationVariable> variables)
        {
            var result = new List<Dictionary<string, double>> { new() };

            foreach (var variable in variables)
            {
                var values = variable.GetValues();
                var newResult = new List<Dictionary<string, double>>();
                foreach (var existing in result)
                {
                    foreach (var val in values)
                    {
                        var newPoint = new Dictionary<string, double>(existing)
                        {
                            [variable.Name] = val
                        };
                        newResult.Add(newPoint);
                    }
                }
                result = newResult;
            }

            return result;
        }

        /// <summary>
        /// Подготовка формулы: ^ → Pow(), ln → Log, lg → Log10
        /// </summary>
        private static string PrepareFormula(string formula)
        {
            formula = formula
                .Replace("ln(", "Log(")
                .Replace("LN(", "Log(")
                .Replace("lg(", "Log10(")
                .Replace("LG(", "Log10(");

            formula = ReplacePow(formula);
            return formula;
        }

        /// <summary>
        /// Заменяем X^Y на Pow(X,Y) обрабатывая справа налево,
        /// чтобы корректно работать с цепочками x^y^2 и вложенными выражениями.
        /// </summary>
        private static string ReplacePow(string s)
        {
            // Обрабатываем справа налево — так цепочки a^b^c раскрываются правильно
            int idx;
            int searchFrom = s.Length - 1;

            while ((idx = s.LastIndexOf('^', searchFrom)) >= 0)
            {
                if (idx == 0) break;

                // Левый операнд
                int leftEnd = idx;
                int left = idx - 1;

                if (s[left] == ')')
                {
                    int depth = 0;
                    while (left >= 0)
                    {
                        if (s[left] == ')') depth++;
                        else if (s[left] == '(') { depth--; if (depth == 0) break; }
                        left--;
                    }
                }
                else
                {
                    while (left > 0 && (char.IsLetterOrDigit(s[left - 1]) || s[left - 1] == '.' || s[left - 1] == '_'))
                        left--;
                }

                if (left < 0) break;
                string leftOp = s.Substring(left, leftEnd - left);

                // Правый операнд
                int right = idx + 1;
                if (right >= s.Length) break;

                int rightEnd = right;

                if (s[right] == '(')
                {
                    int depth = 0;
                    while (rightEnd < s.Length)
                    {
                        if (s[rightEnd] == '(') depth++;
                        else if (s[rightEnd] == ')') { depth--; if (depth == 0) { rightEnd++; break; } }
                        rightEnd++;
                    }
                }
                else if ((s[right] == '-' || s[right] == '+') && right + 1 < s.Length && char.IsDigit(s[right + 1]))
                {
                    // Унарный знак перед числом
                    rightEnd++;
                    while (rightEnd < s.Length && (char.IsDigit(s[rightEnd]) || s[rightEnd] == '.'))
                        rightEnd++;
                }
                else
                {
                    while (rightEnd < s.Length && (char.IsLetterOrDigit(s[rightEnd]) || s[rightEnd] == '.' || s[rightEnd] == '_'))
                        rightEnd++;
                }

                if (rightEnd <= right) break;
                string rightOp = s.Substring(right, rightEnd - right);

                string replacement = $"Pow({leftOp},{rightOp})";
                s = s.Substring(0, left) + replacement + s.Substring(rightEnd);

                // Следующий поиск — левее текущей позиции
                searchFrom = left - 1;
                if (searchFrom < 0) break;
            }
            return s;
        }
    }
}
