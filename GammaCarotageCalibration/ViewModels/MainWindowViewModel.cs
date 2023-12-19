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

    private double alfaAl;
    public double AlfaAl
    {
        get => alfaAl;
        set => this.RaiseAndSetIfChanged(ref alfaAl, value);
    }

    private double alfaDural;
    public double AlfaDural
    {
        get => alfaDural;
        set => this.RaiseAndSetIfChanged(ref alfaDural, value);
    }

    private double alfaMagn;
    public double AlfaMagn
    {
        get => alfaMagn;
        set => this.RaiseAndSetIfChanged(ref alfaMagn, value);
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
        set
        {
            if (Convert.ToInt32(value.TotalSeconds / 4) < Convert.ToInt32(selectedAccumulationTime.TotalSeconds / 4))
            {
                this.RaiseAndSetIfChanged(ref selectedAccumulationTime, value);
            }
        }
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

        SigmaMagn = 1880;
        SigmaAl = 2710;
        SigmaDural = 2850;

        SelectedAccumulationTime = new TimeSpan(0, 30, 0);

        smallProbe = new double[] { 849, 494, 452 };
        largeProbe = new double[] { 205, 43, 34 };

        AlfaMagn = Math.Round(largeProbe[0] / smallProbe[0], 6);
        AlfaAl = Math.Round(largeProbe[1] / smallProbe[1], 6);
        AlfaDural = Math.Round(largeProbe[2] / smallProbe[2], 6);

        ResultTableAlfa = GetResultTable(AlfaMagn, AlfaAl, AlfaDural);
        ResultTableLargeProbe = GetResultTable(largeProbe[0], largeProbe[1], largeProbe[2]);
        ResultTableSmallProbe = GetResultTable(smallProbe[0], smallProbe[1], smallProbe[2]);

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
            var nearProbeAverage = GetAverageProbeData(LasData[Materials.Magnesium], "RSD");
            var farProbeAverage = GetAverageProbeData(LasData[Materials.Magnesium], "RLD");

            smallProbe[0] = Math.Round(nearProbeAverage, 3);
            largeProbe[0] = Math.Round(farProbeAverage, 3);

            AlfaMagn = Math.Round(farProbeAverage / nearProbeAverage, 3);
        }

        // Aluminum
        if (LasData[Materials.Aluminum] is not null)
        {
            var nearProbeAverage = GetAverageProbeData(LasData[Materials.Aluminum], "RSD");
            var farProbeAverage = GetAverageProbeData(LasData[Materials.Aluminum], "RLD");

            smallProbe[1] = Math.Round(nearProbeAverage, 3);
            largeProbe[1] = Math.Round(farProbeAverage, 3);

            AlfaAl = Math.Round(farProbeAverage / nearProbeAverage, 3);
        }

        // Duralumin
        if (LasData[Materials.Duralumin] is not null)
        {
            var nearProbeAverage = GetAverageProbeData(LasData[Materials.Duralumin], "RSD");
            var farProbeAverage = GetAverageProbeData(LasData[Materials.Duralumin], "RLD");

            smallProbe[2] = Math.Round(nearProbeAverage, 3);
            largeProbe[2] = Math.Round(farProbeAverage, 3);

            AlfaDural = Math.Round(farProbeAverage / nearProbeAverage, 3);
        }
    }
    private void ShowResults()
    {
        // проверка на валидность
        if (smallProbe.Length == 0 || largeProbe.Length == 0)
            return;

        GetCurrentProbeMetrics();

        ResultTableAlfa = GetResultTable(AlfaMagn, AlfaAl, AlfaDural);
        ResultTableLargeProbe = GetResultTable(largeProbe[0], largeProbe[1], largeProbe[2]);
        ResultTableSmallProbe = GetResultTable(smallProbe[0], smallProbe[1], smallProbe[2]);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(AlfaDural, SigmaDural),
            new ObservablePoint(AlfaAl, SigmaAl),
            new ObservablePoint(AlfaMagn, SigmaMagn),
        };

        PlotGraph(data);
    }

    private ObservableCollection<Report> GetResultTable(double alfaMagn, double alfaAl, double alfaDural)
    {
        // todo: вывод кэфов на ГУИ
        double C = 2;
        double A = Calculator.GetCoefA(alfaMagn, alfaAl, C);
        double Q = Calculator.GetCoefQ(alfaMagn, alfaAl, C, SigmaMagn);

        // нахождение расчетной плотности сигмы
        var calcSigmaMagn = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaMagn), 3);
        var calcSigmaAl = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaAl), 3);
        var calcSigmaDural = Math.Round(Calculator.CalcDensityPl(Q, A, C, alfaDural), 3);

        var errorMagn = Math.Round((SigmaMagn - calcSigmaMagn) / SigmaMagn * 100, 3);
        var errorAl = Math.Round((SigmaAl - calcSigmaAl) / SigmaAl * 100, 3);
        var errorDural = Math.Round((SigmaDural - calcSigmaDural) / SigmaDural * 100, 3);

        ObservableCollection<Report> table = new ObservableCollection<Report>()
        {
            new Report(
                SigmaMagn,
                alfaMagn,
                calcSigmaMagn,
                errorMagn
            ),
            new Report(
                SigmaAl,
                alfaAl,
                calcSigmaAl,
                errorAl
            ),
            new Report(
                SigmaDural,
                alfaDural,
                calcSigmaDural,
                errorDural
            ),
        };

        return table;
    }

    private void OldShowResults()
    {
        // проверка на валидность поля
        if (AlfaAl == 0 || AlfaDural == 0 || AlfaMagn == 0)
            return;

        // расчет А В С
        double sigma1 = 1880;
        double sigma2 = 2710;
        double sigma3 = 2850;

        var A = Calculator.GetCoefA(
            AlfaAl, AlfaDural, AlfaMagn,
            sigma1, sigma2, sigma3
        );
        var B = Calculator.GetCoefB(
            AlfaAl, AlfaDural, AlfaMagn,
            sigma1, sigma2, sigma3
        );
        var C = Calculator.GetCoefC(
            AlfaAl, AlfaDural, AlfaMagn,
            sigma1, sigma2, sigma3
        );

        // нахождение расчетной плотности сигмы
        var calcSigma1 = Calculator.CalculateDensity(A, B, C, AlfaAl);
        var calcSigma2 = Calculator.CalculateDensity(A, B, C, AlfaDural);
        var calcSigma3 = Calculator.CalculateDensity(A, B, C, AlfaMagn);

        ObservableCollection<ObservablePoint> data = new ObservableCollection<ObservablePoint>
        {
            new ObservablePoint(AlfaAl, sigma1),
            new ObservablePoint(AlfaDural, sigma2),
            new ObservablePoint(AlfaMagn, sigma3)
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
