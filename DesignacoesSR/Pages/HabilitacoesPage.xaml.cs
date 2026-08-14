using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HabilitacoesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<ParteHabilitacao> _partes = new();

    public int ParticipanteSelecionadoId { get; set; }

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

        if (ParticipanteSelecionadoId > 0)
        {
            var participanteSelecionado =
                participantes.FirstOrDefault(
                    x => x.Id == ParticipanteSelecionadoId);

            if (participanteSelecionado != null)
            {
                pickerParticipante.SelectedItem =
                    participanteSelecionado;
            }
        }
    }

    private async void pickerParticipante_SelectedIndexChanged(
    object sender,
    EventArgs e)
    {
        if (pickerParticipante.SelectedItem == null)
            return;

        var participante =
            (Participante)pickerParticipante.SelectedItem;

        var habilitacoes =
            await _database
            .GetHabilitacoesParticipanteAsync(
                participante.Id);

        foreach (var parte in _partes)
        {
            parte.Selecionado = false;
        }

        foreach (var habilitacao in habilitacoes)
        {
            var parte = _partes.FirstOrDefault(
                x => x.ParteId ==
                habilitacao.ParteId);

            if (parte != null)
            {
                parte.Selecionado = true;
            }
        }

        listaPartes.ItemsSource = null;
        listaPartes.ItemsSource = _partes;
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