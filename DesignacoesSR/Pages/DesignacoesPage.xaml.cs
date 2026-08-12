using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class DesignacoesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<DesignacaoResultado> _ultimoResultado = new();

    public DesignacoesPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    private async void VisualizarDesignacoes_Clicked(
        object sender,
        EventArgs e)
    {
        var participantes =
    (await _database.GetParticipantesAsync())
    .Where(x => x.Ativo)
    .ToList();
        var partes = await _database.GetPartesAsync();

        if (participantes.Count < partes.Count)
        {
            await DisplayAlert(
                "Aviso",
                "Existem menos participantes do que partes.",
                "OK");

            return;
        }

        var participantesSorteados = participantes
            .OrderBy(x => x.UltimaParticipacao)
            .ThenBy(x => Guid.NewGuid())
            .ToList();

        List<DesignacaoResultado> resultado = new();

        foreach (var parte in partes)
        {
            var participante = participantesSorteados.First();

            resultado.Add(new DesignacaoResultado
            {
                Parte = parte.Nome,
                Participante = participante.Nome
            });

            participantesSorteados.Remove(participante);
        }
        _ultimoResultado = resultado;

        btnSalvarPrograma.IsEnabled = true;
        listaDesignacoes.ItemsSource = resultado;
    }
    private async void SalvarPrograma_Clicked(
    object sender,
    EventArgs e)
    {
        foreach (var item in _ultimoResultado)
        {
            await _database.SalvarDesignacaoAsync(
                new Designacao
                {
                    DataSemana = dtSemana.Date.Value,
                    Parte = item.Parte,
                    Participante = item.Participante
                });

            var participante =
                await _database.GetParticipantePorNomeAsync(
                    item.Participante);

            if (participante != null)
            {
                participante.UltimaParticipacao =
                    dtSemana.Date;

                await _database.AtualizarParticipanteAsync(
                    participante);
            }
        }

        await DisplayAlert(
            "Sucesso",
            "Programa salvo no histórico.",
            "OK");

        btnSalvarPrograma.IsEnabled = false;
    }
}