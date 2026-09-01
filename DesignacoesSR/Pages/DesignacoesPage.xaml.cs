using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class DesignacoesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<DesignacaoResultado> _ultimoResultado =
        new();

    public DesignacoesPage(
        DatabaseService database)
    {
        InitializeComponent();

        _database = database;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ultimoResultado.Clear();

        listaDesignacoes.ItemsSource =
            null;

        btnSalvarPrograma.IsEnabled =
            false;
    }

    private DateTime ObterDataSelecionada()
    {
        return dtSemana.Date
               ?? DateTime.Today;
    }

    private async void VisualizarDesignacoes_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            btnSalvarPrograma.IsEnabled =
                false;

            listaDesignacoes.ItemsSource =
                null;

            _ultimoResultado.Clear();

            var dataSemana =
                ObterDataSelecionada();

            var partesSemana =
                await _database
                    .GetPartesDaSemanaAsync(
                        dataSemana);

            if (partesSemana.Count == 0)
            {
                await DisplayAlert(
                    "Programação não encontrada",
                    $"Não existem partes cadastradas para " +
                    $"{dataSemana:dd/MM/yyyy}.\n\n" +
                    "Cadastre ou importe primeiro a programação.",
                    "OK");

                return;
            }

            var resultado =
                new List<DesignacaoResultado>();

            /*
             * Impede que uma mesma pessoa seja usada
             * novamente em outra parte da mesma semana.
             */
            var participantesJaUsados =
                new HashSet<int>();

            foreach (var parteSemana in
                     partesSemana.OrderBy(x => x.Numero))
            {
                var parteBase =
                    await _database
                        .GetPartePorIdAsync(
                            parteSemana.ParteId);

                if (parteBase == null)
                {
                    resultado.Add(
                        CriarResultadoComAviso(
                            parteSemana,
                            "PARTE BASE NÃO ENCONTRADA"));

                    continue;
                }

                /*
                 * Busca somente quem possui habilitação
                 * para a parte base atual.
                 */
                var participantesHabilitados =
                    await _database
                        .GetParticipantesPorParteAsync(
                            parteBase.Id);

                /*
                 * Aplica as regras permanentes:
                 *
                 * 1. Precisa estar ativo.
                 * 2. Precisa possuir o sexo permitido.
                 * 3. Não pode ter sido escolhido em outra
                 *    parte desta mesma programação.
                 */
                participantesHabilitados =
                    participantesHabilitados
                        .Where(x => x.Ativo)
                        .Where(x =>
                            string.Equals(
                                x.Sexo,
                                parteBase.SexoPermitido,
                                StringComparison.OrdinalIgnoreCase))
                        .Where(x =>
                            !participantesJaUsados.Contains(
                                x.Id))
                        .ToList();

                /*
                 * Ordena pelo rodízio específico da parte.
                 *
                 * Primeiro aparecem os participantes que
                 * fizeram menos vezes essa parte.
                 */
                var parteFeminina =
    string.Equals(
        parteBase.SexoPermitido,
        "F",
        StringComparison.OrdinalIgnoreCase);

                if (parteFeminina)
                {
                    participantesHabilitados =
                        await _database
                            .OrdenarMulheresPorRodizioGeralAsync(
                                participantesHabilitados);
                }
                else
                {
                    participantesHabilitados =
                        await _database
                            .OrdenarParticipantesPorRodizioAsync(
                                participantesHabilitados,
                                parteBase.Id);
                }

                var quantidadeNecessaria =
                    parteBase.QuantidadeParticipantes <= 1
                        ? 1
                        : 2;

                if (participantesHabilitados.Count <
                    quantidadeNecessaria)
                {
                    var aviso =
                        quantidadeNecessaria == 1
                            ? "SEM HABILITADO"
                            : "FALTAM PARTICIPANTES";

                    resultado.Add(
                        CriarResultadoComAviso(
                            parteSemana,
                            aviso));

                    continue;
                }

                var participante1 =
                    participantesHabilitados[0];

                Participante? participante2 =
                    null;

                if (quantidadeNecessaria == 2)
                {
                    participante2 =
                        participantesHabilitados[1];
                }

                resultado.Add(
                    new DesignacaoResultado
                    {
                        ParteSemanaId =
                            parteSemana.Id,

                        ParteId =
                            parteBase.Id,

                        Numero =
                            parteSemana.Numero,

                        Parte =
                            parteSemana.Titulo,

                        Descricao =
                            parteSemana.Descricao,

                        DuracaoMinutos =
                            parteSemana.DuracaoMinutos,

                        Participante1Id =
                            participante1.Id,

                        Participante1 =
                            participante1.Nome,

                        Participante2Id =
                            participante2?.Id ?? 0,

                        Participante2 =
                            participante2?.Nome
                            ?? string.Empty
                    });

                participantesJaUsados.Add(
                    participante1.Id);

                if (participante2 != null)
                {
                    participantesJaUsados.Add(
                        participante2.Id);
                }
            }

            _ultimoResultado =
                resultado;

            listaDesignacoes.ItemsSource =
                resultado;

            var possuiErro =
                resultado.Any(
                    ResultadoPossuiErro);

            btnSalvarPrograma.IsEnabled =
                resultado.Count > 0 &&
                !possuiErro;

            if (possuiErro)
            {
                await DisplayAlert(
                    "Programa incompleto",
                    "Algumas partes não possuem participantes " +
                    "habilitados suficientes.\n\n" +
                    "Confira as habilitações, o sexo permitido " +
                    "e a quantidade de participantes.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            btnSalvarPrograma.IsEnabled =
                false;

            await DisplayAlert(
                "Erro",
                "Não foi possível gerar as designações." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private static DesignacaoResultado
        CriarResultadoComAviso(
            ParteSemana parteSemana,
            string aviso)
    {
        return new DesignacaoResultado
        {
            ParteSemanaId =
                parteSemana.Id,

            ParteId =
                parteSemana.ParteId,

            Numero =
                parteSemana.Numero,

            Parte =
                parteSemana.Titulo,

            Descricao =
                parteSemana.Descricao,

            DuracaoMinutos =
                parteSemana.DuracaoMinutos,

            Participante1Id =
                0,

            Participante1 =
                aviso,

            Participante2Id =
                0,

            Participante2 =
                string.Empty
        };
    }

    private static bool ResultadoPossuiErro(
        DesignacaoResultado item)
    {
        return string.Equals(
                   item.Participante1,
                   "SEM HABILITADO",
                   StringComparison.OrdinalIgnoreCase) ||

               string.Equals(
                   item.Participante1,
                   "FALTAM PARTICIPANTES",
                   StringComparison.OrdinalIgnoreCase) ||

               string.Equals(
                   item.Participante1,
                   "PARTE BASE NÃO ENCONTRADA",
                   StringComparison.OrdinalIgnoreCase);
    }

    private async void SalvarPrograma_Clicked(
        object sender,
        EventArgs e)
    {
        if (_ultimoResultado.Count == 0)
        {
            await DisplayAlert(
                "Aviso",
                "Visualize as designações antes de salvar.",
                "OK");

            return;
        }

        if (_ultimoResultado.Any(
                ResultadoPossuiErro))
        {
            await DisplayAlert(
                "Programa incompleto",
                "Não é possível salvar porque existem partes " +
                "sem participantes suficientes.",
                "OK");

            return;
        }

        var dataSemana =
            ObterDataSelecionada();

        var programaJaExiste =
            await _database
                .ExisteProgramaNaSemanaAsync(
                    dataSemana);

        if (programaJaExiste)
        {
            var substituir =
                await DisplayAlert(
                    "Programa já cadastrado",
                    $"Já existe um programa salvo para " +
                    $"{dataSemana:dd/MM/yyyy}.\n\n" +
                    "Deseja substituir o programa existente?",
                    "Substituir",
                    "Cancelar");

            if (!substituir)
                return;
        }
        else
        {
            var confirmar =
                await DisplayAlert(
                    "Salvar programa",
                    $"Deseja salvar o programa da semana " +
                    $"{dataSemana:dd/MM/yyyy}?",
                    "Salvar",
                    "Cancelar");

            if (!confirmar)
                return;
        }

        try
        {
            btnSalvarPrograma.IsEnabled =
                false;

            if (programaJaExiste)
            {
                /*
                 * Remove o programa antigo e também os
                 * registros antigos do rodízio.
                 */
                await _database
                    .ExcluirDesignacoesDaSemanaAsync(
                        dataSemana);

                await _database
                    .ExcluirDesignacoesParticipantesDaSemanaAsync(
                        dataSemana);
            }

            foreach (var item in _ultimoResultado)
            {
                var nomesParticipantes =
                    string.IsNullOrWhiteSpace(
                        item.Participante2)
                        ? item.Participante1
                        : $"{item.Participante1} e " +
                          $"{item.Participante2}";

                /*
                 * Salva o registro que será mostrado
                 * no Histórico.
                 */
                await _database
                    .SalvarDesignacaoAsync(
                        new Designacao
                        {
                            DataSemana =
        dataSemana.Date,

                            ParteId =
        item.ParteId,

                            ParteSemanaId =
        item.ParteSemanaId,

                            Numero =
        item.Numero,

                            Parte =
        item.Parte,

                            Participante =
        nomesParticipantes
                        });

                /*
                 * Salva o primeiro participante
                 * individualmente no rodízio.
                 */
                if (item.Participante1Id > 0)
                {
                    await _database
                        .SalvarDesignacaoParticipanteAsync(
                            new DesignacaoParticipante
                            {
                                ParticipanteId =
                                    item.Participante1Id,

                                ParteId =
                                    item.ParteId,

                                ParteSemanaId =
                                    item.ParteSemanaId,

                                DataSemana =
                                    dataSemana.Date,

                                Posicao =
                                    1
                            });
                }

                /*
                 * Se a parte tiver duas pessoas,
                 * salva também o segundo participante.
                 */
                if (item.Participante2Id > 0)
                {
                    await _database
                        .SalvarDesignacaoParticipanteAsync(
                            new DesignacaoParticipante
                            {
                                ParticipanteId =
                                    item.Participante2Id,

                                ParteId =
                                    item.ParteId,

                                ParteSemanaId =
                                    item.ParteSemanaId,

                                DataSemana =
                                    dataSemana.Date,

                                Posicao =
                                    2
                            });
                }

                await AtualizarUltimaParticipacaoAsync(
                    item.Participante1,
                    dataSemana);

                if (!string.IsNullOrWhiteSpace(
                        item.Participante2))
                {
                    await AtualizarUltimaParticipacaoAsync(
                        item.Participante2,
                        dataSemana);
                }
            }

            var mensagem =
                programaJaExiste
                    ? $"Programa de {dataSemana:dd/MM/yyyy} " +
                      "substituído com sucesso."
                    : $"Programa de {dataSemana:dd/MM/yyyy} " +
                      "salvo no histórico.";

            await DisplayAlert(
                "Sucesso",
                mensagem,
                "OK");

            _ultimoResultado.Clear();

            listaDesignacoes.ItemsSource =
                null;

            btnSalvarPrograma.IsEnabled =
                false;
        }
        catch (Exception ex)
        {
            btnSalvarPrograma.IsEnabled =
                true;

            await DisplayAlert(
                "Erro",
                "Não foi possível salvar o programa." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private async Task
        AtualizarUltimaParticipacaoAsync(
            string nomeParticipante,
            DateTime dataSemana)
    {
        if (string.IsNullOrWhiteSpace(
                nomeParticipante))
        {
            return;
        }

        var participante =
            await _database
                .GetParticipantePorNomeAsync(
                    nomeParticipante);

        if (participante == null)
            return;

        participante.UltimaParticipacao =
            dataSemana.Date;

        await _database
            .AtualizarParticipanteAsync(
                participante);
    }
}