using DesignacoesSR.Models;
using DesignacoesSR.Services;
using System.Globalization;
using System.Text;

namespace DesignacoesSR.Pages;

public partial class ParticipantesPage : ContentPage
{
    private readonly DatabaseService _database;

    private List<Participante> _todosParticipantes = new();

    public ParticipantesPage(
        DatabaseService database)
    {
        InitializeComponent();

        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarParticipantesAsync();
    }

    private async Task CarregarParticipantesAsync()
    {
        try
        {
            var participantes =
                await _database.GetParticipantesAsync();

            _todosParticipantes =
                participantes
                    .OrderBy(x => x.Nome)
                    .ToList();

            AplicarFiltro(
                campoPesquisa.Text);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível carregar os participantes." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private void CampoPesquisa_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        AplicarFiltro(
            e.NewTextValue);
    }

    private void CampoPesquisa_SearchButtonPressed(
        object sender,
        EventArgs e)
    {
        AplicarFiltro(
            campoPesquisa.Text);

        campoPesquisa.Unfocus();
    }

    private void AplicarFiltro(
        string? textoPesquisa)
    {
        var pesquisa =
            NormalizarTexto(textoPesquisa);

        List<Participante> participantesFiltrados;

        if (string.IsNullOrWhiteSpace(pesquisa))
        {
            participantesFiltrados =
                _todosParticipantes
                    .OrderBy(x => x.Nome)
                    .ToList();
        }
        else
        {
            participantesFiltrados =
                _todosParticipantes
                    .Where(
                        participante =>
                            NormalizarTexto(
                                participante.Nome)
                            .Contains(pesquisa))
                    .OrderBy(x => x.Nome)
                    .ToList();
        }

        listaParticipantes.ItemsSource = null;

        listaParticipantes.ItemsSource =
            participantesFiltrados;
    }

    private async void EditarParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter
            is not Participante participante)
        {
            return;
        }

        var novoNome =
            await DisplayPromptAsync(
                "Editar participante",
                "Digite o nome do participante:",
                accept: "Salvar",
                cancel: "Cancelar",
                placeholder: "Nome do participante",
                maxLength: 120,
                keyboard: Keyboard.Text,
                initialValue: participante.Nome);

        if (string.IsNullOrWhiteSpace(novoNome))
            return;

        novoNome =
            string.Join(
                " ",
                novoNome
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

        var participanteComMesmoNome =
            _todosParticipantes
                .FirstOrDefault(
                    x =>
                        x.Id != participante.Id &&
                        NormalizarTexto(x.Nome) ==
                        NormalizarTexto(novoNome));

        if (participanteComMesmoNome != null)
        {
            await DisplayAlert(
                "Nome já cadastrado",
                "Já existe outro participante com esse nome.",
                "OK");

            return;
        }

        participante.Nome =
            novoNome.ToUpperInvariant();

        try
        {
            await _database
                .AtualizarParticipanteAsync(
                    participante);

            await CarregarParticipantesAsync();

            await DisplayAlert(
                "Sucesso",
                "Participante atualizado com sucesso.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível atualizar o participante." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private async void AlterarGrupoParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter
            is not Participante participante)
        {
            return;
        }

        var opcao =
            await DisplayActionSheet(
                $"Grupo de {participante.Nome}",
                "Cancelar",
                null,
                "Ancião",
                "Servo Ministerial",
                "Remover do grupo");

        if (string.IsNullOrWhiteSpace(opcao))
            return;

        if (opcao == "Cancelar")
            return;

        participante.Grupo =
            opcao switch
            {
                "Ancião" => "ANCIAO",
                "Servo Ministerial" => "SERVO",
                _ => string.Empty
            };

        try
        {
            await _database
                .AtualizarParticipanteAsync(
                    participante);

            await CarregarParticipantesAsync();

            var grupoExibicao =
                participante.Grupo switch
                {
                    "ANCIAO" => "Ancião",
                    "SERVO" => "Servo Ministerial",
                    _ => "Sem grupo especial"
                };

            await DisplayAlert(
                "Grupo atualizado",
                $"{participante.Nome} foi classificado como " +
                $"{grupoExibicao}.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível alterar o grupo." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private async void AlternarStatusParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter
            is not Participante participante)
        {
            return;
        }

        var novoStatus =
            !participante.Ativo;

        var acao =
            novoStatus
                ? "ativar"
                : "inativar";

        var confirmar =
            await DisplayAlert(
                "Alterar status",
                $"Deseja {acao} o participante " +
                $"{participante.Nome}?",
                "Sim",
                "Não");

        if (!confirmar)
            return;

        participante.Ativo =
            novoStatus;

        try
        {
            await _database
                .AtualizarParticipanteAsync(
                    participante);

            await CarregarParticipantesAsync();

            var status =
                participante.Ativo
                    ? "ativado"
                    : "inativado";

            await DisplayAlert(
                "Status atualizado",
                $"{participante.Nome} foi {status}.",
                "OK");
        }
        catch (Exception ex)
        {
            participante.Ativo =
                !novoStatus;

            await DisplayAlert(
                "Erro",
                "Não foi possível alterar o status." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private async void ExcluirParticipante_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.CommandParameter
            is not Participante participante)
        {
            return;
        }

        var confirmar =
            await DisplayAlert(
                "Excluir participante",
                $"Deseja excluir o participante " +
                $"'{participante.Nome}'?" +
                Environment.NewLine +
                Environment.NewLine +
                "As habilitações desse participante " +
                "também serão removidas.",
                "Excluir",
                "Cancelar");

        if (!confirmar)
            return;

        try
        {
            await _database
                .RemoverHabilitacoesParticipanteAsync(
                    participante.Id);

            await _database
                .ExcluirParticipanteAsync(
                    participante);

            await CarregarParticipantesAsync();

            await DisplayAlert(
                "Sucesso",
                "Participante excluído com sucesso.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro",
                "Não foi possível excluir o participante." +
                Environment.NewLine +
                Environment.NewLine +
                ex.Message,
                "OK");
        }
    }

    private static string NormalizarTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var textoSemEspacosExtras =
            string.Join(
                " ",
                texto
                    .Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

        var textoDecomposto =
            textoSemEspacosExtras
                .ToUpperInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var caracteresSemAcentos =
            textoDecomposto
                .Where(
                    caractere =>
                        CharUnicodeInfo
                            .GetUnicodeCategory(
                                caractere) !=
                        UnicodeCategory.NonSpacingMark)
                .ToArray();

        return new string(
                caracteresSemAcentos)
            .Normalize(
                NormalizationForm.FormC);
    }
}