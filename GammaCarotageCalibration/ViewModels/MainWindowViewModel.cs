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

    private double sigmaAl;
    public double Sigma1
    {
        get => sigmaAl;
        set => this.RaiseAndSetIfChanged(ref sigmaAl, value);
    }
    private double sigmaDural;
    public double Sigma2
    {
        get => sigmaDural;
        set => this.RaiseAndSetIfChanged(ref sigmaDural, value);
    }
    private double sigmaMagn;
    public double Sigma3
    {
        get => sigmaMagn;
        set => this.RaiseAndSetIfChanged(ref sigmaMagn, value);
    }

    private double alfa1;
    public double Alfa1
    {
        get => alfa1;
        set => this.RaiseAndSetIfChanged(ref alfa1, value);
    }

    private double alfa2;
    public double Alfa2
    {
        get => alfa2;
        set => this.RaiseAndSetIfChanged(ref alfa2, value);
    }

    private double alfa3;
    public double Alfa3
    {
        get => alfa3;
        set => this.RaiseAndSetIfChanged(ref alfa3, value);
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

    private double[] largeProbe;
    private double[] smallProbe;

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

        Sigma1 = 1880;
        Sigma2 = 2710;
        Sigma3 = 2850;

        SelectedAccumulationTime = new TimeSpan(0, 30, 0);

        smallProbe = new double[] { 849, 494, 452 };
        largeProbe = new double[] { 205, 43, 34 };

        Alfa1 = Math.Round(largeProbe[0] / smallProbe[0], 6);
        Alfa2 = Math.Round(largeProbe[1] / smallProbe[1], 6);
        Alfa3 = Math.Round(largeProbe[2] / smallProbe[2], 6);

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
    private void ShowResults()
    {
        // проверка на валидность
        if (smallProbe.Length == 0 || largeProbe.Length == 0)
            return;

        // todo: урезать по времени накопления здесь

        ResultTableAlfa = GetResultTable(Alfa1, Alfa2, Alfa3);
        ResultTableLargeProbe = GetResultTable(largeProbe[0], largeProbe[1], largeProbe[2]);
        ResultTableSmallProbe = GetResultTable(smallProbe[0], smallProbe[1], smallProbe[2]);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Alfa1, Sigma1),
            new ObservablePoint(Alfa2, Sigma2),
            new ObservablePoint(Alfa3, Sigma3)
        };

        PlotGraph(data);
    }

    private ObservableCollection<Report> GetResultTable(double alfa1, double alfa2, double alfa3)
    {
        //SelectedTime.TotalSeconds
        double C = 2;
        double A = Calculator.GetCoefA(alfa1, alfa2, C);
        double Q = Calculator.GetCoefQ(alfa1, alfa2, C, Sigma1);

        // нахождение расчетной плотности сигмы
        var calcSigma1 = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfa1), 3);
        var calcSigma2 = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfa2), 3);
        var calcSigma3 = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfa3), 3);

        var error1 = Math.Round((Sigma1 - calcSigma1) / Sigma1 * 100, 3);
        var error2 = Math.Round((Sigma2 - calcSigma2) / Sigma2 * 100, 3);
        var error3 = Math.Round((Sigma3 - calcSigma3) / Sigma3 * 100, 3);

        ObservableCollection<Report> table = new ObservableCollection<Report>()
        {
            new Report(
                Sigma1,
                alfa1,
                calcSigma1,
                error1
            ),
            new Report(
                Sigma2,
                alfa2,
                calcSigma2,
                error2
            ),
            new Report(
                Sigma3,
                alfa3,
                calcSigma3,
                error3
            ),
        };

        return table;
    }

    private void OldShowResults()
    {
        // проверка на валидность поля
        if (Alfa1 == 0 || Alfa2 == 0 || Alfa3 == 0)
            return;

        // расчет А В С
        double sigma1 = 1880;
        double sigma2 = 2710;
        double sigma3 = 2850;

        var A = Calculator.GetCoefA(
            Alfa1, Alfa2, Alfa3,
            sigma1, sigma2, sigma3
        );
        var B = Calculator.GetCoefB(
            Alfa1, Alfa2, Alfa3,
            sigma1, sigma2, sigma3
        );
        var C = Calculator.GetCoefC(
            Alfa1, Alfa2, Alfa3,
            sigma1, sigma2, sigma3
        );

        // нахождение расчетной плотности сигмы
        var calcSigma1 = Calculator.CalculateDensity(A, B, C, Alfa1);
        var calcSigma2 = Calculator.CalculateDensity(A, B, C, Alfa2);
        var calcSigma3 = Calculator.CalculateDensity(A, B, C, Alfa3);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Alfa1, sigma1),
            new ObservablePoint(Alfa2, sigma2),
            new ObservablePoint(Alfa3, sigma3)
        };

        PlotGraph(data);
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

    // todo: кнопка произвести расчеты
    // и построить апроксимирующий график

    // алгоритм следующий
    // сначала вычисляем альфу для воды
    // потом вычисляем альфы для 3-х материалов
    // потом строим график эталонных значений плотности от альфы для каждого материала
    // вычисляем кэфы А В и С
    // далее с помощью метода Calculator.CalculateDensity вычисляем градуировочную хар-ку (расч плотность)
    // вопрос, если альфа для 1 материала в качестве аргумента выдает результат = эталон знач плотности,
    // то значит в формулу нужно подставлять альфу для воды (мрамора)?

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

        var nearProbeAverage = GetAverageProbeData(lasData, "RSD");
        var farProbeAverage = GetAverageProbeData(lasData, "RLD");

        smallProbe[0] = Math.Round(nearProbeAverage, 3);
        largeProbe[0] = Math.Round(farProbeAverage, 3);

        Alfa1 = Math.Round(farProbeAverage / nearProbeAverage, 3);
    }

    private async Task GetLasDataForDuralumin()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Duralumin] = lasData;

        var nearProbeAverage = GetAverageProbeData(lasData, "RSD");
        var farProbeAverage = GetAverageProbeData(lasData, "RLD");

        smallProbe[1] = Math.Round(nearProbeAverage, 3);
        largeProbe[1] = Math.Round(farProbeAverage, 3);

        Alfa2 = Math.Round(farProbeAverage / nearProbeAverage, 3);
    }

    private async Task GetLasDataForMagnesium()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Magnesium] = lasData;

        var nearProbeAverage = GetAverageProbeData(lasData, "RSD");
        var farProbeAverage = GetAverageProbeData(lasData, "RLD");

        smallProbe[2] = Math.Round(nearProbeAverage, 3);
        largeProbe[2] = Math.Round(farProbeAverage, 3);

        Alfa3 = Math.Round(farProbeAverage / nearProbeAverage, 3);
    }

    private async Task GetLasDataForMarble()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Marble] = lasData;
    }
}
