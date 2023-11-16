using Avalonia.Controls.Primitives;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.Services
{
    public class ProbeGraph : ReactiveObject
    {
        private ISeries[] _probeSeries;
        public ISeries[] ProbeSeries
        {
            get => _probeSeries;
            set => this.RaiseAndSetIfChanged(ref _probeSeries, value);
        }
        public LineSeries<double?> LineSeries { get; set; }
        public ScatterSeries<ObservablePoint> ScatterSeries { get; set; }
        public List<double?> Data { get; set; }
        public string Title { get; set; }

        public RectangularSection[] Thumbs { get; set; }

        public ReactiveCommand<PointerCommandArgs, Unit> PointerDownCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerMoveCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerUpCommand { get; }

        private bool isDragging = false;

        public ProbeGraph(string title)
        {
            Thumbs = new[]
            {
                new RectangularSection
                {
                    Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100)),
                    Xi = 0,
                    Xj = 0,
                    Stroke = new SolidColorPaint
                    {
                        Color = SKColors.Red,
                        StrokeThickness = 3,
                        ZIndex = 2
                    }
                },
                new RectangularSection
                {
                    Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100)),
                    Xi = 0,
                    Xj = 0,
                    Stroke = new SolidColorPaint
                    {
                        Color = SKColors.DeepSkyBlue,
                        StrokeThickness = 3,
                        ZIndex = 2
                    }
                }
            };

            Title = title;

            LineSeries = new LineSeries<double?>();

            ProbeSeries = new ISeries[]
            {
                LineSeries,
            };
        }

        public ProbeGraph(List<double?> data, string title)
        {
            Data = data;
            Title = title;

            Thumbs = new[]
            {
                new RectangularSection
                {
                    Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100)),
                    Xi = 0,
                    Xj = 0,
                    Stroke = new SolidColorPaint
                    {
                        Color = SKColors.Red,
                        StrokeThickness = 3,
                        ZIndex = 2
                    }
                },
                new RectangularSection
                {
                    Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100)),
                    Xi = data.Count - 1,
                    Xj = data.Count - 1,
                    Stroke = new SolidColorPaint
                    {
                        Color = SKColors.DeepSkyBlue,
                        StrokeThickness = 3,
                        ZIndex = 2
                    }
                }
            };

            SetProbeSeriesData(data);

            PointerDownCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerDown);
            PointerMoveCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerMove);
            PointerUpCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerUp);
        }

        private void SetProbeSeriesData(List<double?> data)
        {
            Thumbs[0].Xi = 0;
            Thumbs[0].Xj = 0;

            Thumbs[1].Xi = data.Count - 1;
            Thumbs[1].Xj = data.Count - 1;

            LineSeries = new LineSeries<double?>
            {
                Values = data,
                GeometryStroke = null,
                GeometryFill = null,
                Fill = null,
                Stroke = new SolidColorPaint
                {
                    Color = SKColors.RoyalBlue,
                    StrokeThickness = 3,
                    ZIndex = 1
                },
                LineSmoothness = 0,
                ZIndex = 1,
            };

            ProbeSeries = new ISeries[]
            {
                LineSeries,
            };
        }

        public void PointerDown(PointerCommandArgs args)
        {
            isDragging = true;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            //LastPointerPosition = lastPointerPosition;
            //ChangeThumbPosition(LastPointerPosition);
        }

        public void PointerMove(PointerCommandArgs args)
        {
            if (!isDragging) return;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            //LastPointerPosition = lastPointerPosition;
            //ChangeThumbPosition(LastPointerPosition);
        }

        public void PointerUp(PointerCommandArgs args)
        {
            isDragging = false;
        }

        public void ChangeThumbPosition(LvcPointD lastPointerPosition)
        {
            // update the scroll bar thumb when the user is dragging the chart
            var numVertLine = 0;
            if (Math.Abs(Thumbs[0].Xi.Value - lastPointerPosition.X) > Math.Abs(Thumbs[1].Xi.Value - lastPointerPosition.X))
            {
                numVertLine = 1;
            }
            Thumbs[numVertLine].Xi = lastPointerPosition.X;
            Thumbs[numVertLine].Xj = lastPointerPosition.X;
        }

        public void CropData()
        {
            Data = Data.Skip(Convert.ToInt32(Thumbs[0].Xi.Value)).ToList();
            Data = Data.Take(Convert.ToInt32(Thumbs[1].Xi.Value)).ToList();

            SetProbeSeriesData(Data);
        }
    }
}
