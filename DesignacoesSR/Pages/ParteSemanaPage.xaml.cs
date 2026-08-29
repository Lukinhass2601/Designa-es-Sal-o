using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class ParteSemanaPage : ContentPage
{
    private readonly DatabaseService _database;

    private ParteSemana? _parteSemanaEmEdicao;

    public ParteSemanaPage(
        DatabaseService database)
    {
        InitializeComponent();

        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarPartesBaseAsync();
        await CarregarPartesDaSemanaAsync();
    }

    private async Task CarregarPartesBaseAsync()
    {
        var partes =
            await _database.GetPartesAsync();

        pickerParte.ItemsSource =
            partes
                .OrderBy(x => x.Nome)
                .ToList();

        pickerParte.ItemDisplayBinding =
            new Binding(nameof(Parte.Nome));
    }

    private async Task CarregarPartesDaSemanaAsync()
    {
        var dataSelecionada =
            ObterDataSelecionada();

        lblDataSelecionada.Text =
            $"Semana selecionada: " +
            $"{dataSelecionada:dd/MM/yyyy}";

        var partesSemana =
            await _database.GetPartesDaSemanaAsync(
                dataSelecionada);

        listaPartesSemana.ItemsSource =
            null;

        listaPartesSemana.ItemsSource =
            partesSemana;

        lblNenhumaParte.IsVisible =
            partesSemana.Count == 0;

        btnExcluirProgramacao.IsEnabled =
            partesSemana.Count > 0;
    }


    
    private DateTime ObterDataSelecionada()
    {
        return dtSemana.Date?.Date
               ?? DateTime.Today;
    }

    private void pickerParte_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (pickerParte.SelectedItem
            is not Parte parte)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                txtTitulo.Text))
        {
            txtTitulo.Text = parte.Nome;
        }
    }

    private async void dtSemana_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        CancelarEdicao();

        await CarregarPartesDaSemanaAsync();
    }

    private async void SalvarParteSemana_Clicked(
        object sender,
        EventArgs e)
    {
        if (pickerParte.SelectedItem
            is not Parte parteSelecionada)
        {
            await DisplayAlert(
                "Aviso",
                "Selecione uma parte base.",
                "OK");

            return;
        }

        if (!int.TryParse(
                txtNumero.Text,
                out var numero) ||
            numero <= 0)
        {
            await DisplayAlert(
                "Aviso",
                "Informe um número válido para a parte.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(
                txtTitulo.Text))
        {
            await DisplayAlert(
                "Aviso",
                "Informe o título da parte.",
                "OK");

            return;
        }

        if (!int.TryParse(
                txtDuracao.Text,
                out var duracao) ||
            duracao < 0)
        {
            await DisplayAlert(
                "Aviso",
                "Informe uma duração válida.",
                "OK");

            return;
        }

        var dataSelecionada =
            ObterDataSelecionada();

        if (_parteSemanaEmEdicao == null)
        {
            var numeroJaExiste =
                await _database
                    .ParteSemanaExisteAsync(
                        dataSelecionada,
                        numero);

            if (numeroJaExiste)
            {
                await DisplayAlert(
                    "Parte já cadastrada",
                    $"Já existe uma parte número " +
                    $"{numero} para a semana " +
                    $"{dataSelecionada:dd/MM/yyyy}.",
                    "OK");

                return;
            }

            var novaParteSemana =
                new ParteSemana
                {
                    ParteId =
                        parteSelecionada.Id,

                    DataSemana =
                        dataSelecionada,

                    Numero =
                        numero,

                    Titulo =
                        txtTitulo.Text.Trim(),

                    Descricao =
                        txtDescricao.Text?.Trim()
                        ?? string.Empty,

                    DuracaoMinutos =
                        duracao,

                    UrlOrigem =
                        string.Empty
                };

            await _database
                .SalvarParteSemanaAsync(
                    novaParteSemana);

            await DisplayAlert(
                "Sucesso",
                "Parte semanal cadastrada com sucesso.",
                "OK");
        }
        else
        {
            _parteSemanaEmEdicao.ParteId =
                parteSelecionada.Id;

            _parteSemanaEmEdicao.DataSemana =
                dataSelecionada;

            _parteSemanaEmEdicao.Numero =
                numero;

            _parteSemanaEmEdicao.Titulo =
                txtTitulo.Text.Trim();

            _parteSemanaEmEdicao.Descricao =
                txtDescricao.Text?.Trim()
                ?? string.Empty;

            _parteSemanaEmEdicao.DuracaoMinutos =
                duracao;

            await _database
                .AtualizarParteSemanaAsync(
                    _parteSemanaEmEdicao);

            await DisplayAlert(
                "Sucesso",
                "Parte semanal atualizada com sucesso.",
                "OK");
        }

        LimparFormulario();

        await CarregarPartesDaSemanaAsync();
    }

    private void EditarParteSemana_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter
                is not ParteSemana parteSemana)
        {
            return;
        }

        _parteSemanaEmEdicao =
            parteSemana;

        txtNumero.Text =
            parteSemana.Numero.ToString();

        txtTitulo.Text =
            parteSemana.Titulo;

        txtDescricao.Text =
            parteSemana.Descricao;

        txtDuracao.Text =
            parteSemana
                .DuracaoMinutos
                .ToString();

        if (pickerParte.ItemsSource
            is IEnumerable<Parte> partes)
        {
            pickerParte.SelectedItem =
                partes.FirstOrDefault(
                    parte =>
                        parte.Id ==
                        parteSemana.ParteId);
        }

        btnSalvar.Text =
            "Atualizar Parte";

        btnCancelarEdicao.IsVisible =
            true;
    }

    private async void ExcluirParteSemana_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter
                is not ParteSemana parteSemana)
        {
            return;
        }

        var confirmar =
            await DisplayAlert(
                "Excluir parte",
                $"Deseja excluir a parte " +
                $"'{parteSemana.Titulo}' da semana " +
                $"{parteSemana.DataSemana:dd/MM/yyyy}?",
                "Sim",
                "Não");

        if (!confirmar)
            return;

        await _database
            .ExcluirParteSemanaAsync(
                parteSemana);

        if (_parteSemanaEmEdicao?.Id ==
            parteSemana.Id)
        {
            CancelarEdicao();
        }

        await CarregarPartesDaSemanaAsync();

        await DisplayAlert(
            "Sucesso",
            "Parte semanal excluída.",
            "OK");
    }

    private void CancelarEdicao_Clicked(
        object sender,
        EventArgs e)
    {
        CancelarEdicao();
    }

    private void CancelarEdicao()
    {
        _parteSemanaEmEdicao = null;

        LimparFormulario();
    }

    private void LimparFormulario()
    {
        pickerParte.SelectedItem = null;

        txtNumero.Text = string.Empty;
        txtTitulo.Text = string.Empty;
        txtDescricao.Text = string.Empty;
        txtDuracao.Text = string.Empty;

        btnSalvar.Text =
            "Salvar Parte";

        btnCancelarEdicao.IsVisible =
            false;

        _parteSemanaEmEdicao = null;
    }

    private async void ExcluirProgramacao_Clicked(
    object sender,
    EventArgs e)
    {
        var dataSelecionada =
            ObterDataSelecionada();

        var partesSemana =
            await _database.GetPartesDaSemanaAsync(
                dataSelecionada);

        if (partesSemana.Count == 0)
        {
            await DisplayAlert(
                "Aviso",
                "Não existe programação cadastrada " +
                "para esta semana.",
                "OK");

            btnExcluirProgramacao.IsEnabled =
                false;

            return;
        }

        var confirmar =
            await DisplayAlert(
                "Excluir programação",
                $"Deseja excluir toda a programação " +
                $"da semana {dataSelecionada:dd/MM/yyyy}?\n\n" +
                $"Serão excluídas {partesSemana.Count} partes.\n\n" +
                "Os participantes, as partes base e as " +
                "habilitações não serão excluídos.",
                "Excluir",
                "Cancelar");

        if (!confirmar)
            return;

        try
        {
            btnExcluirProgramacao.IsEnabled =
                false;

            var quantidadeExcluida =
                await _database
                    .ExcluirPartesDaSemanaAsync(
                        dataSelecionada);

            CancelarEdicao();

            await CarregarPartesDaSemanaAsync();

            await DisplayAlert(
                "Programação excluída",
                $"A programação de " +
                $"{dataSelecionada:dd/MM/yyyy} foi excluída.\n\n" +
                $"Partes removidas: {quantidadeExcluida}.",
                "OK");
        }
        catch (Exception ex)
        {
            btnExcluirProgramacao.IsEnabled =
                true;

            await DisplayAlert(
                "Erro",
                "Não foi possível excluir a programação." +
                $"\n\n{ex.Message}",
                "OK");
        }
    }
}