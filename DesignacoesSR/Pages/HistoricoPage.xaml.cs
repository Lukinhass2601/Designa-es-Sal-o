using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HistoricoPage : ContentPage
{
    private readonly DatabaseService _database;

    public HistoricoPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var designacoes =
            await _database.GetDesignacoesAsync();

        var historico =
            designacoes.Select(x => new HistoricoItem
            {
                DataSemanaFormatada =
                    x.DataSemana.ToString("dd/MM/yyyy"),

                Parte = x.Parte,

                Participante = x.Participante
            }).ToList();

        listaHistorico.ItemsSource = historico;
    }
}