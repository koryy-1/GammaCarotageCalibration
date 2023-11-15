using LasAnalyzer.Services;
using Looch.LasParser;
using System.Threading.Tasks;

namespace GammaCarotageCalibration.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    private LasFileReader _lasFileReader;

    public LasParser LasData { get; set; }

    private async Task GetLasData()
    {
        var lasData = await _lasFileReader.GetLasData();
        if (lasData is null)
            return;

        LasData = lasData;

    }
}
