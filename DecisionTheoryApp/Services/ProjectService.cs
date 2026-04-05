using System;
using System.Collections.Generic;
using System.Linq;
using DecisionTheoryApp.Models;
using DecisionTheoryApp.Storage;

namespace DecisionTheoryApp.Services
{
    /// <summary>
    /// Сервис для управления проектами
    /// </summary>
    public class ProjectService
    {
        private readonly JsonStorage _storage;
        private ProjectData? _currentProject;

        public event EventHandler? ProjectChanged;

        public ProjectData? CurrentProject => _currentProject;

        public ProjectService()
        {
            _storage = new JsonStorage();
        }

        /// <summary>
        /// Создать новый проект
        /// </summary>
        public void CreateNewProject(string projectName, string methodType)
        {
            _currentProject = new ProjectData
            {
                ProjectName = projectName,
                MethodType = methodType,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            OnProjectChanged();
        }

        /// <summary>
        /// Сохранить текущий проект
        /// </summary>
        public void SaveProject(string filePath)
        {
            if (_currentProject == null)
                throw new InvalidOperationException("Нет открытого проекта");

            _currentProject.ModifiedDate = DateTime.Now;
            _storage.SaveProject(_currentProject, filePath);
        }

        /// <summary>
        /// Загрузить проект из файла
        /// </summary>
        public void LoadProject(string filePath)
        {
            _currentProject = _storage.LoadProject(filePath);
            OnProjectChanged();
        }

        /// <summary>
        /// Закрыть текущий проект
        /// </summary>
        public void CloseProject()
        {
            _currentProject = null;
            OnProjectChanged();
        }

        /// <summary>
        /// Обновить критерии, сохраняя существующие значения матрицы
        /// </summary>
        public void UpdateCriteria(List<string> criteriaNames)
        {
            if (_currentProject == null)
                throw new InvalidOperationException("Нет открытого проекта");

            var oldCriteria = _currentProject.Criteria.Select(c => c.Name).ToList();
            var oldMatrix = _currentProject.CriteriaComparisonMatrix;
            var oldAltMatrices = _currentProject.AlternativesComparisonMatrices;

            _currentProject.Criteria = criteriaNames.Select(name => new Criterion(name)).ToList();
            _currentProject.IsCalculated = false;

            int newSize = criteriaNames.Count;
            int oldSize = oldCriteria.Count;
            int altCount = _currentProject.Alternatives.Count;

            if (newSize >= 2 && altCount >= 2)
            {
                // Если размер не изменился — это переименование, переносим по индексу
                bool isRename = newSize == oldSize;

                var newMatrix = CreateIdentityMatrix(newSize);
                for (int i = 0; i < newSize; i++)
                {
                    int oldI = isRename ? i : oldCriteria.IndexOf(criteriaNames[i]);
                    if (oldI < 0 || oldI >= oldSize) continue;
                    for (int j = 0; j < newSize; j++)
                    {
                        int oldJ = isRename ? j : oldCriteria.IndexOf(criteriaNames[j]);
                        if (oldJ >= 0 && oldJ < oldSize && oldMatrix != null)
                            newMatrix[i, j] = oldMatrix[oldI, oldJ];
                    }
                }
                _currentProject.CriteriaComparisonMatrix = newMatrix;

                var newAltMatrices = new List<double[,]>();
                for (int ci = 0; ci < newSize; ci++)
                {
                    int oldCi = isRename ? ci : oldCriteria.IndexOf(criteriaNames[ci]);
                    if (oldCi >= 0 && oldAltMatrices != null && oldCi < oldAltMatrices.Count)
                        newAltMatrices.Add(oldAltMatrices[oldCi]);
                    else
                        newAltMatrices.Add(CreateEmptyMatrix(altCount));
                }
                _currentProject.AlternativesComparisonMatrices = newAltMatrices;
            }
            else
            {
                _currentProject.CriteriaComparisonMatrix = newSize > 0 ? CreateIdentityMatrix(newSize) : null;
                _currentProject.AlternativesComparisonMatrices.Clear();
            }

            OnProjectChanged();
        }

        /// <summary>
        /// Обновить альтернативы, сохраняя существующие значения матриц
        /// </summary>
        public void UpdateAlternatives(List<string> alternativeNames)
        {
            if (_currentProject == null)
                throw new InvalidOperationException("Нет открытого проекта");

            var oldAltNames = _currentProject.Alternatives.Select(a => a.Name).ToList();
            var oldAltMatrices = _currentProject.AlternativesComparisonMatrices;

            _currentProject.Alternatives = alternativeNames.Select(name => new Alternative(name)).ToList();
            _currentProject.IsCalculated = false;

            int critCount = _currentProject.Criteria.Count;
            int newSize = alternativeNames.Count;

            if (critCount >= 2 && newSize >= 2)
            {
                var newAltMatrices = new List<double[,]>();
                for (int ci = 0; ci < critCount; ci++)
                {
                    var oldMatrix = (oldAltMatrices != null && ci < oldAltMatrices.Count)
                        ? oldAltMatrices[ci] : null;

                    // Переименование — по индексу; добавление/удаление — по имени
                    bool isRename = newSize == oldAltNames.Count;
                    var newMatrix = CreateEmptyMatrix(newSize);
                    for (int i = 0; i < newSize; i++)
                    {
                        int oldI = isRename ? i : oldAltNames.IndexOf(alternativeNames[i]);
                        if (oldI < 0 || oldI >= oldAltNames.Count) continue;
                        for (int j = 0; j < newSize; j++)
                        {
                            int oldJ = isRename ? j : oldAltNames.IndexOf(alternativeNames[j]);
                            if (oldJ >= 0 && oldJ < oldAltNames.Count && oldMatrix != null)
                                newMatrix[i, j] = oldMatrix[oldI, oldJ];
                        }
                    }
                    newAltMatrices.Add(newMatrix);
                }
                _currentProject.AlternativesComparisonMatrices = newAltMatrices;
            }

            OnProjectChanged();
        }

        /// <summary>
        /// Создание единичной матрицы (для критериев)
        /// </summary>
        private double[,] CreateIdentityMatrix(int size)
        {
            var matrix = new double[size, size];
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    matrix[i, j] = (i == j) ? 1.0 : double.NaN;
            return matrix;
        }

        /// <summary>
        /// Создание пустой матрицы для альтернатив (с единицами на диагонали)
        /// </summary>
        private double[,] CreateEmptyMatrix(int size)
        {
            var matrix = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                        matrix[i, j] = 1.0; // Диагональ = 1
                    else
                        matrix[i, j] = double.NaN; // Остальные ячейки пустые
                }
            }
            return matrix;
        }

