using Microsoft.Extensions.Logging;
using JournalApp.Services;
using JournalApp.Models;
using PdfSharpCore.Fonts;


namespace JournalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        GlobalFontSettings.FontResolver = new CustomFontResolver();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        builder.Services.AddSingleton<PdfExportService>();
#endif

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<JournalService>();
        builder.Services.AddSingleton<PdfExportService>();

        return builder.Build();
    }
}