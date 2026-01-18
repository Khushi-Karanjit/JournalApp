using Microsoft.Extensions.Logging;
using JournalApp.Services;

namespace JournalApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AppDataContext>();
            builder.Services.AddSingleton<MoodCatalog>();
            builder.Services.AddSingleton<TagCatalog>();
            builder.Services.AddSingleton<JournalQueryService>();
            builder.Services.AddSingleton<JournalApp.Services.DiaryEntryService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<JournalDatabaseService>();
            builder.Services.AddSingleton<EntryService>();
            builder.Services.AddSingleton<MoodService>();
            builder.Services.AddSingleton<TagService>();






#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
