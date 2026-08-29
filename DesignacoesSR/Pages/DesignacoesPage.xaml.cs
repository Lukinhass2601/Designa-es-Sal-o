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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ultimoResultado.Clear();
        listaDesignacoes.ItemsSource = null;
        btnSalvarPrograma.IsEnabled = false;
    }

    private DateTime ObterDataSelecionada()
    {
        return dtSemana.Date ?? DateTime.Today;
    }

    private async void VisualizarDesignacoes_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            btnSalvarPrograma.IsEnabled = false;
            listaDesignacoes.ItemsSource = null;
            _ultimoResultado.Clear();

            var dataSemana = ObterDataSelecionada();

            // Busca somente as partes cadastradas
            // na programação da data selecionada.
            var partesSemana =
                await _database.GetPartesDaSemanaAsync(
                    dataSemana);

            if (partesSemana.Count == 0)
            {
                await DisplayAlert(
                    "Programação não encontrada",
                    $"Não existem partes cadastradas para " +
                    $"{dataSemana:dd/MM/yyyy}.\n\n" +
                    "Cadastre as partes primeiro na aba Programação.",
                    "OK");

                return;
            }

            var resultado =
                new List<DesignacaoResultado>();

            // Impede que alguém seja usado duas vezes
            // dentro da mesma visualização.
            var participantesJaUsados =
                new HashSet<int>();

            foreach (var parteSemana
                     in partesSemana.OrderBy(x => x.Numero))
            {
                var parteBase =
                    await _database.GetPartePorIdAsync(
                        parteSemana.ParteId);

                if (parteBase == null)
                {
                    resultado.Add(
                        CriarResultadoComAviso(
                            parteSemana,
                            "PARTE BASE NÃO ENCONTRADA"));

                    continue;
                }

                var participantesHabilitados =
                    await _database
                        .GetParticipantesPorParteAsync(
                            parteBase.Id);

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
                        .OrderBy(x => Guid.NewGuid())
                        .ToList();

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

                Participante? participante2 = null;

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

                        Numero =
                            parteSemana.Numero,

                        Parte =
                            parteSemana.Titulo,

                        Descricao =
                            parteSemana.Descricao,

                        DuracaoMinutos =
                            parteSemana.DuracaoMinutos,

                        Participante1 =
                            participante1.Nome,

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

            _ultimoResultado = resultado;

            listaDesignacoes.ItemsSource = resultado;

            var possuiErro =
                resultado.Any(ResultadoPossuiErro);

            btnSalvarPrograma.IsEnabled =
                resultado.Count > 0 && !possuiErro;

            if (possuiErro)
            {
                await DisplayAlert(
                    "Programa incompleto",
                    "Algumas partes não possuem participantes " +
                    "habilitados suficientes.\n\n" +
                    "Confira o sexo, a quantidade de pessoas e " +
                    "as habilitações.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            btnSalvarPrograma.IsEnabled = false;

            await DisplayAlert(
                "Erro",
                "Não foi possível visualizar as designações." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private static DesignacaoResultado CriarResultadoComAviso(
        ParteSemana parteSemana,
        string aviso)
    {
        return new DesignacaoResultado
        {
            ParteSemanaId =
                parteSemana.Id,

            Numero =
                parteSemana.Numero,

            Parte =
                parteSemana.Titulo,

            Descricao =
                parteSemana.Descricao,

            DuracaoMinutos =
                parteSemana.DuracaoMinutos,

            Participante1 =
                aviso,

            Participante2 =
                string.Empty
        };
    }

    private static bool ResultadoPossuiErro(
        DesignacaoResultado item)
    {
        return item.Participante1 ==
                   "SEM HABILITADO" ||

               item.Participante1 ==
                   "FALTAM PARTICIPANTES" ||

               item.Participante1 ==
                   "PARTE BASE NÃO ENCONTRADA";
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

        if (_ultimoResultado.Any(ResultadoPossuiErro))
        {
            await DisplayAlert(
                "Programa incompleto",
                "Não é possível salvar porque existem partes " +
                "sem participantes habilitados suficientes.",
                "OK");

            return;
        }

        var dataSemana = ObterDataSelecionada();

        var programaJaExiste =
            await _database.ExisteProgramaNaSemanaAsync(
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

            await _database
                .ExcluirDesignacoesDaSemanaAsync(
                    dataSemana);
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
            btnSalvarPrograma.IsEnabled = false;

            foreach (var item in _ultimoResultado)
            {
                var nomesParticipantes =
                    string.IsNullOrWhiteSpace(
                        item.Participante2)
                        ? item.Participante1
                        : $"{item.Participante1} e " +
                          $"{item.Participante2}";

                await _database.SalvarDesignacaoAsync(
                    new Designacao
                    {
                        DataSemana =
                            dataSemana.Date,

                        Parte =
                            item.Parte,

                        Participante =
                            nomesParticipantes
                    });

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
            listaDesignacoes.ItemsSource = null;
            btnSalvarPrograma.IsEnabled = false;
        }
        catch (Exception ex)
        {
            btnSalvarPrograma.IsEnabled = true;

            await DisplayAlert(
                "Erro",
                "Não foi possível salvar o programa." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private async Task AtualizarUltimaParticipacaoAsync(
        string nomeParticipante,
        DateTime dataSemana)
    {
        if (string.IsNullOrWhiteSpace(nomeParticipante))
            return;

        var participante =
            await _database.GetParticipantePorNomeAsync(
                nomeParticipante);

        if (participante == null)
            return;

        participante.UltimaParticipacao =
            dataSemana;

        await _database.AtualizarParticipanteAsync(
            participante);
    }
}