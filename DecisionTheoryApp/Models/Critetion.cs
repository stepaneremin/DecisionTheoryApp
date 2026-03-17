using System;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Модель критерия для метода анализа иерархий
    /// </summary>
    public class Criterion
    {
        /// <summary>
        /// Уникальный идентификатор критерия
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Название критерия
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание критерия (необязательное поле)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Вес критерия (рассчитывается алгоритмом)
        /// </summary>
        public double Weight { get; set; }

        public Criterion()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Description = string.Empty;
            Weight = 0;
        }

        public Criterion(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Description = string.Empty;
            Weight = 0;
        }

        public override string ToString()
        {
            return $"{Name} ({(Weight * 100):F1}%)";
        }
    }
}