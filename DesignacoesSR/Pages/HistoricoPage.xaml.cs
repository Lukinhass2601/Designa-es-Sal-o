using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class HistoricoPage : ContentPage
{
    private readonly DatabaseService _database;

    private DateTime _dataSelecionada = DateTime.MinValue;

    public HistoricoPage(DatabaseService database)
    {
        InitializeComponent();

        _database = database;
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

            listaSemanas.ItemsSource = null;
            listaSemanas.ItemsSource = semanas;

            if (semanas.Count == 0)
            {
                _dataSelecionada =
                    DateTime.MinValue;

                listaSemanas.SelectedItem =
                    null;

                listaDesignacoes.ItemsSource =
                    null;

                lblSemanaSelecionada.Text =
                    "Nenhuma semana foi salva";

                btnExcluirSemana.IsEnabled =
                    false;

                return;
            }

            var semanaMaisRecente =
                semanas
                    .OrderByDescending(x => x)
                    .First();

            _dataSelecionada =
                semanaMaisRecente.Date;

            listaSemanas.SelectedItem =
                semanaMaisRecente;

            lblSemanaSelecionada.Text =
                $"Designações de {_dataSelecionada:dd/MM/yyyy}";

            btnExcluirSemana.IsEnabled =
                true;

            await CarregarDesignacoesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível carregar o histórico." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }

    private async void listaSemanas_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        var itemSelecionado =
            e.CurrentSelection.FirstOrDefault();

        if (itemSelecionado is not DateTime data)
            return;

        _dataSelecionada =
            data.Date;

        lblSemanaSelecionada.Text =
            $"Designações de {_dataSelecionada:dd/MM/yyyy}";

        btnExcluirSemana.IsEnabled =
            true;

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
                "Não foi possível carregar as designações." +
                $"\n\n{ex.Message}",
                "OK");
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
                $"da semana {_dataSelecionada:dd/MM/yyyy}?",
                "Excluir",
                "Cancelar");

        if (!confirmar)
            return;

        try
        {
            btnExcluirSemana.IsEnabled =
                false;

            await _database.ExcluirSemanaAsync(
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
                "A semana foi excluída do histórico.",
                "OK");
        }
        catch (Exception ex)
        {
            btnExcluirSemana.IsEnabled =
                true;

            await DisplayAlert(
                "Erro",
                "Não foi possível excluir a semana." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }
}