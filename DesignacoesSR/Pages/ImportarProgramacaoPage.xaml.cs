using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class ImportarProgramacaoPage : ContentPage
{
    private readonly DatabaseService _database;
    private readonly JwProgramacaoService _jwService;

    private List<ParteSemanaImportacao>
        _previa = new();

    public ImportarProgramacaoPage(
        DatabaseService database,
        JwProgramacaoService jwService)
    {
        InitializeComponent();

        _database = database;
        _jwService = jwService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        LimparPrevia();
    }

    private DateTime ObterDataSelecionada()
    {
        return dtSemana.Date
               ?? DateTime.Today;
    }

    private async void BuscarProgramacao_Clicked(
        object sender,
        EventArgs e)
    {
        var url =
            txtUrlProgramacao.Text?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            await DisplayAlert(
                "Aviso",
                "Informe o link da página semanal do JW.ORG.",
                "OK");

            return;
        }

        try
        {
            btnBuscarProgramacao.IsEnabled = false;
            btnSalvarProgramacao.IsEnabled = false;

            listaPrevia.ItemsSource = null;
            _previa.Clear();

            _previa =
                await _jwService.BuscarProgramacaoAsync(
                    url,
                    ObterDataSelecionada());

            if (_previa.Count == 0)
            {
                await DisplayAlert(
                    "Nenhuma parte encontrada",
                    "Não foram encontradas partes numeradas " +
                    "na página informada.",
                    "OK");

                return;
            }

            listaPrevia.ItemsSource = _previa;

            AtualizarBotaoSalvar();

            var partesEncontradas =
                _previa.Count(
                    x => x.ParteBaseEncontrada);

            var partesNaoEncontradas =
                _previa.Count -
                partesEncontradas;

            await DisplayAlert(
                "Programação carregada",
                $"Partes encontradas: {_previa.Count}\n" +
                $"Relacionadas: {partesEncontradas}\n" +
                $"Sem parte base: {partesNaoEncontradas}",
                "OK");
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert(
                "Erro de conexão",
                "Não foi possível acessar o JW.ORG." +
                $"\n\n{ex.Message}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível interpretar a programação." +
                $"\n\n{ex.Message}",
                "OK");
        }
        finally
        {
            btnBuscarProgramacao.IsEnabled = true;
        }
    }

    private async void SalvarProgramacao_Clicked(
        object sender,
        EventArgs e)
    {
        var partesSelecionadas =
            _previa
                .Where(x => x.Selecionado)
                .ToList();

        if (partesSelecionadas.Count == 0)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione ao menos uma parte.",
                "OK");

            return;
        }

        var partesSemBase =
            partesSelecionadas
                .Where(x =>
                    !x.ParteBaseEncontrada ||
                    x.ParteBaseId <= 0)
                .ToList();

        if (partesSemBase.Count > 0)
        {
            var nomes =
                string.Join(
                    "\n",
                    partesSemBase
                        .Take(8)
                        .Select(
                            x =>
                                $"{x.Numero}. " +
                                $"{x.TituloOriginal}"));

            await DisplayAlert(
                "Partes sem relacionamento",
                "Estas partes não possuem uma parte base " +
                "correspondente:\n\n" +
                nomes +
                "\n\nCadastre ou ajuste as partes base " +
                "antes de importar.",
                "OK");

            return;
        }

        var dataSemana =
            ObterDataSelecionada();

        var partesJaExistentes =
            await _database.GetPartesDaSemanaAsync(
                dataSemana);

        if (partesJaExistentes.Count > 0)
        {
            var substituir =
                await DisplayAlert(
                    "Programação já cadastrada",
                    $"Já existem partes cadastradas para " +
                    $"{dataSemana:dd/MM/yyyy}.\n\n" +
                    "Deseja substituir a programação existente?",
                    "Substituir",
                    "Cancelar");

            if (!substituir)
                return;
        }
        else
        {
            var confirmar =
                await DisplayAlert(
                    "Salvar programação",
                    $"Deseja salvar " +
                    $"{partesSelecionadas.Count} partes para " +
                    $"{dataSemana:dd/MM/yyyy}?",
                    "Salvar",
                    "Cancelar");

            if (!confirmar)
                return;
        }

        try
        {
            btnSalvarProgramacao.IsEnabled = false;

            var novasPartes =
                partesSelecionadas
                    .Select(
                        item =>
                            new ParteSemana
                            {
                                ParteId =
                                    item.ParteBaseId,

                                DataSemana =
                                    dataSemana.Date,

                                Numero =
                                    item.Numero,

                                Titulo =
                                    item.TituloOriginal,

                                Descricao =
                                    item.Descricao,

                                DuracaoMinutos =
                                    item.DuracaoMinutos,

                                UrlOrigem =
                                    item.UrlOrigem
                            })
                    .ToList();

            await _database
                .SubstituirPartesDaSemanaAsync(
                    dataSemana,
                    novasPartes);

            await DisplayAlert(
                "Sucesso",
                $"Programação de {dataSemana:dd/MM/yyyy} " +
                "salva com sucesso.",
                "OK");

            LimparPrevia();
        }
        catch (Exception ex)
        {
            btnSalvarProgramacao.IsEnabled = true;

            await DisplayAlert(
                "Erro",
                "Não foi possível salvar a programação." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private void LimparPrevia()
    {
        _previa.Clear();

        listaPrevia.ItemsSource = null;

        btnSalvarProgramacao.IsEnabled = false;
    }
    private async void AdicionarParteBase_Clicked(
    object sender,
    EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter
                is not ParteSemanaImportacao item)
        {
            return;
        }

        if (item.ParteBaseEncontrada)
        {
            await DisplayAlert(
                "Aviso",
                "Essa parte já está cadastrada.",
                "OK");

            return;
        }

        var nomeParte = await DisplayPromptAsync(
            "Adicionar parte",
            "Confira ou altere o nome da parte:",
            accept: "Continuar",
            cancel: "Cancelar",
            initialValue: item.TituloOriginal,
            maxLength: 150);

        if (string.IsNullOrWhiteSpace(nomeParte))
            return;

        nomeParte = nomeParte.Trim();

        var parteExistente =
            await _database
                .GetPartePorNomeNormalizadoAsync(
                    nomeParte);

        if (parteExistente != null)
        {
            item.ParteBaseId =
                parteExistente.Id;

            item.NomeParteBase =
                parteExistente.Nome;

            item.ParteBaseEncontrada =
                true;

            AtualizarPrevia();

            await DisplayAlert(
                "Parte encontrada",
                "Essa parte já existia e foi relacionada " +
                "com a programação.",
                "OK");

            return;
        }

        var quantidadeTexto =
            await DisplayActionSheet(
                "Quantidade de participantes",
                "Cancelar",
                null,
                "1 participante",
                "2 participantes");

        if (quantidadeTexto == "Cancelar" ||
            string.IsNullOrWhiteSpace(quantidadeTexto))
        {
            return;
        }

        var quantidadeParticipantes =
            quantidadeTexto.StartsWith("2")
                ? 2
                : 1;

        var sexoTexto =
            await DisplayActionSheet(
                "Sexo permitido para esta parte",
                "Cancelar",
                null,
                "Masculino",
                "Feminino");

        if (sexoTexto == "Cancelar" ||
            string.IsNullOrWhiteSpace(sexoTexto))
        {
            return;
        }

        var sexoPermitido =
            sexoTexto == "Feminino"
                ? "F"
                : "M";

        var confirmar =
            await DisplayAlert(
                "Confirmar nova parte",
                $"Nome: {nomeParte}\n" +
                $"Participantes: {quantidadeParticipantes}\n" +
                $"Sexo permitido: {sexoTexto}\n\n" +
                "Deseja adicionar essa parte às habilitações?",
                "Adicionar",
                "Cancelar");

        if (!confirmar)
            return;

        try
        {
            var novaParte =
                new Parte
                {
                    Nome =
                        nomeParte,

                    QuantidadeParticipantes =
                        quantidadeParticipantes,

                    SexoPermitido =
                        sexoPermitido
                };
            var opcaoGrupo =
    await DisplayActionSheet(
        "Quem pode fazer esta parte?",
        "Cancelar",
        null,
        "Somente Anciãos",
        "Somente Servos Ministeriais",
        "Anciãos e Servos",
        "Selecionar manualmente");

            if (string.IsNullOrWhiteSpace(opcaoGrupo) ||
                opcaoGrupo == "Cancelar")
            {
                return;
            }

            var codigoGrupo =
                opcaoGrupo switch
                {
                    "Somente Anciãos" =>
                        "ANCIAO",

                    "Somente Servos Ministeriais" =>
                        "SERVO",

                    "Anciãos e Servos" =>
                        "ANCIAOS_E_SERVOS",

                    _ =>
                        "MANUAL"
                };
            await _database
                .SalvarParteAsync(
                    novaParte);
            var quantidadeHabilitada = 0;

            if (codigoGrupo != "MANUAL")
            {
                quantidadeHabilitada =
                    await _database
                        .HabilitarGrupoParaParteAsync(
                            novaParte.Id,
                            codigoGrupo);
            }

            item.ParteBaseId =
                novaParte.Id;

            item.NomeParteBase =
                novaParte.Nome;

            item.ParteBaseEncontrada =
                true;



            AtualizarPrevia();

            AtualizarBotaoSalvar();

            await DisplayAlert(
                "Parte adicionada",
                $"A parte '{novaParte.Nome}' foi adicionada.\n\n" +
                "Agora abra a aba Habilitações e selecione " +
                "quem poderá fazer essa parte.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível adicionar a parte." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private void AtualizarPrevia()
    {
        listaPrevia.ItemsSource = null;
        listaPrevia.ItemsSource = _previa;
    }

    private void AtualizarBotaoSalvar()
    {
        var partesSelecionadas =
            _previa
                .Where(x => x.Selecionado)
                .ToList();

        btnSalvarProgramacao.IsEnabled =
            partesSelecionadas.Count > 0 &&
            partesSelecionadas.All(
                x => x.ParteBaseEncontrada);
    }
}