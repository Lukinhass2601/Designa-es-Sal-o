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

        List<DesignacaoResultado> resultado = new();

        var participantesJaUsados = new List<int>();

        foreach (var parte in partes)
        {
            var participantesHabilitados =
                await _database.GetParticipantesPorParteAsync(
                    parte.Id);

            var participantesOrdenados =
                new List<(Participante Participante, int Quantidade)>();
            foreach (var participanteAtual in participantesHabilitados)
            {
                var quantidade =
                    await _database.GetQuantidadeDesignacoesAsync(
                        parte.Nome,
                        participanteAtual.Nome);

                participantesOrdenados.Add(
                    (participanteAtual, quantidade));
            }

            participantesHabilitados =
                participantesOrdenados
                .OrderBy(x => x.Quantidade)
                .ThenBy(x => Guid.NewGuid())
                .Select(x => x.Participante)
                .Where(x => !participantesJaUsados.Contains(x.Id))
                .ToList();

            if (!participantesHabilitados.Any())
            {
                resultado.Add(new DesignacaoResultado
                {
                    Parte = parte.Nome,
                    Participante = "SEM HABILITADO"
                });

                continue;
            }

            var participante = participantesHabilitados.First();

            resultado.Add(new DesignacaoResultado
            {
                Parte = parte.Nome,
                Participante = participante.Nome
            });

            participantesJaUsados.Add(participante.Id);
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