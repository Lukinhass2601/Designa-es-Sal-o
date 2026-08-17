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

        var partes =
            await _database.GetPartesAsync();

        List<DesignacaoResultado> resultado = new();

        var participantesJaUsados =
            new List<int>();

        foreach (var parte in partes)
        {
            var participantesHabilitados =
                await _database.GetParticipantesPorParteAsync(
                    parte.Id);

            //participantesHabilitados =
            //    participantesHabilitados
            //    .Where(x => x.Ativo)
            //    .Where(x => x.Sexo == parte.SexoPermitido)
            //    .Where(x => !participantesJaUsados.Contains(x.Id))
            //    .OrderBy(x => x.UltimaParticipacao ?? DateTime.MinValue)
            //    .Take(5)
            //    .OrderBy(x => Guid.NewGuid())
            //    .ToList();

            participantesHabilitados =
    participantesHabilitados
    .Where(x => x.Ativo)
    .Where(x => x.Sexo == parte.SexoPermitido)
    .Where(x => !participantesJaUsados.Contains(x.Id))
    .OrderBy(x => x.UltimaParticipacao ?? DateTime.MinValue)
    .Take(Math.Min(5, participantesHabilitados.Count))
    .OrderBy(x => Guid.NewGuid())
    .ToList();

            if (parte.QuantidadeParticipantes == 1)
            {
                if (!participantesHabilitados.Any())
                {
                    resultado.Add(
                        new DesignacaoResultado
                        {
                            Parte = parte.Nome,
                            Participante1 = "SEM HABILITADO"
                        });

                    continue;
                }

                var participante1 =
                    participantesHabilitados.First();

                resultado.Add(
                    new DesignacaoResultado
                    {
                        Parte = parte.Nome,
                        Participante1 = participante1.Nome
                    });

                participantesJaUsados.Add(
                    participante1.Id);
            }
            else
            {
                if (participantesHabilitados.Count < 2)
                {
                    resultado.Add(
                        new DesignacaoResultado
                        {
                            Parte = parte.Nome,
                            Participante1 =
                                "FALTAM PARTICIPANTES"
                        });

                    continue;
                }

                var participante1 =
                    participantesHabilitados[0];

                var participante2 =
                    participantesHabilitados[1];

                resultado.Add(
                    new DesignacaoResultado
                    {
                        Parte = parte.Nome,
                        Participante1 =
                            participante1.Nome,

                        Participante2 =
                            participante2.Nome
                    });

                participantesJaUsados.Add(
                    participante1.Id);

                participantesJaUsados.Add(
                    participante2.Id);
            }
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
                    Participante =
    string.IsNullOrWhiteSpace(item.Participante2)
        ? item.Participante1
        : $"{item.Participante1} e {item.Participante2}"
                });

            var participante =
    await _database.GetParticipantePorNomeAsync(
        item.Participante1);

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

    private void GerarNovoSorteio_Clicked(
    object sender,
    EventArgs e)
    {
        VisualizarDesignacoes_Clicked(sender, e);
    }
}