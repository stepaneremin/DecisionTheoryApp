using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DecisionTheoryApp.Controls
{
    /// <summary>
    /// Логика взаимодействия для MatrixEditor.xaml
    /// </summary>
    public partial class MatrixEditor : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MatrixProperty =
            DependencyProperty.Register("Matrix", typeof(double[,]), typeof(MatrixEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMatrixChanged));

        public static readonly DependencyProperty RowLabelsProperty =
            DependencyProperty.Register("RowLabels", typeof(IList<string>), typeof(MatrixEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ColumnLabelsProperty =
            DependencyProperty.Register("ColumnLabels", typeof(IList<string>), typeof(MatrixEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(MatrixEditor),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        private ObservableCollection<ObservableCollection<MatrixCell>> _cells;

        public MatrixEditor()
        {
            InitializeComponent();
            _cells = new ObservableCollection<ObservableCollection<MatrixCell>>();
            MatrixItemsControl.ItemsSource = _cells;
        }

        public double[,] Matrix
        {
            get { return (double[,])GetValue(MatrixProperty); }
            set { SetValue(MatrixProperty, value); }
        }

        public IList<string> RowLabels
        {
            get { return (IList<string>)GetValue(RowLabelsProperty); }
            set { SetValue(RowLabelsProperty, value); }
        }

        public IList<string> ColumnLabels
        {
            get { return (IList<string>)GetValue(ColumnLabelsProperty); }
            set { SetValue(ColumnLabelsProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MatrixEditor)d;
            control.UpdateMatrixCells();
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MatrixEditor)d;
            control.UpdateReadOnlyState();
        }

        private void UpdateMatrixCells()
        {
            _cells.Clear();

            if (Matrix == null) return;

            int rows = Matrix.GetLength(0);
            int cols = Matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                var row = new ObservableCollection<MatrixCell>();
                for (int j = 0; j < cols; j++)
                {
                    bool isDiagonal = i == j;
                    var cell = new MatrixCell
                    {
                        Value = Matrix[i, j].ToString("G"),
                        Row = i,
                        Column = j,
                        IsReadOnly = IsReadOnly || isDiagonal,
                        Background = isDiagonal ? new SolidColorBrush(Colors.LightGray) : new SolidColorBrush(Colors.White)
                    };
                    cell.PropertyChanged += Cell_PropertyChanged;
                    row.Add(cell);
                }
                _cells.Add(row);
            }
        }

        private void UpdateReadOnlyState()
        {
            foreach (var row in _cells)
            {
                foreach (var cell in row)
                {
                    if (cell.Row != cell.Column) // Не диагональ
                    {
                        cell.IsReadOnly = IsReadOnly;
                    }
                }
            }
        }

        private void Cell_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MatrixCell.Value))
            {
                var cell = (MatrixCell)sender;

                // Парсим значение
                if (double.TryParse(cell.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double value))
                {
                    if (value <= 0)
                    {
                        cell.Value = "1";
                        value = 1;
                    }

                    // Обновляем матрицу
                    Matrix[cell.Row, cell.Column] = value;

                    // Обновляем симметричную ячейку
                    if (cell.Row != cell.Column)
                    {
                        Matrix[cell.Column, cell.Row] = 1.0 / value;

                        // Находим и обновляем симметричную ячейку в UI
                        var symmetricCell = _cells[cell.Column][cell.Row];
                        symmetricCell.Value = (1.0 / value).ToString("G");
                    }

                    // Триггерим обновление свойства Matrix
                    var matrix = Matrix;
                    Matrix = null;
                    Matrix = matrix;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Класс для ячейки матрицы с поддержкой уведомлений
    /// </summary>
    public class MatrixCell : INotifyPropertyChanged
    {
        private string _value = "1";
        private bool _isReadOnly;
        private Brush? _background;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public int Row { get; set; }
        public int Column { get; set; }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    OnPropertyChanged(nameof(IsReadOnly));
                }
            }
        }

        public Brush? Background
        {
            get => _background;
            set
            {
                if (_background != value)
                {
                    _background = value;
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}