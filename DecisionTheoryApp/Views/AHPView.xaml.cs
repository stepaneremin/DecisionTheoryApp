using System;
using System.Data;
using System.Windows;
using Microsoft.VisualBasic;
using DecisionTheoryApp.Models;

namespace DecisionTheoryApp.Views
{
    public partial class AHPView : Window
    {
        private DataTable matrixTable = new DataTable();
        private ProjectData project = new ProjectData();

        public AHPView()
        {
            InitializeComponent();
        }

        private void AddCriterion(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите критерий");

            if (!string.IsNullOrWhiteSpace(name))
            {
                CriteriaList.Items.Add(name);
                project.Criteria.Add(new Criterion { Name = name });
            }
        }

        private void AddAlternative(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Введите альтернативу");

            if (!string.IsNullOrWhiteSpace(name))
            {
                AlternativeList.Items.Add(name);
                project.Alternatives.Add(new Alternative { Name = name });
            }
        }

        private void CreateMatrix(object sender, RoutedEventArgs e)
        {
            int n = project.Criteria.Count;

            if (n == 0)
            {
                MessageBox.Show("Добавьте критерии");
                return;
            }

            matrixTable = new DataTable();

            for (int i = 0; i < n; i++)
            {
                matrixTable.Columns.Add(project.Criteria[i].Name);
            }

            for (int i = 0; i < n; i++)
            {
                var row = matrixTable.NewRow();

                for (int j = 0; j < n; j++)
                {
                    row[j] = (i == j) ? 1 : 1;
                }

                matrixTable.Rows.Add(row);
            }

            MatrixGrid.ItemsSource = matrixTable.DefaultView;
        }

        private void Calculate(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Расчет пока упрощен (матрица создана)");
        }

        private void SaveProject(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Сохранение проекта пока отключено");
        }

        private void LoadProject(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Загрузка проекта пока отключена");
        }

        private void CreateReport(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Отчет пока отключен");
        }
    }
}