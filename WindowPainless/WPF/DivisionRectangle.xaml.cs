using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowPainless.WPF
{
    /// <summary>
    /// Interaction logic for DivisionRectangle.xaml
    /// </summary>
    public partial class DivisionRectangle
    {
        public DivisionRectangle()
        {
            InitializeComponent();

            var enabledDescriptor = DependencyPropertyDescriptor.FromProperty(EnabledProperty, typeof(GridControl));

            enabledDescriptor?.AddValueChanged(this, EnabledValueChangedHandler);
        }

        private void EnabledValueChangedHandler(object sender, EventArgs e)
        {
            if (Enabled)
            {
                divisionRectangle.Fill = new SolidColorBrush(Colors.LightSeaGreen);
            }
            else
            {
                divisionRectangle.Fill = SystemColors.ControlLightBrush;
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public Division Division { get; set; }

        public event EventHandler StatusChanged;

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.Register("Enabled", typeof(bool), typeof(DivisionRectangle));

        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Enabled = !Enabled;
        }
    }
}