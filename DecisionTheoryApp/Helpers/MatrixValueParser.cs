using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DecisionTheoryApp.Helpers
{
    /// <summary>
    /// Парсер для значений матрицы попарных сравнений
    /// Поддерживает:
    /// - десятичные дроби с точкой (1.5)
    /// - десятичные дроби с запятой (1,5)
    /// - простые дроби (1/3, 2/5)
    /// - целые числа
    /// </summary>
    public static class MatrixValueParser
    {
        /// <summary>
        /// Преобразует строковое значение в double
        /// </summary>
        /// <param name="value">Строковое значение для парсинга</param>
        /// <returns>Число double</returns>
        /// <exception cref="FormatException">Выбрасывается, если не удалось распарсить</exception>
        public static double ParseToDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException("Пустое значение");
            }

            // Удаляем пробелы в начале и конце
            value = value.Trim();

            // Проверка на дробь вида "a/b"
            if (value.Contains('/'))
            {
                return ParseFraction(value);
            }

            // Заменяем запятую на точку для парсинга
            string normalizedValue = value.Replace(',', '.');

            // Пытаемся распарсить как double с инвариантной культурой
            if (double.TryParse(normalizedValue,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double result))
            {
                return result;
            }

            throw new FormatException($"Невозможно преобразовать '{value}' в число");
        }

        /// <summary>
        /// Пытается преобразовать строковое значение в double без выбрасывания исключения
        /// </summary>
        /// <param name="value">Строковое значение</param>
        /// <param name="result">Результат парсинга</param>
        /// <returns>True, если удалось распарсить</returns>
        public static bool TryParseToDouble(string value, out double result)
        {
            result = 0;

            try
            {
                result = ParseToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Парсит дробь вида "a/b"
        /// </summary>
        private static double ParseFraction(string value)
        {
            // Регулярное выражение для поиска дроби
            // Ищет числа, разделенные слешем, с возможными пробелами
            var match = Regex.Match(value, @"^\s*(\d+)\s*/\s*(\d+)\s*$");

            if (match.Success && match.Groups.Count == 3)
            {
                if (int.TryParse(match.Groups[1].Value, out int numerator) &&
                    int.TryParse(match.Groups[2].Value, out int denominator))
                {
                    if (denominator == 0)
                    {
                        throw new DivideByZeroException("Знаменатель не может быть равен нулю");
                    }

                    return (double)numerator / denominator;
                }
            }

            throw new FormatException($"Некорректный формат дроби: '{value}'");
        }

        /// <summary>
        /// Проверяет, является ли строка корректным числом для матрицы
        /// </summary>
        public static bool IsValidMatrixValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            // Проверка на дробь
            if (value.Contains('/'))
            {
                var match = Regex.Match(value, @"^\s*(\d+)\s*/\s*(\d+)\s*$");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[2].Value, out int denominator))
                    {
                        return denominator != 0;
                    }
                }
                return false;
            }

            // Проверка на десятичное число
            string normalizedValue = value.Replace(',', '.');
            return double.TryParse(normalizedValue,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out _);
        }
    }
}