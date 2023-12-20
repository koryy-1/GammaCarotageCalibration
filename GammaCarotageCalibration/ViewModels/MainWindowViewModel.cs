using GammaCarotageCalibration.Models;
using GammaCarotageCalibration.Services;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Looch.LasParser;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System.Linq;
using LiveChartsCore.Geo;
using System.Globalization;
using DynamicData;

namespace GammaCarotageCalibration.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private LasFileReader _lasFileReader;

    public Dictionary<Materials, LasParser> LasData { get; set; }

    private Axis[] yAxis;
    public Axis[] YAxis
    {
        get => yAxis;
        set => this.RaiseAndSetIfChanged(ref yAxis, value);
    }

    private ProbeGraph _probeGraph;
    public ProbeGraph ProbeGraph
    {
        get => _probeGraph;
        set => this.RaiseAndSetIfChanged(ref _probeGraph, value);
    }

    private ISeries[] _probeSeries;
    public ISeries[] ProbeSeries
    {
        get => _probeSeries;
        set => this.RaiseAndSetIfChanged(ref _probeSeries, value);
    }

    private ObservableCollection<Report> resultTableSmallProbe;
    public ObservableCollection<Report> ResultTableSmallProbe
    {
        get => resultTableSmallProbe;
        set => this.RaiseAndSetIfChanged(ref resultTableSmallProbe, value);
    }

    private ObservableCollection<Report> resultTableLargeProbe;
    public ObservableCollection<Report> ResultTableLargeProbe
    {
        get => resultTableLargeProbe;
        set => this.RaiseAndSetIfChanged(ref resultTableLargeProbe, value);
    }

    private ObservableCollection<Report> resultTableAlfa;
    public ObservableCollection<Report> ResultTableAlfa
    {
        get => resultTableAlfa;
        set => this.RaiseAndSetIfChanged(ref resultTableAlfa, value);
    }

    private TimeSpan selectedAccumulationTime;
    public TimeSpan SelectedAccumulationTime
    {
        get => selectedAccumulationTime;
        set => this.RaiseAndSetIfChanged(ref selectedAccumulationTime, value);
    }

    private int countOfSamples;

    public IMaterial Aluminum { get; set; }
    public IMaterial Duralumin { get; set; }
    public IMaterial Magnesium { get; set; }

    public ReactiveCommand<Unit, Unit> OpenLasFileForAlumCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForDuralCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForMagnesCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForMarbleCommand { get; }

    public ReactiveCommand<Unit, Unit> PlotGraphCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowResultsCommand { get; }

    public MainWindowViewModel()
    {
        _lasFileReader = new LasFileReader();

        LasData = new Dictionary<Materials, LasParser>
        {
            { Materials.Aluminum, null },
            { Materials.Duralumin, null },
            { Materials.Magnesium, null },
            { Materials.Marble, null }
        };

        Magnesium = new Magnesium(1880, 849, 205);
        Aluminum = new Aluminum(2710, 494, 43);
        Duralumin = new Duralumin(2850, 452, 34);

        SelectedAccumulationTime = new TimeSpan(0, 30, 0);

        ResultTableAlfa = GetResultTable(Magnesium.ProbeMetrics.Alfa, Aluminum.ProbeMetrics.Alfa, Duralumin.ProbeMetrics.Alfa);
        ResultTableLargeProbe = GetResultTable(Magnesium.ProbeMetrics.AverageLargeProbe, Aluminum.ProbeMetrics.AverageLargeProbe, Duralumin.ProbeMetrics.AverageLargeProbe);
        ResultTableSmallProbe = GetResultTable(Magnesium.ProbeMetrics.AverageSmallProbe, Aluminum.ProbeMetrics.AverageSmallProbe, Duralumin.ProbeMetrics.AverageSmallProbe);

        OpenLasFileForAlumCommand = ReactiveCommand.CreateFromTask(GetLasDataForAluminum);
        OpenLasFileForDuralCommand = ReactiveCommand.CreateFromTask(GetLasDataForDuralumin);
        OpenLasFileForMagnesCommand = ReactiveCommand.CreateFromTask(GetLasDataForMagnesium);
        OpenLasFileForMarbleCommand = ReactiveCommand.CreateFromTask(GetLasDataForMarble);
        //PlotGraphCommand = ReactiveCommand.Create(PlotGraph);
        ShowResultsCommand = ReactiveCommand.Create(ShowResults);

        var LineSeries = new LineSeries<ObservablePoint>();

        ProbeSeries = new ISeries[]
        {
            LineSeries,
        };

        YAxis = new[]
        {
            new Axis
            {
                //MaxLimit = LasDataForGamma.NearProbe.Max() * 1.1,
                //MinLimit = LasDataForGamma.NearProbe.Min() * 0.9
            }
        };
    }

    // todo: сделать поля для большого и малого зондов рядом с полями для плотностей
    // и поля для их отношений
    private void GetCurrentProbeMetrics()
    {
        // Magnesium
        if (LasData[Materials.Magnesium] is not null)
        {
            Magnesium.ProbeMetrics = new ProbeMetrics(
                LasData[Materials.Magnesium].Data["RSD"],
                LasData[Materials.Magnesium].Data["RSD"],
                SelectedAccumulationTime
            );

            Magnesium.ProbeMetrics.CountOfSamples = LasData[Materials.Magnesium].Data["TIME"].Length;
            if (Magnesium.ProbeMetrics.CountOfSamples < countOfSamples)
            {
                countOfSamples = Magnesium.ProbeMetrics.CountOfSamples;
            }
        }

        // Aluminum
        if (LasData[Materials.Aluminum] is not null)
        {
            var nearProbeAverage = GetAverageProbeData(LasData[Materials.Aluminum], "RSD");
            var farProbeAverage = GetAverageProbeData(LasData[Materials.Aluminum], "RLD");

            Aluminum.ProbeMetrics.AverageSmallProbe = Math.Round(nearProbeAverage, 3);
            Aluminum.ProbeMetrics.AverageLargeProbe = Math.Round(farProbeAverage, 3);

            Aluminum.ProbeMetrics.Alfa = Math.Round(farProbeAverage / nearProbeAverage, 3);

            Aluminum.ProbeMetrics.CountOfSamples = LasData[Materials.Aluminum].Data["TIME"].Length;
            if (Aluminum.ProbeMetrics.CountOfSamples < countOfSamples)
            {
                countOfSamples = Aluminum.ProbeMetrics.CountOfSamples;
            }
        }

        // Duralumin
        if (LasData[Materials.Duralumin] is not null)
        {
            var nearProbeAverage = GetAverageProbeData(LasData[Materials.Duralumin], "RSD");
            var farProbeAverage = GetAverageProbeData(LasData[Materials.Duralumin], "RLD");

            Duralumin.ProbeMetrics.AverageSmallProbe = Math.Round(nearProbeAverage, 3);
            Duralumin.ProbeMetrics.AverageLargeProbe = Math.Round(farProbeAverage, 3);

            Duralumin.ProbeMetrics.Alfa = Math.Round(farProbeAverage / nearProbeAverage, 3);

            Duralumin.ProbeMetrics.CountOfSamples = LasData[Materials.Duralumin].Data["TIME"].Length;
            if (Duralumin.ProbeMetrics.CountOfSamples < countOfSamples)
            {
                countOfSamples = Duralumin.ProbeMetrics.CountOfSamples;
            }
        }
    }
    private void ShowResults()
    {
        // проверка на валидность
        if (Magnesium is null || Aluminum is null || Duralumin is null)
            return;

        GetCurrentProbeMetrics();

        ResultTableAlfa = GetResultTable(Magnesium.ProbeMetrics.Alfa, Aluminum.ProbeMetrics.Alfa, Duralumin.ProbeMetrics.Alfa);
        ResultTableLargeProbe = GetResultTable(Magnesium.ProbeMetrics.AverageLargeProbe, Aluminum.ProbeMetrics.AverageLargeProbe, Duralumin.ProbeMetrics.AverageLargeProbe);
        ResultTableSmallProbe = GetResultTable(Magnesium.ProbeMetrics.AverageSmallProbe, Aluminum.ProbeMetrics.AverageSmallProbe, Duralumin.ProbeMetrics.AverageSmallProbe);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Duralumin.ProbeMetrics.Alfa, Duralumin.Sigma),
            new ObservablePoint(Aluminum.ProbeMetrics.Alfa, Aluminum.Sigma),
            new ObservablePoint(Magnesium.ProbeMetrics.Alfa, Magnesium.Sigma),
        };

        PlotGraph(data);
    }

    private ObservableCollection<Report> GetResultTable(double alfaMagn, double alfaAl, double alfaDural)
    {
        // todo: вывод кэфов на ГУИ
        double C = 2;
        double A = Calculator.GetCoefA(alfaMagn, alfaAl, C);
        double Q = Calculator.GetCoefQ(alfaMagn, alfaAl, C, Magnesium.Sigma);

        // нахождение расчетной плотности сигмы
        var calcSigmaMagn = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaMagn), 3);
        var calcSigmaAl = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaAl), 3);
        var calcSigmaDural = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaDural), 3);

        var errorMagn = Math.Round((Magnesium.Sigma - calcSigmaMagn) / Magnesium.Sigma * 100, 3);
        var errorAl = Math.Round((Aluminum.Sigma - calcSigmaAl) / Aluminum.Sigma * 100, 3);
        var errorDural = Math.Round((Duralumin.Sigma - calcSigmaDural) / Duralumin.Sigma * 100, 3);

        ObservableCollection<Report> table = new ObservableCollection<Report>()
        {
            new Report(
                Magnesium.Sigma,
                alfaMagn,
                calcSigmaMagn,
                errorMagn
            ),
            new Report(
                Aluminum.Sigma,
                alfaAl,
                calcSigmaAl,
                errorAl
            ),
            new Report(
                Duralumin.Sigma,
                alfaDural,
                calcSigmaDural,
                errorDural
            ),
        };

        return table;
    }

    private void PlotGraph(ObservableCollection<ObservablePoint> data)
    {
        var LineSeries = new LineSeries<ObservablePoint>
        {
            Values = data,
            //GeometryStroke = null,
            //GeometryFill = null,
            //Fill = null,
            Stroke = new SolidColorPaint
            {
                Color = SKColors.RoyalBlue,
                StrokeThickness = 3,
            },
            LineSmoothness = 0,
        };

        ProbeSeries = new ISeries[]
        {
            LineSeries,
        };
    }

    private double GetAverageProbeData(LasParser lasData, string probeName)
    {
        int countOfSamples = Convert.ToInt32(SelectedAccumulationTime.TotalSeconds / 4);
        return lasData.Data[probeName].Take(countOfSamples).Average().Value;
    }

    private async Task GetLasDataForAluminum()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Aluminum] = lasData;
    }

    private async Task GetLasDataForDuralumin()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Duralumin] = lasData;
    }

    private async Task GetLasDataForMagnesium()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Magnesium] = lasData;
    }

    private async Task GetLasDataForMarble()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Marble] = lasData;
    }
}