        /// <summary>
        /// Получить матрицу альтернатив для конкретного критерия
        /// </summary>
        public double[,]? GetAlternativesMatrixForCriterion(int criterionIndex)
        {
            if (_currentProject == null ||
                _currentProject.AlternativesComparisonMatrices == null ||
                criterionIndex < 0 ||
                criterionIndex >= _currentProject.AlternativesComparisonMatrices.Count)
                return null;

            return _currentProject.AlternativesComparisonMatrices[criterionIndex];
        }

        /// <summary>
        /// Обновить значение в матрице альтернатив (без перестройки UI)
        /// </summary>
        public void UpdateAlternativesMatrixValue(int criterionIndex, int row, int col, double value)
        {
            var matrix = GetAlternativesMatrixForCriterion(criterionIndex);
            if (matrix != null && row >= 0 && col >= 0 && row < matrix.GetLength(0) && col < matrix.GetLength(1))
            {
                matrix[row, col] = value;
                if (row != col)
                    matrix[col, row] = Math.Round(1.0 / value, 2);
                _currentProject!.IsCalculated = false;
            }
        }

        /// <summary>
        /// Обновить значение в матрице критериев (без перестройки UI)
        /// </summary>
        public void UpdateCriteriaMatrixValue(int row, int col, double value)
        {
            var matrix = _currentProject?.CriteriaComparisonMatrix;
            if (matrix != null && row >= 0 && col >= 0 && row < matrix.GetLength(0) && col < matrix.GetLength(1))
            {
                matrix[row, col] = value;
                if (row != col)
                    matrix[col, row] = Math.Round(1.0 / value, 2);
                _currentProject!.IsCalculated = false;
            }
            else
            {
            }
        }

        /// <summary>
        /// Проверить, все ли матрицы заполнены
        /// </summary>
        public bool AreAllMatricesFilled()
        {
            if (_currentProject == null) return false;

            // Проверка матрицы критериев
            if (_currentProject.CriteriaComparisonMatrix == null)
                return false;

            // Проверка матриц альтернатив
            if (_currentProject.AlternativesComparisonMatrices == null ||
                _currentProject.AlternativesComparisonMatrices.Count != _currentProject.Criteria.Count)
                return false;

            foreach (var matrix in _currentProject.AlternativesComparisonMatrices)
            {
                if (!IsMatrixFilled(matrix))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверить, заполнена ли матрица
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
                }
            }
            return true;
        }

        /// <summary>
        /// Сбросить результаты расчета
        /// </summary>
        public void ResetCalculation()
        {
            if (_currentProject != null)
            {
                _currentProject.IsCalculated = false;
                _currentProject.CriteriaWeights = null;
                _currentProject.AlternativesWeightsByCriterion = null;
                _currentProject.FinalPriorities = null;

                // Сбрасываем веса в критериях и альтернативах
                foreach (var criterion in _currentProject.Criteria)
                {
                    criterion.Weight = 0;
                }

                foreach (var alternative in _currentProject.Alternatives)
                {
                    alternative.Priority = 0;
                }

                OnProjectChanged();
            }
        }

        private void OnProjectChanged()
        {
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}