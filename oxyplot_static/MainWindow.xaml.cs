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
using OxyPlot.Annotations;
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
        private bool isStatic = false;
        // List to store the selected series
        private List<string> selectedSeries = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            // Create PlotModel and configure axes and series
            //Title = "Real-time Data Plot"
            var plotModel = new PlotModel { };

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
            var xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm:ss",
                AxisTickToLabelDistance = 0,
                AxisDistance = 10

                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Dot
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                //MajorGridlineStyle = LineStyle.Solid
                //MinorGridlineStyle = LineStyle.Dot
            };

            // Add axes to the plot model
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

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

        private void StaticToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            // When Checked, set the status to "Static"
            StatusTextBlock.Text = "Static Mode";
            isStatic = true; // Update the mode to static
            StartUpdatingPlot();
        }

        private void StaticToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // When Unchecked, set the status to "Dynamic"
            StatusTextBlock.Text = "Dynamic Mode";
            isStatic = false; // Update the mode to dynamic
            StartUpdatingPlot();

        }


        // Add series to the list when checked
        private void OnCheckboxChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && !selectedSeries.Contains(checkBox.Content.ToString()))
            {
                selectedSeries.Add(checkBox.Content.ToString());
            }

            // Call dynamicPlot immediately when the checkbox is checked
            dynamicPlot(selectedSeries.ToArray());
        }

        // Remove series from the list when unchecked
        private void OnCheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null && selectedSeries.Contains(checkBox.Content.ToString()))
            {
                selectedSeries.Remove(checkBox.Content.ToString());
            }

            // Call dynamicPlot immediately when the checkbox is unchecked
            dynamicPlot(selectedSeries.ToArray());
        }



        private void StaticPlot(params string[] keys)
        {
            // Handle static data loading and updating
            string jsonFilePath = "C:/Users/sriha/Downloads/0aa3595f-51d0-481b-8562-b7472056f54d.json";
            string jsonContent = File.ReadAllText(jsonFilePath);

            // Deserialize the JSON file into a dictionary
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

            // Prepare data points for the given curve names
            var curveData = new Dictionary<string, List<DataPoint>>();
            foreach (var curveName in keys)
            {
                if (jsonObject.TryGetValue(curveName, out var dataJson))
                {
                    var dataPoints = JsonConvert.DeserializeObject<List<DataPoint>>(dataJson);
                    curveData[curveName] = dataPoints;
                }
                else
                {
                    throw new ArgumentException($"Curve name '{curveName}' not found in the JSON file.");
                }
            }

            // Update the plot on the UI thread
            Dispatcher.Invoke(() =>
            {
                var plotModel = plotView.Model;

                // Clear existing series in the plot model
                plotModel.Series.Clear();

                // Add series for each curve dynamically
                foreach (var curveEntry in curveData)
                {
                    var curveName = curveEntry.Key;
                    var dataPoints = curveEntry.Value;

                    var lineSeries = new LineSeries
                    {
                        Title = curveName,
                        StrokeThickness = 4,
                        MarkerSize = 0,
                        MarkerType = MarkerType.Circle
                    };

                    // Add data points to the series
                    foreach (var point in dataPoints)
                    {
                        lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));
                    }

                    plotModel.Series.Add(lineSeries);
                }

                // Update axis limits dynamically
                UpdateAxisLimits(curveData.Values.SelectMany(dp => dp), plotModel);

                // Refresh the plot
                plotView.InvalidatePlot(true);
            });
        }

        private void UpdateAxisLimits(IEnumerable<DataPoint> allDataPoints, PlotModel plotModel)
        {
            if (allDataPoints.Any())
            {
                var minX = allDataPoints.Min(dp => DateTimeAxis.ToDouble(dp.Timestamp));
                var maxX = allDataPoints.Max(dp => DateTimeAxis.ToDouble(dp.Timestamp));
                var minY = allDataPoints.Min(dp => dp.Value);
                var maxY = allDataPoints.Max(dp => dp.Value);

                var xAxis = plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                var yAxis = plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);

                if (xAxis != null)
                {
                    xAxis.Minimum = minX;
                    xAxis.Maximum = maxX;
                }

                if (yAxis != null)
                {
                    yAxis.Minimum = minY;
                    yAxis.Maximum = maxY;
                }

                plotModel.InvalidatePlot(true);
            }
        }

        private void dynamicPlot(params string[] keys)
        {
            ResetGraph();

            Thread backgroundThread = new Thread(() =>
            {
                string jsonFilePath = "C:/Users/sriha/Downloads/0aa3595f-51d0-481b-8562-b7472056f54d.json";
                string jsonContent = File.ReadAllText(jsonFilePath);

                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

                // Deserialize data for each key dynamically
                var dataPointsMap = new Dictionary<string, List<DataPoint>>();
                foreach (var key in keys)
                {
                        if (jsonObject.ContainsKey(key))
                        {
                            var dataJson = jsonObject[key];
                            dataPointsMap[key] = JsonConvert.DeserializeObject<List<DataPoint>>(dataJson);
                        }
                        else
                        {
                            throw new ArgumentException($"Key {key} not found in the JSON data.");
                        }
                    
                }

                var dataX = new List<DateTime>();
                var dataYMap = keys.ToDictionary(key => key, _ => new List<double>());
                for (int i = 0; i < dataPointsMap[keys[0]].Count; i++)
                {
                    if (isFrozen)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    var dataPoints = keys.Select(key => dataPointsMap[key][i]).ToList();

                    dataX.Add(dataPoints[0].Timestamp);

                    // Add Y values dynamically to the corresponding lists
                    for (int j = 0; j < keys.Length; j++)
                    {
                        dataYMap[keys[j]].Add(dataPoints[j].Value);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        var plotModel = plotView.Model;

                        // Update series dynamically based on the keys
                        for (int j = 0; j < keys.Length; j++)
                        {
                            var lineSeries = plotModel.Series[j] as LineSeries;
                            var point = dataPoints[j];
                            lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));

                            // Limit the number of points displayed
                            if (lineSeries.Points.Count > windowSize)
                            {
                                lineSeries.Points.RemoveAt(0);
                            }
                        }

                        // Update X and Y axis limits dynamically
                        var maxX = DateTimeAxis.ToDouble(dataX[dataX.Count - 1]);
                        var minX = DateTimeAxis.ToDouble(dataX[Math.Max(dataX.Count - windowSize, 0)]);

                        double minY = dataYMap.Values.SelectMany(y => y).Min();
                        double maxY = dataYMap.Values.SelectMany(y => y).Max();

                        double padding = (maxY - minY) * 0.1;
                        minY -= padding;
                        maxY += padding + 100;

                        plotModel.Axes[1].Minimum = minY;
                        plotModel.Axes[1].Maximum = maxY;

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



        private void StartUpdatingPlot()
        {
            if (false)
            {
                string[] labels = { "I1", "I2", "I3", "IN" };
                StaticPlot(labels);
            }
            else
            {
                string[] labels = { "I1", "I2", "I3", "IN" };
                dynamicPlot(labels);
            }
        }



        private void UpdateAxisLimits(List<DataPoint> dataPointsI1, List<DataPoint> dataPointsI2, List<DataPoint> dataPointsI3, List<DataPoint> dataPointsIN, PlotModel plotModel)
        {
            // Get the minimum and maximum values for the axes
            var allData = dataPointsI1.Concat(dataPointsI2).Concat(dataPointsI3).Concat(dataPointsIN);
            double minX = DateTimeAxis.ToDouble(allData.Min(dp => dp.Timestamp));
            double maxX = DateTimeAxis.ToDouble(allData.Max(dp => dp.Timestamp));

            double minY = Math.Min(Math.Min(dataPointsI1.Min(dp => dp.Value), Math.Min(dataPointsI2.Min(dp => dp.Value), Math.Min(dataPointsI3.Min(dp => dp.Value), dataPointsIN.Min(dp => dp.Value)))), 0);
            double maxY = Math.Max(Math.Max(dataPointsI1.Max(dp => dp.Value), Math.Max(dataPointsI2.Max(dp => dp.Value), Math.Max(dataPointsI3.Max(dp => dp.Value), dataPointsIN.Max(dp => dp.Value)))), 0);

            // Add padding to the Y-axis for better visualization
            double padding = (maxY - minY) * 0.1;
            minY -= padding;
            maxY += padding + 100;

            // Update the X and Y axis limits
            plotModel.Axes[0].Minimum = minX;
            plotModel.Axes[0].Maximum = maxX;
            plotModel.Axes[1].Minimum = minY;
            plotModel.Axes[1].Maximum = maxY;
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

        private void ResetGraph()
        {
            var plotModel = plotView.Model;

            // Reset axes if needed
            foreach (var axis in plotModel.Axes)
            {
                axis.Reset();
            }

            // Clear previous data
            foreach (var series in plotModel.Series)
            {
                (series as LineSeries)?.Points.Clear();
            }

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
            (sender as Button).Content = isFrozen ? "▣" : "▢"; // Change button text
        }

    }

}
