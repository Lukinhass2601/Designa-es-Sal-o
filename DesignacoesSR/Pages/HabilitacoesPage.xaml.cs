using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HabilitacoesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<ParteHabilitacao> _partes = new();

    public HabilitacoesPage(
        DatabaseService database)
    {
        InitializeComponent();

        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarParticipantesAsync();
        await CarregarPartesAsync();

        btnSalvarHabilitacoes.IsEnabled =
            pickerParticipante.SelectedItem != null;
    }

    private async Task CarregarParticipantesAsync()
    {
        var participanteSelecionado =
            pickerParticipante.SelectedItem
            as Participante;

        var participantes =
            await _database.GetParticipantesAsync();

        participantes =
            participantes
                .Where(x => x.Ativo)
                .OrderBy(x => x.Nome)
                .ToList();

        pickerParticipante.ItemsSource =
            participantes;

        pickerParticipante.ItemDisplayBinding =
            new Binding(nameof(Participante.Nome));

        if (participanteSelecionado != null)
        {
            pickerParticipante.SelectedItem =
                participantes.FirstOrDefault(
                    x => x.Id ==
                         participanteSelecionado.Id);
        }
    }

    private async Task CarregarPartesAsync()
    {
        var partes =
            await _database.GetPartesAsync();

        _partes =
            partes
                .OrderBy(x => x.Nome)
                .Select(
                    parte =>
                        new ParteHabilitacao
                        {
                            ParteId =
                                parte.Id,

                            Nome =
                                parte.Nome,

                            Selecionado =
                                false
                        })
                .ToList();

        listaPartes.ItemsSource = null;
        listaPartes.ItemsSource = _partes;

        await CarregarHabilitacoesSelecionadasAsync();
    }

    private async Task
        CarregarHabilitacoesSelecionadasAsync()
    {
        foreach (var parte in _partes)
        {
            parte.Selecionado = false;
        }

        if (pickerParticipante.SelectedItem
            is not Participante participante)
        {
            AtualizarListaPartes();

            btnSalvarHabilitacoes.IsEnabled =
                false;

            return;
        }

        var habilitacoes =
            await _database
                .GetHabilitacoesParticipanteAsync(
                    participante.Id);

        var idsHabilitados =
            habilitacoes
                .Select(x => x.ParteId)
                .ToHashSet();

        foreach (var parte in _partes)
        {
            parte.Selecionado =
                idsHabilitados.Contains(
                    parte.ParteId);
        }

        AtualizarListaPartes();

        btnSalvarHabilitacoes.IsEnabled =
            true;
    }

    private void AtualizarListaPartes()
    {
        listaPartes.ItemsSource = null;
        listaPartes.ItemsSource = _partes;
    }

    private async void
        pickerParticipante_SelectedIndexChanged(
            object sender,
            EventArgs e)
    {
        await CarregarHabilitacoesSelecionadasAsync();
    }

    private async void Salvar_Clicked(
        object sender,
        EventArgs e)
    {
        if (pickerParticipante.SelectedItem
            is not Participante participante)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione um participante.",
                "OK");

            return;
        }

        try
        {
            btnSalvarHabilitacoes.IsEnabled =
                false;

            await _database
                .RemoverHabilitacoesParticipanteAsync(
                    participante.Id);

            var partesSelecionadas =
                _partes
                    .Where(x => x.Selecionado)
                    .ToList();

            foreach (var parte in partesSelecionadas)
            {
                await _database
                    .SalvarParticipanteParteAsync(
                        new ParticipanteParte
                        {
                            ParticipanteId =
                                participante.Id,

                            ParteId =
                                parte.ParteId
                        });
            }

            await DisplayAlert(
                "Sucesso",
                $"Habilitações de {participante.Nome} " +
                "salvas com sucesso.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível salvar as habilitações." +
                $"\n\n{ex.Message}",
                "OK");
        }
        finally
        {
            btnSalvarHabilitacoes.IsEnabled =
                pickerParticipante.SelectedItem != null;
        }
    }

    private async void ExcluirParte_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter
                is not ParteHabilitacao parteItem)
        {
            return;
        }

        var confirmar =
            await DisplayAlert(
                "Excluir parte",
                $"Deseja excluir a parte " +
                $"'{parteItem.Nome}'?\n\n" +
                "Essa ação também excluirá as habilitações " +
                "e programações semanais relacionadas a ela.",
                "Excluir",
                "Cancelar");

        if (!confirmar)
            return;

        try
        {
            await _database
                .ExcluirParteCompletaAsync(
                    parteItem.ParteId);

            await CarregarPartesAsync();

            await DisplayAlert(
                "Sucesso",
                "A parte foi excluída.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível excluir a parte." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }
}