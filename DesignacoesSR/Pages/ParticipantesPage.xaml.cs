using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class ParticipantesPage : ContentPage
{
    private readonly DatabaseService _database;

    public ParticipantesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        listaParticipantes.ItemsSource =
            await _database.GetParticipantesAsync();
    }

    private async void EditarParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        var button = (Button)sender;

        var participante =
            (Participante)button.CommandParameter;

        string novoNome =
            await DisplayPromptAsync(
                "Editar Participante",
                "Digite o novo nome:",
                initialValue: participante.Nome);

        if (string.IsNullOrWhiteSpace(novoNome))
            return;

        participante.Nome = novoNome;

        await _database.AtualizarParticipanteAsync(
            participante);

        listaParticipantes.ItemsSource =
            await _database.GetParticipantesAsync();
    }

    private async void ExcluirParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        var button = (Button)sender;

        var participante =
            (Participante)button.CommandParameter;

        bool confirmar =
            await DisplayAlert(
                "Excluir",
                $"Deseja excluir {participante.Nome}?",
                "Sim",
                "Não");

        if (!confirmar)
            return;

        await _database.ExcluirParticipanteAsync(
            participante);

        listaParticipantes.ItemsSource =
            await _database.GetParticipantesAsync();
    }

    private async void HabilitacoesParticipante_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;

        var participante =
            (Participante)button.CommandParameter;

        await DisplayAlert(
            "Participante",
            participante.Nome,
            "OK");
    }
}