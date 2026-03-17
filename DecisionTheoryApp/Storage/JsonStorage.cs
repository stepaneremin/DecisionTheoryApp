using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using DecisionTheoryApp.Models;

namespace DecisionTheoryApp.Storage
{
    /// <summary>
    /// Класс для сохранения и загрузки проектов в формате JSON
    /// </summary>
    public class JsonStorage
    {
        private readonly JsonSerializerSettings _settings;

        public JsonStorage()
        {
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        /// <summary>
        /// Сохранить проект в файл
        /// </summary>
        public void SaveProject(ProjectData project, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(project, _settings);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении проекта: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Загрузить проект из файла
        /// </summary>
        public ProjectData LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Файл проекта не найден", filePath);

                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var project = JsonConvert.DeserializeObject<ProjectData>(json, _settings);

                if (project == null)
                    throw new Exception("Не удалось загрузить проект");

                return project;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при загрузке проекта: {ex.Message}", ex);
            }
        }
    }
}