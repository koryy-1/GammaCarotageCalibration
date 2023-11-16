using GammaCarotageCalibration.Models;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Kernel.Events;
using System.Reactive;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.Drawing;
using LiveChartsCore.Defaults;

namespace GammaCarotageCalibration.Services
{
    public class GraphService
    {
        // при инициализации будут создаваться 4 объекта Graph
        // которые и будут представлять графики
        // в этом классе будет определятья точка когда заканчивается нагрев и начинается охлаждение

        public ProbeGraph GraphNearProbe { get; set; }
        public ProbeGraph GraphFarProbe { get; set; }
        public ProbeGraph GraphFarToNearProbeRatio { get; set; }

        public RectangularSection[] Thumbs { get; set; }

        public ReactiveCommand<PointerCommandArgs, Unit> PointerDownCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerMoveCommand { get; }
        public ReactiveCommand<PointerCommandArgs, Unit> PointerUpCommand { get; }

        public ReactiveCommand<Unit, Unit> CropDataCommand { get; }

        public LvcPointD LastPointerPosition { get; set; }

        private bool isDragging = false;

        public GraphService((string, string) titles)
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
                }
            };

            GraphNearProbe = new ProbeGraph(titles.Item1);
            GraphFarProbe = new ProbeGraph(titles.Item2);
            GraphFarToNearProbeRatio = new ProbeGraph($"{titles.Item2}/{titles.Item1}");
        }

        public GraphService(GraphData graphData, (string, string) titles)
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
                }
            };

            GraphNearProbe = new ProbeGraph(graphData.NearProbe, titles.Item1);
            GraphFarProbe = new ProbeGraph(graphData.FarProbe, titles.Item2);
            GraphFarToNearProbeRatio = new ProbeGraph(graphData.FarToNearProbeRatio, $"{titles.Item2}/{titles.Item1}");

            PointerDownCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerDown);
            PointerMoveCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerMove);
            PointerUpCommand = ReactiveCommand.Create<PointerCommandArgs>(PointerUp);

            CropDataCommand = ReactiveCommand.Create(CropData);
        }

        public void CropData()
        {
            GraphNearProbe.CropData();
            GraphFarProbe.CropData();
            GraphFarToNearProbeRatio.CropData();
        }

        private void PointerDown(PointerCommandArgs args)
        {
            isDragging = true;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            var pointerX = Math.Round(lastPointerPosition.X);
            var idx = pointerX > GraphNearProbe.Data.Count - 1
                ?
                GraphNearProbe.Data.Count - 1
                :
                pointerX;
            idx = idx < 0 ? 0 : idx;

            lastPointerPosition.X = idx;
            LastPointerPosition = lastPointerPosition;
            ChangeThumbPosition(LastPointerPosition);
        }

        private void PointerMove(PointerCommandArgs args)
        {
            if (!isDragging) return;

            var chart = (ICartesianChartView<SkiaSharpDrawingContext>)args.Chart;
            var lastPointerPosition = chart.ScalePixelsToData(args.PointerPosition);

            var pointerX = Math.Round(lastPointerPosition.X);
            var idx = pointerX > GraphNearProbe.Data.Count - 1
                ?
                GraphNearProbe.Data.Count - 1
                :
                pointerX;
            idx = idx < 0 ? 0 : idx;

            lastPointerPosition.X = idx;
            LastPointerPosition = lastPointerPosition;
            ChangeThumbPosition(LastPointerPosition);
        }

        private void PointerUp(PointerCommandArgs args)
        {
            isDragging = false;
        }

        private void ChangeThumbPosition(LvcPointD lastPointerPosition)
        {
            // update the scroll bar thumb when the user is dragging the chart
            GraphNearProbe.ChangeThumbPosition(lastPointerPosition);
            GraphFarProbe.ChangeThumbPosition(lastPointerPosition);
            GraphFarToNearProbeRatio.ChangeThumbPosition(lastPointerPosition);
        }
    }
}
