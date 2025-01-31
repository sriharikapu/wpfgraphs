﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using OxyPlot.Legends;
using OxyPlot.Wpf;
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
        PlotModel plotModel = new PlotModel();
        // List to store the selected series
        private List<string> selectedSeries = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            // Create PlotModel and configure axes and series
            //Title = "Real-time Data Plot"


            // Create LineSeries for each dataset
            var lineSeriesI1 = new LineSeries
            {
                Title = "I1",
                Color = OxyColors.SkyBlue,
                StrokeThickness = 4,
                LineStyle = LineStyle.Solid,
                BrokenLineColor = OxyColors.WhiteSmoke,
                BrokenLineStyle = LineStyle.Solid,
                BrokenLineThickness = 6,
                ToolTip = "I1",
                TrackerKey = "CustomTracker",
                EdgeRenderingMode = EdgeRenderingMode.PreferGeometricAccuracy

            };

            var lineSeriesI2 = new LineSeries
            {
                Title = "I2",
                Color = OxyColors.Red,
                StrokeThickness = 4,
                LineStyle = LineStyle.Dash,
                BrokenLineColor = OxyColors.WhiteSmoke,
                BrokenLineStyle = LineStyle.Solid,
                BrokenLineThickness = 6,
                ToolTip = "I2"
            };

            var lineSeriesI3 = new LineSeries
            {
                Title = "I3",
                Color = OxyColors.Green,
                StrokeThickness = 4,
                LineStyle = LineStyle.Dot,
                BrokenLineColor = OxyColors.WhiteSmoke,
                BrokenLineStyle = LineStyle.Solid,

                BrokenLineThickness = 6,
                ToolTip = "I3"
            };

            var lineSeriesIN = new LineSeries
            {
                Title = "IN",
                Color = OxyColors.Orange,
                StrokeThickness = 4,
                LineStyle = LineStyle.DashDot,
                BrokenLineColor = OxyColors.WhiteSmoke,
                BrokenLineStyle = LineStyle.Solid,
                BrokenLineThickness = 6,
                ToolTip = "IN"
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
                AxisDistance = 10,
                ExtraGridlineColor = OxyColors.Gray,
                ExtraGridlineStyle = LineStyle.LongDashDot,


                //MajorGridlineStyle = LineStyle.Solid,
                //MinorGridlineStyle = LineStyle.Dot
            };

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                ExtraGridlineColor = OxyColors.Gray,
                ExtraGridlineStyle = LineStyle.LongDashDot,
                //MajorGridlineStyle = LineStyle.Solid
                //MinorGridlineStyle = LineStyle.Dot
            };

            // Add axes to the plot model
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);


            // Inside the MainWindow constructor, after adding the series and axes
            var legend = new Legend
            {
                LegendTitle = "Data Series",
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorderThickness = 1,
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColors.White,
                LegendTextColor = OxyColors.Black,
                LegendSymbolLength = 24
            };


            //plotModel.Legends.Add(legend);

            plotView.Model = plotModel; // Bind PlotModel to PlotView


                // Define a custom tracker string in the plot model
           // plotModel.Annotations.Add(new LineAnnotation { X = 1.5, Text = "Custom Tooltip" });

           // plotModel.Annotations.Add(new LineAnnotation { X = 1.5, Text = "Custom Tooltip" });

            // Define a custom tracker string in the plot model
            plotModel.Annotations.Add(new LineAnnotation
            {
                X = 1.5,
                Text = "Custom Tooltip",
                ToolTip = "Custom Tooltip"
            });



            //plot

            //plotModel.TrackerDefinitions.Add(new TrackerDefinition
           
            //{
            //    Key = "CustomTracker",
            //    TrackerTemplate = new OxyTemplateTracker
            //    {
            //        Background = OxyColors.Black,
            //        BorderThickness = 2,
            //        BorderColor = OxyColors.White,
            //        Content = "{Title}: X={2:0.00}, Y={4:0.00}"
            //    }
            //});
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
            //0aa3595f-51d0-481b-8562-b7472056f54d
            //d125d0a8-3baa-430e-9559-3e09d308b19e
            string jsonFilePath = "C:/Users/sriha/Downloads/d125d0a8-3baa-430e-9559-3e09d308b19e.json";
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
                    List<DataPoint> temp = new List<DataPoint>();
                    if (dataPoints != null && dataPoints.Count > 1)
                    {

                        temp.Add(dataPoints[0]);
                        for (int i = 1; i < dataPoints.Count; i++)
                        {
                            foreach (DataPoint data in dataPoints)
                            {
                                try
                                {

                                    if ((data.Timestamp - temp[temp.Count - 1].Timestamp).TotalMinutes > 1)
                                    {
                                        // Add missing timestamps with null values
                                        DateTime lastTimestamp = temp[temp.Count - 1].Timestamp;
                                        DateTime currentTimestamp = data.Timestamp;

                                        while ((currentTimestamp - lastTimestamp).TotalMinutes > 1)
                                        {
                                            lastTimestamp = lastTimestamp.AddMinutes(1);
                                            //Debug.WriteLine("********************************************");
                                            //Debug.WriteLine("Data :");
                                            //Debug.WriteLine("Current Time stamp" + currentTimestamp);
                                            //Debug.WriteLine("last  Time stamp" + lastTimestamp);
                                            //Debug.WriteLine((currentTimestamp - lastTimestamp).TotalMinutes);
                                            //Debug.WriteLine(".............................................");

                                            temp.Add(new DataPoint(lastTimestamp, double.NaN)); // Add null value
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        temp.Add(data);
                                    }


                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                        curveData[curveName] = temp;
                    }
                    else
                    {
                        throw new ArgumentException($"Curve name '{curveName}' not found in the JSON file.");
                    }
                }

                // Update the plot on the UI thread
                Dispatcher.Invoke(() =>
                {
                    var plotModel = this.plotModel;

                    // Clear existing series in the plot model
                    //plotModel.Series.Clear();

                    // Add series for each curve dynamically
                    foreach (var curveEntry in curveData)
                    {
                        var curveName = curveEntry.Key;
                        var dataPoints = curveEntry.Value;

                        LineSeries lineSeries;
                        if (curveName == "I1")
                        {
                            lineSeries = plotModel.Series[0] as LineSeries;
                        }
                        else if (curveName == "I2")
                        {
                            lineSeries = plotModel.Series[1] as LineSeries;
                        }
                        else if (curveName == "I3")
                        {
                            lineSeries = plotModel.Series[2] as LineSeries;
                        }
                        else if (curveName == "IN")
                        {
                            lineSeries = plotModel.Series[3] as LineSeries;
                        }
                        else
                        {
                            throw new ArgumentException($"Curve name '{curveName}' not found in the plot model.");
                        }

                        // Add data points to the series
                        foreach (var point in dataPoints)
                        {
                            if (!double.IsNaN(point.Value))
                            {
                                lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));
                            }
                            else
                            {
                                lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), double.NaN));
                            }

                            //{
                            //    lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));
                            //}
                        }
                        //foreach (var point in dataPoints)
                        //{
                        //    if (double.IsNaN(point.Value))
                        //    {
                        //       // Debug.WriteLine("");   
                        //    }
                        //    else
                        //    {
                        //        lineSeries.Points.Add(new OxyPlot.DataPoint(DateTimeAxis.ToDouble(point.Timestamp), point.Value));
                        //    }
                        //}

                        // plotModel.Series.Add(lineSeries);
                    }

                    // Update axis limits dynamically
                    UpdateAxisLimits(curveData.Values.SelectMany(dp => dp), plotModel);

                    // Refresh the plot
                    plotView.InvalidatePlot(true);

                });
            }
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
                string jsonFilePath = "C:/Users/sriha/Downloads/d125d0a8-3baa-430e-9559-3e09d308b19e.json";
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
            if (true)
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
