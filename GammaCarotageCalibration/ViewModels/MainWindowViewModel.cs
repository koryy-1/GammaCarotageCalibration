using GammaCarotageCalibration.Models;
using GammaCarotageCalibration.Services;
using LiveChartsCore.SkiaSharpView;
using Looch.LasParser;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

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

    public ReactiveCommand<Unit, Unit> OpenLasFileForAlumCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForDuralCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForMagnesCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLasFileForMarbleCommand { get; }

    public ReactiveCommand<Unit, Unit> PlotGraphCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowResultsCommand { get; }

    public MainWindowViewModel()
    {
        _lasFileReader = new LasFileReader();

        OpenLasFileForAlumCommand = ReactiveCommand.CreateFromTask(GetLasDataForAluminum);
        OpenLasFileForDuralCommand = ReactiveCommand.CreateFromTask(GetLasDataForDuralumin);
        OpenLasFileForMagnesCommand = ReactiveCommand.CreateFromTask(GetLasDataForMagnesium);
        OpenLasFileForMarbleCommand = ReactiveCommand.CreateFromTask(GetLasDataForMarble);
        PlotGraphCommand = ReactiveCommand.Create(PlotGraph);
        ShowResultsCommand = ReactiveCommand.Create(ShowResults);

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
        // пусть пока что числа будут выводиться в главном окне
        // тут будут производиться расчеты кэфов
        // а потом строиться график (зависимость отношений б/м к плотности материала)
        throw new NotImplementedException();
    }

    private void PlotGraph()
    {
        // todo: выставить ProbeGraph, в нем будет только кривая
        throw new NotImplementedException();
    }

    // todo: кнопка произвести расчеты
    // и построить апроксимирующий график

    private async Task GetLasDataForAluminum()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData.Add(Materials.Aluminum, lasData);
    }

    private async Task GetLasDataForDuralumin()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData.Add(Materials.Duralumin, lasData);
    }

    private async Task GetLasDataForMagnesium()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData.Add(Materials.Magnesium, lasData);
    }

    private async Task GetLasDataForMarble()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData.Add(Materials.Marble, lasData);
    }
}
