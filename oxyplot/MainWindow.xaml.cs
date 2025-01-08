using System;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        // Store the data to maintain the connection between points
        private List<DateTime> dataX = new List<DateTime>();
        private List<double> dataY = new List<double>();

        // Define the visible window of data (number of points to display)
        private int windowSize = 50;

        public MainWindow()
        {
            InitializeComponent();

            // Setup plot in the Loaded event (recommended for .NET Framework)
            Loaded += (s, e) =>
            {
                // Setup OxyPlot PlotModel
                var plotModel = new PlotModel { Title = "Real-time Data Plot" };

                // Add LineSeries to the PlotModel
                var lineSeries = new LineSeries
                {
                    Title = "I1",
                    Color = OxyColors.SkyBlue,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.Circle
                };

                // Add LineSeries to the PlotModel
                plotModel.Series.Add(lineSeries);

                // Add DateTime axis for the x-axis
                plotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "HH:mm:ss",
                    Title = "Timestamp"
                });

                // Add Linear axis for the y-axis
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Value"
                });

                // Set plot model to the OxyPlot control
                plotView.Model = plotModel;
            };

            // Start a background thread to simulate updating the plot
            StartUpdatingPlot();
        }

        // Define a class for the I1 data
        public class DataPoint
        {
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }

            // Add a constructor that takes two arguments
            public DataPoint(DateTime timestamp, double value)
            {
                Timestamp = timestamp;
                Value = value;
            }
        }

        // Method to simulate background data update
        private void StartUpdatingPlot()
        {
            Thread backgroundThread = new Thread(() =>
            {
                // Store the data to maintain the connection between points
                List<DateTime> dataXa = new List<DateTime>();
                List<double> dataYa = new List<double>();

                // Define the visible window of data (number of points to display)
                // Read JSON data from a file (you can replace the file path accordingly)
                string jsonFilePath = "C:/Users/sriha/Downloads/c213f341-b340-4a63-89e1-353554c8490a.json";  // Specify the correct path
                string jsonContent = File.ReadAllText(jsonFilePath);

                // Deserialize the JSON into a C# object
                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                string i1DataJson = jsonObject["I1"];

                // Deserialize the I1 JSON array into a list of DataPoint objects
                List<DataPoint> dataPoints = JsonConvert.DeserializeObject<List<DataPoint>>(i1DataJson);

                foreach (var point in dataPoints)
                {
                    // Add data to the X and Y lists
                    dataX.Add(point.Timestamp);
                    dataY.Add(point.Value);

                    // Update plot on the UI thread using Dispatcher
                    Dispatcher.Invoke(() =>
                    {
                        var plotModel = plotView.Model;

                        // Get the line series from the plot model
                        var lineSeries = plotModel.Series[0] as LineSeries;

                        if (lineSeries != null)
                        {
                            // Add new data point to the line series
                            lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));

                            // Optionally remove old data points to keep the window size fixed
                            if (lineSeries.Points.Count > windowSize)
                            {
                                lineSeries.Points.RemoveAt(0); // Remove first point to simulate scrolling effect
                            }

                            // Adjust X and Y axis dynamically
                            var maxX = DateTimeAxis.ToDouble(dataX[dataX.Count - 1]);
                            var minX = DateTimeAxis.ToDouble(dataX[Math.Max(dataX.Count - windowSize, 0)]);
                            var minY = Math.Min(dataY.Min(), 0);
                            var maxY = Math.Max(dataY.Max(), 300);

                            // Set axis limits dynamically
                            plotModel.Axes[0].Minimum = minX;
                            plotModel.Axes[0].Maximum = maxX;
                            plotModel.Axes[1].Minimum = minY;
                            plotModel.Axes[1].Maximum = maxY;

                            // Refresh the plot
                            plotView.InvalidatePlot(true);
                        }
                    });

                    Thread.Sleep(500); // Delay between updates (500ms for demonstration)
                }
            });

            backgroundThread.IsBackground = true; // Set background thread so it doesn't block app shutdown
            backgroundThread.Start();
        }
    }
}
