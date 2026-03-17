using System;

namespace DecisionTheoryApp.Models
{
    /// <summary>
    /// Модель альтернативы для метода анализа иерархий
    /// </summary>
    public class Alternative
    {
        /// <summary>
        /// Уникальный идентификатор альтернативы
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Название альтернативы
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание альтернативы (необязательное поле)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Итоговый приоритет альтернативы (рассчитывается алгоритмом)
        /// </summary>
        public double Priority { get; set; }

        public Alternative()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Description = string.Empty;
            Priority = 0;
        }

        public Alternative(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            Description = string.Empty;
            Priority = 0;
        }

        public override string ToString()
        {
            return $"{Name} ({(Priority * 100):F1}%)";
        }
    }
}