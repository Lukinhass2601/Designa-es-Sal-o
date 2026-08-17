using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class CadastroPage : ContentPage
{
    private readonly DatabaseService _database;

    public CadastroPage(DatabaseService database)
    {
        InitializeComponent();
        _database = database;
    }

    private async void SalvarParticipante_Clicked(
    object sender,
    EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtParticipante.Text))
            return;

        await _database.SalvarParticipanteAsync(
new Participante
{
    Nome = txtParticipante.Text,
    Sexo = pickerSexoParticipante.SelectedIndex == 0
        ? "M"
        : "F"
});

        txtParticipante.Text = string.Empty;

        await DisplayAlert(
            "Sucesso",
            "Participante salvo com sucesso.",
            "OK");
    }

    private async void SalvarParte_Clicked(
    object sender,
    EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtParte.Text))
            return;

        await _database.SalvarParteAsync(
            new Parte
            {
                Nome = txtParte.Text,

                QuantidadeParticipantes =
        pickerQuantidade.SelectedIndex == 1 ? 2 : 1,

                SexoPermitido =
        pickerSexoParte.SelectedIndex == 0
            ? "M"
            : "F"
            });

        txtParte.Text = string.Empty;

        await DisplayAlert(
            "Sucesso",
            "Parte salva com sucesso.",
            "OK");
    }
    private async void ExcluirParticipante_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;
        var participante = (Participante)button.CommandParameter;

        bool excluir = await DisplayAlert(
            "Confirmação",
            $"Excluir {participante.Nome}?",
            "Sim",
            "Não");

        if (!excluir)
            return;

        await _database.ExcluirParticipanteAsync(participante);
    }

    private async void EditarParticipante_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;
        var participante = (Participante)button.CommandParameter;

        string novoNome = await DisplayPromptAsync(
            "Editar",
            "Nome do participante:",
            initialValue: participante.Nome);

        if (string.IsNullOrWhiteSpace(novoNome))
            return;

        participante.Nome = novoNome;

        await _database.AtualizarParticipanteAsync(participante);
    }
    private async void EditarParte_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;
        var parte = (Parte)button.CommandParameter;

        string novoNome = await DisplayPromptAsync(
            "Editar Parte",
            "Digite o novo nome:",
            initialValue: parte.Nome);

        if (string.IsNullOrWhiteSpace(novoNome))
            return;

        parte.Nome = novoNome;

        await _database.AtualizarParteAsync(parte);
    }
    private async void ExcluirParte_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;
        var parte = (Parte)button.CommandParameter;

        bool confirmar = await DisplayAlert(
            "Excluir",
            $"Deseja excluir a parte '{parte.Nome}'?",
            "Sim",
            "Não");

        if (!confirmar)
            return;

        await _database.ExcluirParteAsync(parte);
    }

    private async void AlternarStatusParticipante_Clicked(
    object sender,
    EventArgs e)
    {
        var button = (Button)sender;

        var participante =
            (Participante)button.CommandParameter;

        participante.Ativo = !participante.Ativo;

        await _database.AtualizarParticipanteAsync(
            participante);
    }
}