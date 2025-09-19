using System;
using System.Windows;

namespace OurDecor
{
    public partial class InputParametersWindow : Window
    {
        public int ProductQuantity { get; private set; }
        public double Param1 { get; private set; }
        public double Param2 { get; private set; }

        public InputParametersWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите корректное количество продукции (>0).");
                return;
            }
            if (!double.TryParse(Param1TextBox.Text, out double p1) || p1 <= 0)
            {
                MessageBox.Show("Введите корректный параметр 1 (>0).");
                return;
            }
            if (!double.TryParse(Param2TextBox.Text, out double p2) || p2 <= 0)
            {
                MessageBox.Show("Введите корректный параметр 2 (>0).");
                return;
            }

            ProductQuantity = qty;
            Param1 = p1;
            Param2 = p2;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
