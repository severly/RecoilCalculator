using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;
using Microsoft.Win32;



namespace RecoilCalculator.ViewModels
{
    using OxyPlot.Series;
    using OxyPlot.Axes;
    using OxyPlot.Pdf;
    using OxyPlot.Reporting;
    using System.Windows;

    public class PlotWindowModel : INotifyPropertyChanged
    {

        
        private int colorsIndex = 0;
        private double sampleIndex = 0;

        private OxyColor[] colors = { OxyColors.Black, OxyColors.Blue, OxyColors.DarkOrange, OxyColors.SaddleBrown };
        public PlotModel model;
        private LinearAxis timeAxis, valueAxis;
        public event PropertyChangedEventHandler PropertyChanged;

        public PlotModel PlotModel
        {
            get { return model; }
            set { model = value; OnPropertyChanged("PlotModel"); }
        }

        public PlotWindowModel()
        {
            model = new PlotModel("Recoil Force Plot");
            SetUpModel();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        // Set up the visual aspects of the plot, including Legend and Axes
        private void SetUpModel()
        {
            timeAxis = new LinearAxis(AxisPosition.Bottom, 0, 1000, "Time [ms]") {
                MajorGridlineStyle = OxyPlot.LineStyle.Solid,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                IntervalLength = 80,
                TitleFontSize = 18,
            };
            
            valueAxis = new LinearAxis(AxisPosition.Left, -100, 1200, "Recoil Force [lbs]") {
                MajorGridlineStyle = OxyPlot.LineStyle.Solid,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                TitleFontSize = 18,
            };

            model.Axes.Add(timeAxis);
            model.Axes.Add(valueAxis); 
        }

        //
        public void LoadData( double[] data, double sampleRate, String product, String recoil )
        {
            sampleRate /= 1000;

            var recoilLine = new LineSeries
            {
                StrokeThickness = 2,
                MarkerSize = 3,
                MarkerStroke = OxyColors.Black,
                MarkerType = MarkerType.None,
                Color = colors[colorsIndex++],
                CanTrackerInterpolatePoints = false,
                Title = product,
                Smooth = false,
                Tag = "Recoil force = " + recoil,
            };

            colorsIndex %= 4;
            sampleIndex = 0;

            foreach( double point in data )
                recoilLine.Points.Add( new DataPoint( (sampleIndex++) / sampleRate, point) );

            timeAxis.Maximum = data.Length / sampleRate;

            // Set window size according to data minimum and maximum
            valueAxis.Maximum = 1.2 * data.Max();
            valueAxis.Minimum = data.Min() - 0.2 * data.Min();
            model.Series.Add(recoilLine);
            model.RefreshPlot(true);
        }

        public void addRecoilEnergy()
        {

        }

        // Removes all lineseries from the plotmodel
        public void clearPlot()
        {
            model.Series.Clear();
            model.RefreshPlot(true);
        }


        public void UpdateModel( double[] data, String product )
        {

            var line = model.Series[0] as LineSeries;

            if (line != null)
            {
                foreach (double point in data)
                    line.Points.Add(new DataPoint(sampleIndex++, point));
            }
            else
            {
                //LoadData(data, product);
            }
        }


    }
}
