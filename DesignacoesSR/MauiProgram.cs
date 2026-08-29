using DesignacoesSR.Pages;
using DesignacoesSR.Services;
using Microsoft.Extensions.Logging;

namespace DesignacoesSR
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<CadastroPage>();
            builder.Services.AddSingleton<DesignacoesPage>();
            builder.Services.AddSingleton<HistoricoPage>();
            builder.Services.AddSingleton<ParticipantesPage>();
            builder.Services.AddSingleton<PartesPage>();
            builder.Services.AddSingleton<HabilitacoesPage>();
            builder.Services.AddSingleton<ImportacaoExcelService>();
            builder.Services.AddSingleton<ParteSemanaPage>();
            builder.Services.AddSingleton<ImportarProgramacaoPage>();
            builder.Services.AddSingleton<HttpClient>();

            builder.Services.AddSingleton<JwProgramacaoService>();

            return builder.Build();
        }
    }
}
