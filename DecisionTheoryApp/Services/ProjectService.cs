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
        /// Обновить критерии
        /// </summary>
        public void UpdateCriteria(List<string> criteriaNames)
        {
            if (_currentProject == null)
                throw new InvalidOperationException("Нет открытого проекта");

            _currentProject.Criteria = criteriaNames.Select(name => new Criterion(name)).ToList();
            InitializeMatrices();
            OnProjectChanged();
        }

        /// <summary>
        /// Обновить альтернативы
        /// </summary>
        public void UpdateAlternatives(List<string> alternativeNames)
        {
            if (_currentProject == null)
                throw new InvalidOperationException("Нет открытого проекта");

            _currentProject.Alternatives = alternativeNames.Select(name => new Alternative(name)).ToList();
            InitializeMatrices();
            OnProjectChanged();
        }

        /// <summary>
        /// Инициализация матриц
        /// </summary>
        private void InitializeMatrices()
        {
            if (_currentProject == null) return;

            int criteriaCount = _currentProject.Criteria.Count;
            int alternativesCount = _currentProject.Alternatives.Count;

            if (criteriaCount >= 2 && alternativesCount >= 2)
            {
                // Инициализация матрицы критериев
                _currentProject.CriteriaComparisonMatrix = CreateIdentityMatrix(criteriaCount);

                // Инициализация матриц альтернатив
                _currentProject.AlternativesComparisonMatrices.Clear();
                for (int i = 0; i < criteriaCount; i++)
                {
                    _currentProject.AlternativesComparisonMatrices.Add(CreateIdentityMatrix(alternativesCount));
                }
            }
        }

        /// <summary>
        /// Создание единичной матрицы
        /// </summary>
        private double[,] CreateIdentityMatrix(int size)
        {
            var matrix = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    matrix[i, j] = (i == j) ? 1.0 : 0.0;
                }
            }
            return matrix;
        }

        private void OnProjectChanged()
        {
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}