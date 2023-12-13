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
    public double SigmaAl
    {
        get => sigmaAl;
        set => this.RaiseAndSetIfChanged(ref sigmaAl, value);
    }
    private double sigmaDural;
    public double SigmaDural
    {
        get => sigmaDural;
        set => this.RaiseAndSetIfChanged(ref sigmaDural, value);
    }
    private double sigmaMagn;
    public double SigmaMagn
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

    private string coefs;
    public string Coefs
    {
        get => coefs;
        set => this.RaiseAndSetIfChanged(ref coefs, value);
    }

    private string calcSigmas;
    public string CalcSigmas
    {
        get => calcSigmas;
        set => this.RaiseAndSetIfChanged(ref calcSigmas, value);
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

        SigmaAl = 2710;
        SigmaDural = 2850;
        SigmaMagn = 1880;

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

    private void ShowResults()
    {
        // проверка на валидность поля
        if (Alfa1 == 0 || Alfa2 == 0 || Alfa3 == 0)
            return;

        double C = 2;
        double A = Calculator.GetCoefA(Alfa1, Alfa2, C);
        double Q = Calculator.GetCoefQ(Alfa1, Alfa2, C, SigmaAl);

        // нахождение расчетной плотности сигмы
        var calcSigma1 = Calculator.CalcDensityPl(Q, A, C, Alfa1);
        var calcSigma2 = Calculator.CalcDensityPl(Q, A, C, Alfa2);
        var calcSigma3 = Calculator.CalcDensityPl(Q, A, C, Alfa3);

        Coefs = $"Q = {Q}\nA = {A}\nC = {C}\n";
        CalcSigmas = $"calcSigma1 = {calcSigma1}\ncalcSigma2 = {calcSigma2}\ncalcSigma3 = {calcSigma3}\n";

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Alfa1, SigmaAl),
            new ObservablePoint(Alfa2, SigmaDural),
            new ObservablePoint(Alfa3, SigmaMagn)
        };

        // todo: сделать вывод погрешностей в %

        PlotGraph(data);
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

        Coefs = $"A = {A}\nB = {B}\nC = {C}\n";
        CalcSigmas = $"calcSigma1 = {calcSigma1}\ncalcSigma2 = {calcSigma2}\ncalcSigma3 = {calcSigma3}\n";

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(Alfa1, sigma1),
            new ObservablePoint(Alfa2, sigma2),
            new ObservablePoint(Alfa3, sigma3)
        };

        PlotGraph(data);
    }

    private void SolveEquation()
    {

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

    private async Task GetLasDataForAluminum()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Aluminum] = lasData;

        var nearProbeAverage = lasData.Data["RSD"].Average().Value;
        var farProbeAverage = lasData.Data["RLD"].Average().Value;

        Alfa1 = farProbeAverage / nearProbeAverage;
    }

    private async Task GetLasDataForDuralumin()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Duralumin] = lasData;

        var nearProbeAverage = lasData.Data["RSD"].Average().Value;
        var farProbeAverage = lasData.Data["RLD"].Average().Value;

        Alfa2 = farProbeAverage / nearProbeAverage;
    }

    private async Task GetLasDataForMagnesium()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Magnesium] = lasData;

        var nearProbeAverage = lasData.Data["RSD"].Average().Value;
        var farProbeAverage = lasData.Data["RLD"].Average().Value;

        Alfa3 = farProbeAverage / nearProbeAverage;
    }

    private async Task GetLasDataForMarble()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData[Materials.Marble] = lasData;
    }
}
