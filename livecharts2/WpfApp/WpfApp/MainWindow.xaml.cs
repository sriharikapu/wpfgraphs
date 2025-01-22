using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Windows;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Property to hold the line values
        public List<int?> LineValues { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the values with nullable integers
            LineValues = new List<int?>
            {
                5,
                4,
                2,
                null, // This null will create a gap in the line
                3,
                8,
                6
            };

            // Set the DataContext to enable data binding
            DataContext = this;
        }
    }
}