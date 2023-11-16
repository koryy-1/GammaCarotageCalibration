using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Looch.LasParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GammaCarotageCalibration.Services
{
    public class LasFileReader
    {
        public async Task<LasParser> GetLasData()
        {
            var file = await DoOpenFilePickerAsync();
            if (file is null) return null;

            string filePath = HttpUtility.UrlDecode(file.Path.AbsolutePath);

            var lasData = OpenLasFile(filePath);
            if (lasData is null) return null;

            return lasData;
        }

        private LasParser OpenLasFile(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            LasParser lasParser = new LasParser();
            lasParser.ReadFile(filePath, "windows-1251");

            return lasParser;
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            FilePickerFileType LasFileType = new("Las files")
            {
                Patterns = new[] { "*.las" },
            };

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open Text File",
                FileTypeFilter = new[] { LasFileType },
                AllowMultiple = false
            });

            return files?.Count >= 1 ? files[0] : null;
        }
    }
}
