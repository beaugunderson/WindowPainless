using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WindowPainless.WPF
{
    /// <summary>
    /// Interaction logic for GridControl.xaml
    /// </summary>
    public sealed partial class GridControl
    {
        public GridControl()
        {
            InitializeComponent();

            MainGrid.DataContext = this;

            var rowsDescriptor = DependencyPropertyDescriptor.FromProperty(RowsProperty, typeof(GridControl));
            var columnsDescriptor = DependencyPropertyDescriptor.FromProperty(ColumnsProperty, typeof(GridControl));

            rowsDescriptor?.AddValueChanged(this, RowsOrColumnsChangedHandler);
            columnsDescriptor?.AddValueChanged(this, RowsOrColumnsChangedHandler);
        }

        public event EventHandler<DivisionsChangedEventArgs> DivisionsChanged;

        public List<DivisionRectangle> Divisions { get; } = new List<DivisionRectangle>();

        private void RowsOrColumnsChangedHandler(object sender, EventArgs eventArgs)
        {
            if (Rows == 0 || Columns == 0)
            {
                return;
            }

            var width = new GridLength(1.0 / Columns, GridUnitType.Star);
            var height = new GridLength(1.0 / Rows, GridUnitType.Star);

            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();
            MainGrid.Children.Clear();

            Divisions.Clear();

            for (var i = 0; i < Rows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition() { Height = height });
            }

            for (var i = 0; i < Columns; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = width });
            }

            var divisionGrid = new DivisionGrid() {Width = Columns, Height = Rows };

            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Columns; j++)
                {
                    var divisionRectangle = new DivisionRectangle
                    {
                        Division = new Division()
                        {
                            Width = Columns,
                            Height = Rows,

                            X = j + 1,
                            Y = i + 1
                        },
                    };

                    divisionRectangle.StatusChanged += DivisionRectangleOnStatusChanged;

                    Divisions.Add(divisionRectangle);

                    Grid.SetRow(divisionRectangle, i);
                    Grid.SetColumn(divisionRectangle, j);

                    MainGrid.Children.Add(divisionRectangle);
                }
            }
        }

        private void DivisionRectangleOnStatusChanged(object sender, EventArgs eventArgs)
        {
            var divisionRectangle = (DivisionRectangle)sender;

            var args = new DivisionsChangedEventArgs()
            {
                Division = divisionRectangle.Division,
                Enabled = divisionRectangle.Enabled
            };

            DivisionsChanged?.Invoke(this, args);
        }

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(GridControl));

        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(GridControl));

        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }
    }
}