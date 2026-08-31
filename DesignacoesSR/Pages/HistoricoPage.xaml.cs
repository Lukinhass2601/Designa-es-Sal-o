using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HistoricoPage : ContentPage
{
    private readonly DatabaseService _database;

    private readonly RelatorioExcelService
        _relatorioExcelService;

    private DateTime _dataSelecionada =
        DateTime.MinValue;

    public HistoricoPage(
        DatabaseService database,
        RelatorioExcelService relatorioExcelService)
    {
        InitializeComponent();

        _database = database;

        _relatorioExcelService =
            relatorioExcelService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarSemanasAsync();
    }

    private async Task CarregarSemanasAsync()
    {
        try
        {
            var semanas =
                await _database.GetSemanasAsync();

            listaSemanas.ItemsSource =
                null;

            listaSemanas.ItemsSource =
                semanas;

            if (semanas.Count == 0)
            {
                LimparSelecao();

                lblSemanaSelecionada.Text =
                    "Nenhuma semana foi salva";

                return;
            }

            DateTime semanaParaSelecionar;

            var semanaAtualAindaExiste =
                semanas.Any(
                    data =>
                        data.Date ==
                        _dataSelecionada.Date);

            if (_dataSelecionada != DateTime.MinValue &&
                semanaAtualAindaExiste)
            {
                semanaParaSelecionar =
                    semanas.First(
                        data =>
                            data.Date ==
                            _dataSelecionada.Date);
            }
            else
            {
                semanaParaSelecionar =
                    semanas
                        .OrderByDescending(
                            data => data)
                        .First();
            }

            _dataSelecionada =
                semanaParaSelecionar.Date;

            listaSemanas.SelectedItem =
                semanaParaSelecionar;

            lblSemanaSelecionada.Text =
                $"Designações de " +
                $"{_dataSelecionada:dd/MM/yyyy}";

            AtualizarEstadoBotoes(
                true);

            await CarregarDesignacoesAsync();
        }
        catch (Exception ex)
        {
            LimparSelecao();

            await DisplayAlert(
                "Erro",
                "Não foi possível carregar o histórico." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private async void listaSemanas_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        var itemSelecionado =
            e.CurrentSelection.FirstOrDefault();

        if (itemSelecionado is not DateTime data)
        {
            return;
        }

        _dataSelecionada =
            data.Date;

        lblSemanaSelecionada.Text =
            $"Designações de " +
            $"{_dataSelecionada:dd/MM/yyyy}";

        AtualizarEstadoBotoes(
            true);

        await CarregarDesignacoesAsync();
    }

    private async Task CarregarDesignacoesAsync()
    {
        try
        {
            if (_dataSelecionada ==
                DateTime.MinValue)
            {
                listaDesignacoes.ItemsSource =
                    null;

                return;
            }

            var designacoes =
                await _database
                    .GetDesignacoesSemanaAsync(
                        _dataSelecionada);

            listaDesignacoes.ItemsSource =
                null;

            listaDesignacoes.ItemsSource =
                designacoes;
        }
        catch (Exception ex)
        {
            listaDesignacoes.ItemsSource =
                null;

            await DisplayAlert(
                "Erro",
                "Não foi possível carregar as designações " +
                "da semana selecionada." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private async void GerarRelatorio_Clicked(
        object sender,
        EventArgs e)
    {
        if (_dataSelecionada ==
            DateTime.MinValue)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione uma semana para gerar o relatório.",
                "OK");

            return;
        }

        try
        {
            AtualizarEstadoBotoes(
                false);

            var designacoes =
                await _database
                    .GetDesignacoesSemanaAsync(
                        _dataSelecionada);

            if (designacoes.Count == 0)
            {
                await DisplayAlert(
                    "Histórico vazio",
                    "A semana selecionada não possui " +
                    "designações salvas.",
                    "OK");

                return;
            }

            var caminhoArquivo =
                await _relatorioExcelService
                    .GerarAsync(
                        _dataSelecionada);

            var mensagem =
                "O relatório foi gerado com sucesso.";

            if (!string.IsNullOrWhiteSpace(
                    caminhoArquivo))
            {
                mensagem +=
                    Environment.NewLine +
                    Environment.NewLine +
                    "Arquivo salvo em:" +
                    Environment.NewLine +
                    caminhoArquivo;
            }

            await DisplayAlert(
                "Relatório gerado",
                mensagem,
                "OK");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert(
                "Operação cancelada",
                "O salvamento do relatório foi cancelado.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível gerar o relatório." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
        finally
        {
            AtualizarEstadoBotoes(
                _dataSelecionada !=
                DateTime.MinValue);
        }
    }

    private async void ExcluirSemana_Clicked(
        object sender,
        EventArgs e)
    {
        if (_dataSelecionada ==
            DateTime.MinValue)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione uma semana para excluir.",
                "OK");

            return;
        }

        var confirmar =
            await DisplayAlert(
                "Excluir semana",
                $"Deseja excluir todas as designações " +
                $"da semana {_dataSelecionada:dd/MM/yyyy}?" +
                Environment.NewLine +
                Environment.NewLine +
                "Os registros dessa semana também serão " +
                "removidos do controle de rodízio.",
                "Excluir",
                "Cancelar");

        if (!confirmar)
        {
            return;
        }

        try
        {
            AtualizarEstadoBotoes(
                false);

            await _database
                .ExcluirSemanaAsync(
                    _dataSelecionada);

            await _database
                .ExcluirDesignacoesParticipantesDaSemanaAsync(
                    _dataSelecionada);

            _dataSelecionada =
                DateTime.MinValue;

            listaSemanas.SelectedItem =
                null;

            listaDesignacoes.ItemsSource =
                null;

            lblSemanaSelecionada.Text =
                "Selecione uma semana para visualizar";

            await CarregarSemanasAsync();

            await DisplayAlert(
                "Sucesso",
                "A semana foi excluída do histórico " +
                "e do controle de rodízio.",
                "OK");
        }
        catch (Exception ex)
        {
            AtualizarEstadoBotoes(
                true);

            await DisplayAlert(
                "Erro",
                "Não foi possível excluir a semana." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private void AtualizarEstadoBotoes(
        bool habilitado)
    {
        btnExcluirSemana.IsEnabled =
            habilitado;

        btnGerarRelatorio.IsEnabled =
            habilitado;
    }

    private void LimparSelecao()
    {
        _dataSelecionada =
            DateTime.MinValue;

        listaSemanas.SelectedItem =
            null;

        listaDesignacoes.ItemsSource =
            null;

        AtualizarEstadoBotoes(
            false);
    }
}