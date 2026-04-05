using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DecisionTheoryApp.Models;

namespace DecisionTheoryApp.Services
{
    /// <summary>
    /// Сервис для формирования отчетов в формате DOCX
    /// </summary>
    public class ReportService
    {
        private readonly ProjectService _projectService;

        public ReportService(ProjectService projectService)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        }

        /// <summary>
        /// Сформировать отчет по методу анализа иерархий
        /// </summary>
        public void GenerateAHPReport(string filePath)
        {
            var project = _projectService.CurrentProject;
            if (project == null)
                throw new InvalidOperationException("Нет открытого проекта");

            using (var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                // Добавление основной части документа
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                AddHeading(body, $"Отчет по методу анализа иерархий: {project.ProjectName}", 1);
                AddParagraph(body, $"Дата создания отчета: {DateTime.Now:dd.MM.yyyy HH:mm}");
                AddParagraph(body, $"Дата создания проекта: {project.CreatedDate:dd.MM.yyyy HH:mm}");
                AddParagraph(body, $"Дата последнего изменения: {project.ModifiedDate:dd.MM.yyyy HH:mm}");

                // Критерии
                AddHeading(body, "Критерии", 2);
                foreach (var criterion in project.Criteria)
                {
                    AddParagraph(body, $"• {criterion.Name} (вес: {criterion.Weight * 100:F1}%)");
                }

                // Альтернативы
                AddHeading(body, "Альтернативы", 2);
                foreach (var alternative in project.Alternatives)
                {
                    AddParagraph(body, $"• {alternative.Name} (приоритет: {alternative.Priority * 100:F1}%)");
                }

                // Матрица критериев
                AddHeading(body, "Матрица попарных сравнений критериев", 2);
                AddMatrix(body, project.CriteriaComparisonMatrix, project.Criteria.Select(c => c.Name).ToList());

                // Матрицы альтернатив
                AddHeading(body, "Матрицы попарных сравнений альтернатив", 2);
                for (int i = 0; i < project.Criteria.Count; i++)
                {
                    AddHeading(body, $"По критерию: {project.Criteria[i].Name}", 3);
                    AddMatrix(body, project.AlternativesComparisonMatrices[i],
                        project.Alternatives.Select(a => a.Name).ToList());
                }

                // Результаты
                AddHeading(body, "Результаты расчета", 2);
                if (project.IsCalculated && project.Results != null)
                {
                    var results = project.Alternatives
                        .OrderByDescending(a => a.Priority)
                        .ToList();

                    AddParagraph(body, "Итоговые приоритеты альтернатив:");

                    var table = body.AppendChild(new Table());
                    AddTableHeader(table, new[] { "Альтернатива", "Приоритет", "Ранг" });

                    for (int i = 0; i < results.Count; i++)
                    {
                        var alt = results[i];
                        AddTableRow(table, new[]
                        {
                            alt.Name,
                            $"{alt.Priority * 100:F2}%",
                            $"{i + 1}"
                        });
                    }

                    AddParagraph(body, $"Лучшая альтернатива: {results[0].Name} " +
                        $"(приоритет {results[0].Priority * 100:F2}%)");
                }
                else
                {
                    AddParagraph(body, "Расчет еще не выполнен.");
                }

                mainPart.Document.Save();
            }
        }

        private void AddHeading(Body body, string text, int level)
        {
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(text));

            var props = new ParagraphProperties();
            var styleId = $"Heading{level}";
            props.ParagraphStyleId = new ParagraphStyleId() { Val = styleId };
            paragraph.ParagraphProperties = props;
        }

        private void AddParagraph(Body body, string text)
        {
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(text));
        }

        private void AddMatrix(Body body, double[,] matrix, List<string> labels)
        {
            int n = matrix.GetLength(0);

            var table = body.AppendChild(new Table());

            // Заголовок таблицы
            var headerRow = new TableRow();
            headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(""))))); // Пустая ячейка для угла

            foreach (var label in labels)
            {
                headerRow.AppendChild(new TableCell(new Paragraph(new Run(new Text(label)))));
            }
            table.AppendChild(headerRow);

            // Данные матрицы
            for (int i = 0; i < n; i++)
            {
                var row = new TableRow();

                // Заголовок строки
                row.AppendChild(new TableCell(new Paragraph(new Run(new Text(labels[i])))));

                // Значения
                for (int j = 0; j < n; j++)
                {
                    string value;
                    if (i == j)
                        value = "1";
                    else if (double.IsNaN(matrix[i, j]))
                        value = "";
                    else
                    {
                        // Округляем до 2 знаков и убираем trailing zeros
                        // Используем русскую культуру чтобы разделитель был запятой
                        double rounded = Math.Round(matrix[i, j], 2);
                        value = rounded.ToString("G", new System.Globalization.CultureInfo("ru-RU"));
                    }

                    var cell = new TableCell(new Paragraph(new Run(new Text(value))));

                    // Выделение диагонали
                    if (i == j)
                    {
                        cell.TableCellProperties = new TableCellProperties();
                        cell.TableCellProperties.Shading = new Shading()
                        {
                            Fill = "D3D3D3",
                            Val = ShadingPatternValues.Clear
                        };
                    }

                    row.AppendChild(cell);
                }
                table.AppendChild(row);
            }

            // Добавить пустую строку после матрицы
            AddParagraph(body, "");
        }

        private void AddTableHeader(Table table, string[] headers)
        {
            var headerRow = new TableRow();

            foreach (var header in headers)
            {
                var cell = new TableCell();
                cell.AppendChild(new Paragraph(new Run(new Text(header))));

                // Делаем заголовок жирным
                cell.TableCellProperties = new TableCellProperties();
                cell.TableCellProperties.TableCellBorders = new TableCellBorders();
                cell.TableCellProperties.TableCellBorders.BottomBorder = new BottomBorder()
                {
                    Val = BorderValues.Single,
                    Size = 2
                };

                headerRow.AppendChild(cell);
            }

            table.AppendChild(headerRow);
        }

        private void AddTableRow(Table table, string[] values)
        {
            var row = new TableRow();

            foreach (var value in values)
            {
                row.AppendChild(new TableCell(new Paragraph(new Run(new Text(value)))));
            }

            table.AppendChild(row);
        }
    }
}