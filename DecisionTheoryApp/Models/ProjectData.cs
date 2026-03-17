using System;
using System.Collections.Generic;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Класс для хранения данных проекта
    /// </summary>
    public class ProjectData
    {
        /// <summary>
        /// Название проекта
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Дата создания проекта
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Тип метода (AHP или MainCriterion)
        /// </summary>
        public string MethodType { get; set; }

        /// <summary>
        /// Список критериев
        /// </summary>
        public List<Criterion> Criteria { get; set; }

        /// <summary>
        /// Список альтернатив
        /// </summary>
        public List<Alternative> Alternatives { get; set; }

        /// <summary>
        /// Матрица попарных сравнений для критериев
        /// </summary>
        public double[,] CriteriaComparisonMatrix { get; set; }

        /// <summary>
        /// Список матриц попарных сравнений для альтернатив по каждому критерию
        /// </summary>
        public List<double[,]> AlternativesComparisonMatrices { get; set; }

        /// <summary>
        /// Результаты расчета (веса альтернатив)
        /// </summary>
        public double[]? Results { get; set; }

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
    }
}