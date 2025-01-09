using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private List<DateTime> dataX = new List<DateTime>();
        private List<double> dataY1 = new List<double>();
        private List<double> dataY2 = new List<double>();
        private List<double> dataY3 = new List<double>();
        private List<double> dataYN = new List<double>();
        private int windowSize = 500;
        private bool isFrozen = false;

        public MainWindow()
        {
            InitializeComponent();

            // Create PlotModel and configure axes and series
            var plotModel = new PlotModel { Title = "Real-time Data Plot" };

            // Create LineSeries for each dataset
            var lineSeriesI1 = new LineSeries
            {
                Title = "I1",
                Color = OxyColors.SkyBlue,
                StrokeThickness = 4
            };

            var lineSeriesI2 = new LineSeries
            {
                Title = "I2",
                Color = OxyColors.Red,
                StrokeThickness = 4
            };

            var lineSeriesI3 = new LineSeries
            {
                Title = "I3",
                Color = OxyColors.Green,
                StrokeThickness = 4
            };

            var lineSeriesIN = new LineSeries
            {
                Title = "IN",
                Color = OxyColors.Orange,
                StrokeThickness = 4
            };

            // Add LineSeries to the plot model
            plotModel.Series.Add(lineSeriesI1);
            plotModel.Series.Add(lineSeriesI2);
            plotModel.Series.Add(lineSeriesI3);
            plotModel.Series.Add(lineSeriesIN);

            // Configure X and Y axes
            plotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                //Title = "Timestamp"
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                //Title = "Value"
            });

            plotView.Model = plotModel; // Bind PlotModel to PlotView

            StartUpdatingPlot();
        }

        public class DataPoint
        {
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }

            public DataPoint(DateTime timestamp, double value)
            {
                Timestamp = timestamp;
                Value = value;
            }
        }

        private void StartUpdatingPlot()
        {
            Thread backgroundThread = new Thread(() =>
            {
                string jsonFilePath = "C:/Users/sriha/Downloads/c213f341-b340-4a63-89e1-353554c8490a.json";
                string jsonContent = File.ReadAllText(jsonFilePath);

                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                string i1DataJson = jsonObject["I1"];
                string i2DataJson = jsonObject["I2"];
                string i3DataJson = jsonObject["I3"];
                string inDataJson = jsonObject["IN"];

                List<DataPoint> dataPointsI1 = JsonConvert.DeserializeObject<List<DataPoint>>(i1DataJson);
                List<DataPoint> dataPointsI2 = JsonConvert.DeserializeObject<List<DataPoint>>(i2DataJson);
                List<DataPoint> dataPointsI3 = JsonConvert.DeserializeObject<List<DataPoint>>(i3DataJson);
                List<DataPoint> dataPointsIN = JsonConvert.DeserializeObject<List<DataPoint>>(inDataJson);

                for (int i = 0; i < dataPointsI1.Count; i++)
                {
                    if (isFrozen) // Skip updates if the graph is frozen
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    // Update data for all datasets
                    var pointI1 = dataPointsI1[i];
                    var pointI2 = dataPointsI2[i];
                    var pointI3 = dataPointsI3[i];
                    var pointIN = dataPointsIN[i];

                    dataX.Add(pointI1.Timestamp);
                    dataY1.Add(pointI1.Value);
                    dataY2.Add(pointI2.Value);
                    dataY3.Add(pointI3.Value);
                    dataYN.Add(pointIN.Value);

                    Dispatcher.Invoke(() =>
                    {
                        var plotModel = plotView.Model;

                        var lineSeriesI1 = plotModel.Series[0] as LineSeries;
                        var lineSeriesI2 = plotModel.Series[1] as LineSeries;
                        var lineSeriesI3 = plotModel.Series[2] as LineSeries;
                        var lineSeriesIN = plotModel.Series[3] as LineSeries;

                        // Add data points to each series
                        lineSeriesI1.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(pointI1.Timestamp), pointI1.Value));
                        lineSeriesI2.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(pointI2.Timestamp), pointI2.Value));
                        lineSeriesI3.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(pointI3.Timestamp), pointI3.Value));
                        lineSeriesIN.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(pointIN.Timestamp), pointIN.Value));

                        // Limit the number of points displayed
                        if (lineSeriesI1.Points.Count > windowSize)
                        {
                            lineSeriesI1.Points.RemoveAt(0);
                            lineSeriesI2.Points.RemoveAt(0);
                            lineSeriesI3.Points.RemoveAt(0);
                            lineSeriesIN.Points.RemoveAt(0);
                        }

                        var maxX = DateTimeAxis.ToDouble(dataX[dataX.Count - 1]);
                        var minX = DateTimeAxis.ToDouble(dataX[Math.Max(dataX.Count - windowSize, 0)]);
                       
                        // Calculate the dynamic range with some padding
                        double minY = Math.Min(Math.Min(dataY1.Min(), Math.Min(dataY2.Min(), Math.Min(dataY3.Min(), dataYN.Min()))), 0);
                        double maxY = Math.Max(Math.Max(dataY1.Max(), Math.Max(dataY2.Max(), Math.Max(dataY3.Max(), dataYN.Max()))), 0);

                        // Add padding to Y-axis for better visualization
                        double padding = (maxY - minY) * 0.1;
                        minY -= padding;
                        maxY += padding+100;

                        // Update Y-axis limits dynamically
                        plotModel.Axes[1].Minimum = minY;
                        plotModel.Axes[1].Maximum = maxY;


                        // Update axes limits
                        plotModel.Axes[0].Minimum = minX;
                        plotModel.Axes[0].Maximum = maxX;

                        plotView.InvalidatePlot(true);
                    });

                    Thread.Sleep(500);
                }
            });

            backgroundThread.IsBackground = true;
            backgroundThread.Start();
        }

        private void MyPlot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

            var plotModel = plotView.Model;
            plotModel.IsLegendVisible = true;
            // Simulate a button click programmatically by passing the FreezeButton as the sender 
            // Code to Freeze the graph
            // FreezeGraph_Click(FreezeButton, new RoutedEventArgs(Button.ClickEvent));
            plotView.InvalidatePlot(true);
        }

        private void MyPlot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {

            var plotModel = plotView.Model;
            plotModel.IsLegendVisible = false;
            plotView.InvalidatePlot(true);
        }



        private void ResetGraph_Click(object sender, RoutedEventArgs e)
        {
            var plotModel = plotView.Model;

            foreach (var axis in plotModel.Axes)
            {
                axis.Reset();
            }

            plotView.InvalidatePlot(true);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            var xAxis = plotView.Model.Axes[0] as DateTimeAxis;
            var yAxis = plotView.Model.Axes[1] as LinearAxis;

            if (xAxis != null && yAxis != null)
            {
                double xRange = xAxis.ActualMaximum - xAxis.ActualMinimum;
                double yRange = yAxis.ActualMaximum - yAxis.ActualMinimum;

                xAxis.Minimum += xRange * 0.1;
                xAxis.Maximum -= xRange * 0.1;

                yAxis.Minimum += yRange * 0.1;
                yAxis.Maximum -= yRange * 0.1;

                xAxis.Reset(); // Ensure changes are preserved
                yAxis.Reset();
                plotView.InvalidatePlot(true);
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var xAxis = plotView.Model.Axes[0] as DateTimeAxis;
            var yAxis = plotView.Model.Axes[1] as LinearAxis;

            if (xAxis != null && yAxis != null)
            {
                double xRange = xAxis.ActualMaximum - xAxis.ActualMinimum;
                double yRange = yAxis.ActualMaximum - yAxis.ActualMinimum;

                xAxis.Minimum -= xRange * 0.1;
                xAxis.Maximum += xRange * 0.1;

                yAxis.Minimum -= yRange * 0.1;
                yAxis.Maximum += yRange * 0.1;

                xAxis.Reset();
                yAxis.Reset();
                plotView.InvalidatePlot(true);
            }
        }

        private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            var xAxis = plotView.Model.Axes[0] as DateTimeAxis;

            if (xAxis != null)
            {
                double range = xAxis.ActualMaximum - xAxis.ActualMinimum;
                xAxis.Minimum -= range * 0.1;
                xAxis.Maximum -= range * 0.1;

                xAxis.Reset();
                plotView.InvalidatePlot(true);
            }
        }

        private void ScrollRight_Click(object sender, RoutedEventArgs e)
        {
            var xAxis = plotView.Model.Axes[0] as DateTimeAxis;

            if (xAxis != null)
            {
                double range = xAxis.ActualMaximum - xAxis.ActualMinimum;
                xAxis.Minimum += range * 0.1;
                xAxis.Maximum += range * 0.1;

                xAxis.Reset();
                plotView.InvalidatePlot(true);
            }
        }


        private void FreezeGraph_Click(object sender, RoutedEventArgs e)
        {
            isFrozen = !isFrozen; // Toggle the freeze state
            (sender as Button).Content = isFrozen ? "Unfreeze" : "Freeze"; // Change button text
        }

    }
}
