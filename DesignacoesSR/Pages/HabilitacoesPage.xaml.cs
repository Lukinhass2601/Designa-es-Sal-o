using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HabilitacoesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<ParteHabilitacao> _partes = new();

    public HabilitacoesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var participantes =
            await _database.GetParticipantesAsync();

        pickerParticipante.ItemsSource =
            participantes;

        pickerParticipante.ItemDisplayBinding =
            new Binding("Nome");

        var partes =
            await _database.GetPartesAsync();

        _partes = partes.Select(p => new ParteHabilitacao
        {
            ParteId = p.Id,
            Nome = p.Nome,
            Selecionado = false
        }).ToList();

        listaPartes.ItemsSource = _partes;
    }

    private void pickerParticipante_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
    }

    private async void Salvar_Clicked(
        object sender,
        EventArgs e)
    {
        if (pickerParticipante.SelectedItem == null)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione um participante.",
                "OK");

            return;
        }

        var participante =
            (Participante)pickerParticipante.SelectedItem;

        await _database
            .RemoverHabilitacoesParticipanteAsync(
                participante.Id);

        foreach (var parte in _partes.Where(x => x.Selecionado))
        {
            await _database.SalvarParticipanteParteAsync(
                new ParticipanteParte
                {
                    ParticipanteId = participante.Id,
                    ParteId = parte.ParteId
                });
        }

        await DisplayAlert(
            "Sucesso",
            "Habilitações salvas com sucesso.",
            "OK");
    }
}