using Avalonia.Controls.Primitives;
using LasAnalyzer.Models;
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

namespace LasAnalyzer.Services.Graphics
{
    public class ProbeGraph : ReactiveObject, IGraph
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

        // Points
        public ExtremumPoints HeatingExtremumPoints { get; set; }
        public ExtremumPoints CoolingExtremumPoints { get; set; }

        public RectangularSection[] Thumbs { get; set; }

        public ObservablePoint NearlyExtrema { get; set; }

        public ReactiveCommand<PointerCommandArgs, Unit> PointerDownCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerMoveCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerUpCommand { get; }

        public bool IsEnabledMovementChart { get; set; } = false;
        public bool IsEnabledMovementVertLines { get; set; } = true;
        public bool IsEnabledMovementPoints { get; set; } = false;

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

        public ProbeGraph(List<double?> data, string title, int coolingStartIndex, int baseHeatIndex, int baseCoolIndex)
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

            HeatingExtremumPoints = new ExtremumPoints();
            CoolingExtremumPoints = new ExtremumPoints();

            SetProbeSeriesData(data, coolingStartIndex, baseHeatIndex, baseCoolIndex);

            PointerDownCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerDown);
            PointerMoveCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerMove);
            PointerUpCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerUp);
        }

        private void SetProbeSeriesData(List<double?> data, int transitionIndex, int baseHeatIndex, int baseCoolIndex)
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

            // продумать
            HeatingExtremumPoints = FindExtremum(
                transitionIndex == -1 ? data : data.Take(transitionIndex).ToList(),
                TempType.Heating,
                baseHeatIndex,
                transitionIndex
            );
            CoolingExtremumPoints = FindExtremum(
                transitionIndex == -1 ? data : data.Skip(transitionIndex).ToList(),
                TempType.Cooling,
                transitionIndex == -1 ? baseCoolIndex : baseCoolIndex - transitionIndex,
                transitionIndex
            );
            // todo: use 6 sckatter series with 1 ObservablePoint
            // мб использовать пункирную линию для отрисовки минимума
            // использовать более темные цвета для отрисовки базовой точки
            // mb use VisualElement instead ScatterSeries?
            ScatterSeries = new ScatterSeries<ObservablePoint>
            {
                Values = new ObservableCollection<ObservablePoint>
                {
                    HeatingExtremumPoints.BasePoint,
                    CoolingExtremumPoints.BasePoint,
                    HeatingExtremumPoints.MaxPoint,
                    HeatingExtremumPoints.MinPoint,
                    CoolingExtremumPoints.MaxPoint,
                    CoolingExtremumPoints.MinPoint,
                },
                Fill = new SolidColorPaint(new SKColor(229, 57, 53, 100)),
                Stroke = new SolidColorPaint
                {
                    Color = SKColors.Red,
                    StrokeThickness = 3,
                    ZIndex = 1
                },
                GeometrySize = 10,
            };

            ProbeSeries = new ISeries[]
            {
                LineSeries,
                ScatterSeries
            };
        }

        private double GetDistanceToPointer(ObservablePoint point, LvcPointD lastPointerPosition)
        {
            double dx = point.X.Value - lastPointerPosition.X;
            double dy = point.Y.Value - lastPointerPosition.Y;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance;
        }

        public ObservablePoint GetNearlyExtrema(LvcPointD lastPointerPosition)
        {
            NearlyExtrema = ScatterSeries.Values.Skip(2).Where(point => point != null).OrderBy(point => GetDistanceToPointer(point, lastPointerPosition)).First();

            return NearlyExtrema;
        }

        public void PointerDown(PointerCommandArgs args)
        {
            // при наведении на точку она чуть увеличивается, за нее можно схватиться ЛКМ
            // мб флаг определяющий что конкретная точка схвачена

            isDragging = true;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            if (IsEnabledMovementVertLines)
            {
                //LastPointerPosition = lastPointerPosition;
                //ChangeThumbPosition(LastPointerPosition);
            }

            //if (IsEnabledMovementPoints)
            //{
            //    var dist = FindExtremaNearPointer(lastPointerPosition);
            //}
        }

        public void PointerMove(PointerCommandArgs args)
        {
            if (!isDragging) return;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            if (IsEnabledMovementVertLines)
            {
                //LastPointerPosition = lastPointerPosition;
                //ChangeThumbPosition(LastPointerPosition);
            }
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

        public void CropData(int coolingStartIndex, int baseHeatIndex, int baseCoolIndex)
        {
            Data = Data.Skip(Convert.ToInt32(Thumbs[0].Xi.Value)).ToList();
            Data = Data.Take(Convert.ToInt32(Thumbs[1].Xi.Value)).ToList();

            SetProbeSeriesData(Data, coolingStartIndex, baseHeatIndex, baseCoolIndex);
        }



        // points for calc
        private ExtremumPoints FindExtremum(List<double?> data, TempType tempType, int baseIndex, int coolingStartIndex)
        {
            if (baseIndex == -1)
                return new ExtremumPoints();

            ExtremumPoints ExtremumPoints = new ExtremumPoints();

            ExtremumPoints.BasePoint = FindBaseValues(data, tempType, baseIndex);
            ExtremumPoints.MaxPoint = FindMaxExtremum(data, ExtremumPoints.BasePoint);
            ExtremumPoints.MinPoint = FindMinExtremum(data, ExtremumPoints.BasePoint);

            // todo: fix this shit
            if (tempType == TempType.Cooling && coolingStartIndex != -1)
            {
                ExtremumPoints.BasePoint.X += coolingStartIndex;
                ExtremumPoints.MaxPoint.X += coolingStartIndex;
                ExtremumPoints.MinPoint.X += coolingStartIndex;
            }

            return ExtremumPoints;
        }

        public ObservablePoint FindBaseValues(List<double?> data, TempType tempType, int baseIndex)
        {
            var basePoint = new ObservablePoint();
            basePoint.X = baseIndex;

            if (tempType == TempType.Heating)
            {
                baseIndex = baseIndex == 0 ? 1 : baseIndex;
                basePoint.Y = data.Take(baseIndex).Average();
            }
            else if (tempType == TempType.Cooling)
            {
                baseIndex = baseIndex == data.Count - 1 ? data.Count - 2 : baseIndex;
                basePoint.Y = data.Skip(baseIndex).Average();
            }

            return basePoint;
        }

        private ObservablePoint FindMaxExtremum(List<double?> data, ObservablePoint basePoint)
        {
            var maxPoint = new ObservablePoint();
            var maxIdx = data.IndexOf(data.Max());
            var max = data[maxIdx];

            if (IsExceedThreshold(max, basePoint.Y.Value))
            {
                maxPoint.X = maxIdx;
                maxPoint.Y = max;
            }
            else
            {
                maxPoint.X = basePoint.X.Value;
                maxPoint.Y = basePoint.Y.Value;
            }

            return maxPoint;
        }

        private ObservablePoint FindMinExtremum(List<double?> data, ObservablePoint basePoint)
        {
            var minPoint = new ObservablePoint();
            var minIdx = data.IndexOf(data.Min());
            var min = data[minIdx];

            if (IsExceedThreshold(min, basePoint.Y.Value))
            {
                minPoint.X = minIdx;
                minPoint.Y = min;
            }
            else
            {
                minPoint.X = basePoint.X.Value;
                minPoint.Y = basePoint.Y.Value;
            }

            return minPoint;
        }

        private bool IsExceedThreshold(double? extrema, double baseValue, double threshold = 0.005)
        {
            return Math.Abs((extrema.Value - baseValue) / baseValue) > threshold;
        }
    }
}
