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

    private ObservableCollection<Coefficients> coefTable;
    public ObservableCollection<Coefficients> CoefTable
    {
        get => coefTable;
        set => this.RaiseAndSetIfChanged(ref coefTable, value);
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

    private string metaDataAl;
    public string MetaDataAl
    {
        get => metaDataAl;
        set => this.RaiseAndSetIfChanged(ref metaDataAl, value);
    }

    private string metaDataDural;
    public string MetaDataDural
    {
        get => metaDataDural;
        set => this.RaiseAndSetIfChanged(ref metaDataDural, value);
    }

    private string metaDataMagn;
    public string MetaDataMagn
    {
        get => metaDataMagn;
        set => this.RaiseAndSetIfChanged(ref metaDataMagn, value);
    }

    private string metaDataMarble;
    public string MetaDataMarble
    {
        get => metaDataMarble;
        set => this.RaiseAndSetIfChanged(ref metaDataMarble, value);
    }

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

        CoefTable = GetCoefs(Magnesium.ProbeMetrics, Aluminum.ProbeMetrics, Duralumin.ProbeMetrics);

        ResultTableAlfa = GetResultTable(CoefTable[0], Magnesium.ProbeMetrics.Alfa, Aluminum.ProbeMetrics.Alfa, Duralumin.ProbeMetrics.Alfa);
        ResultTableLargeProbe = GetResultTable(CoefTable[1], Magnesium.ProbeMetrics.AverageLargeProbe, Aluminum.ProbeMetrics.AverageLargeProbe, Duralumin.ProbeMetrics.AverageLargeProbe);
        ResultTableSmallProbe = GetResultTable(CoefTable[2], Magnesium.ProbeMetrics.AverageSmallProbe, Aluminum.ProbeMetrics.AverageSmallProbe, Duralumin.ProbeMetrics.AverageSmallProbe);

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

        CoefTable = GetCoefs(Magnesium.ProbeMetrics, Aluminum.ProbeMetrics, Duralumin.ProbeMetrics);

        ResultTableAlfa = GetResultTable(CoefTable[0], Magnesium.ProbeMetrics.Alfa, Aluminum.ProbeMetrics.Alfa, Duralumin.ProbeMetrics.Alfa);
        ResultTableLargeProbe = GetResultTable(CoefTable[1], Magnesium.ProbeMetrics.AverageLargeProbe, Aluminum.ProbeMetrics.AverageLargeProbe, Duralumin.ProbeMetrics.AverageLargeProbe);
        ResultTableSmallProbe = GetResultTable(CoefTable[2], Magnesium.ProbeMetrics.AverageSmallProbe, Aluminum.ProbeMetrics.AverageSmallProbe, Duralumin.ProbeMetrics.AverageSmallProbe);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Duralumin.ProbeMetrics.Alfa, Duralumin.Sigma),
            new ObservablePoint(Aluminum.ProbeMetrics.Alfa, Aluminum.Sigma),
            new ObservablePoint(Magnesium.ProbeMetrics.Alfa, Magnesium.Sigma),
        };

        PlotGraph(data);
    }

    private ObservableCollection<Coefficients> GetCoefs(ProbeMetrics magnesium, ProbeMetrics aluminum, ProbeMetrics duralumin)
    {
        double C = 2;

        ObservableCollection<Coefficients> table = new ObservableCollection<Coefficients>()
        {
            new Coefficients {
                Description = "Отн Б/М",
                Q = Math.Round(Calculator.GetCoefQ(magnesium.Alfa, aluminum.Alfa, C, Magnesium.Sigma), 1),
                A = Math.Round(Calculator.GetCoefA(magnesium.Alfa, aluminum.Alfa, C), 1),
                C = C,
            },
            new Coefficients {
                Description = "Для Б зонда",
                Q = Math.Round(Calculator.GetCoefQ(magnesium.AverageLargeProbe, aluminum.AverageLargeProbe, C, Magnesium.Sigma), 1),
                A = Math.Round(Calculator.GetCoefA(magnesium.AverageLargeProbe, aluminum.AverageLargeProbe, C), 1),
                C = C,
            },
            new Coefficients {
                Description = "Для М зонда",
                Q = Math.Round(Calculator.GetCoefQ(magnesium.AverageSmallProbe, aluminum.AverageSmallProbe, C, Magnesium.Sigma), 1),
                A = Math.Round(Calculator.GetCoefA(magnesium.AverageSmallProbe, aluminum.AverageSmallProbe, C), 1),
                C = C,
            },
        };

        return table;
    }

    private ObservableCollection<Report> GetResultTable(Coefficients coefficients, double alfaMagn, double alfaAl, double alfaDural)
    {
        // нахождение расчетной плотности сигмы
        var calcSigmaMagn = Calculator.CalcDensityPl(coefficients.Q, coefficients.A, coefficients.C, alfaMagn);
        var calcSigmaAl = Calculator.CalcDensityPl(coefficients.Q, coefficients.A, coefficients.C, alfaAl);
        var calcSigmaDural = Calculator.CalcDensityPl(coefficients.Q, coefficients.A, coefficients.C, alfaDural);

        var errorMagn = (Magnesium.Sigma - calcSigmaMagn) / Magnesium.Sigma * 100;
        var errorAl = (Aluminum.Sigma - calcSigmaAl) / Aluminum.Sigma * 100;
        var errorDural = (Duralumin.Sigma - calcSigmaDural) / Duralumin.Sigma * 100;

        ObservableCollection<Report> table = new ObservableCollection<Report>()
        {
            new Report(
                Magnesium.Sigma,
                alfaMagn.ToString("F3"),
                calcSigmaMagn.ToString("F3"),
                errorMagn.ToString("F3")
            ),
            new Report(
                Aluminum.Sigma,
                alfaAl.ToString("F3"),
                calcSigmaAl.ToString("F3"),
                errorAl.ToString("F3")
            ),
            new Report(
                Duralumin.Sigma,
                alfaDural.ToString("F3"),
                calcSigmaDural.ToString("F3"),
                errorDural.ToString("F3")
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

    private string GetDateAndSerialNum(LasParser lasData)
    {
        var dateIdx = lasData.Wmnem.IndexOf("DATE");
        var date = dateIdx != -1 ? lasData.Wvalue[dateIdx].Replace("/", ".") : string.Empty;
        var serialNumberIdx = lasData.Wmnem.IndexOf("SNUM");
        var serialNumber = serialNumberIdx != -1 ? lasData.Wvalue[serialNumberIdx] : string.Empty;
        return $"Дата: {date}\nНомер прибора: {serialNumber}";
    }

    private async Task GetLasDataForAluminum()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Aluminum] = lasData;

        MetaDataAl = GetDateAndSerialNum(lasData);
    }

    private async Task GetLasDataForDuralumin()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Duralumin] = lasData;

        MetaDataDural = GetDateAndSerialNum(lasData);
    }

    private async Task GetLasDataForMagnesium()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Magnesium] = lasData;

        MetaDataMagn = GetDateAndSerialNum(lasData);
    }

    private async Task GetLasDataForMarble()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Marble] = lasData;

        MetaDataMarble = GetDateAndSerialNum(lasData);
    }
}
