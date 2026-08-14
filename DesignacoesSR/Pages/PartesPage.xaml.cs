using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class PartesPage : ContentPage
{
    private readonly DatabaseService _database;

    public PartesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        listaPartes.ItemsSource =
            await _database.GetPartesAsync();
    }

    private async void EditarParte_Clicked(
        object sender,
        EventArgs e)
    {
        var button = (Button)sender;

        var parte =
            (Parte)button.CommandParameter;

        string novoNome =
            await DisplayPromptAsync(
                "Editar Parte",
                "Digite o novo nome:",
                initialValue: parte.Nome);

        if (string.IsNullOrWhiteSpace(novoNome))
            return;

        parte.Nome = novoNome;

        await _database.AtualizarParteAsync(parte);

        listaPartes.ItemsSource =
            await _database.GetPartesAsync();
    }

    private async void ExcluirParte_Clicked(
        object sender,
        EventArgs e)
    {
        var button = (Button)sender;

        var parte =
            (Parte)button.CommandParameter;

        bool confirmar =
            await DisplayAlert(
                "Excluir",
                $"Deseja excluir {parte.Nome}?",
                "Sim",
                "Não");

        if (!confirmar)
            return;

        await _database.ExcluirParteAsync(parte);

        listaPartes.ItemsSource =
            await _database.GetPartesAsync();
    }
}