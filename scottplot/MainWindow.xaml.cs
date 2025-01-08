using System;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using ScottPlot;  // Ensure this is included
using System.IO;
using Newtonsoft.Json;
using System.Windows.Media.Animation;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        // Store the data to maintain the connection between points
        private List<double> dataX = new List<double>();
        private List<double> dataY = new List<double>();

        // Define the visible window of data (number of points to display)
        private int windowSize = 50;

        public MainWindow()
        {
            InitializeComponent();

            // Setup plot in the Loaded event (recommended for .NET Framework)
            Loaded += (s, e) =>
            {

            };

            // Start a background thread to simulate updating the plot
            StartUpdatingPlot();
        }

        // Define a class for the I1 data
        public class DataPoint
        {
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }
        }

        // Method to simulate background data update
        private void StartUpdatingPlot()
        {
            Thread backgroundThread = new Thread(() =>
            {
                // Store the data to maintain the connection between points
                List<double> dataXa = new List<double>();
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

                    long unixTimestamp = ((DateTimeOffset)point.Timestamp).ToUnixTimeSeconds();
                    dataX.Add(unixTimestamp);

                    dataY.Add(point.Value);

                    //// Simulate new data
                    double[] newX = dataX.ToArray() ;
                    string labelX = point.Timestamp.ToString("HH:mm:ss"); // Use HH:mm:ss format for X
                    
    

                        // Update plot on the UI thread using Dispatcher
                        Dispatcher.Invoke(() =>
                    {
                        // Add new scatter plot with connected dots (line)
                        WpfPlot1.Plot.Clear(); // Clear existing plot before redrawing
                        WpfPlot1.Plot.Add.ScatterLine(dataX.ToArray(), dataY.ToArray()); // Redraw all points with lines
                        WpfPlot1.Plot.XLabel(labelX);


                        // Adjust the X axis to show the latest `windowSize` points (scrolling effect)
                        double maxX = dataX[dataX.Count - 1]; // Latest X value
                        double minX = maxX - windowSize; // Show only `windowSize` points

                        // Optionally adjust Y axis if desired (for automatic scaling)
                        double minY = Math.Min(dataY.Min(), 0);
                        double maxY = Math.Max(dataY.Max(), 300);

                        // Set axis limits dynamically using ScottPlot 4.x
                        WpfPlot1.Plot.Axes.SetLimits(minX, maxX, minY, maxY);

                        // Refresh the plot
                        WpfPlot1.Refresh();
                    });

                    Thread.Sleep(500); // Delay between updates (500ms for demonstration)
                }
            });

            backgroundThread.IsBackground = true; // Set background thread so it doesn't block app shutdown
            backgroundThread.Start();
        }
    }
}
