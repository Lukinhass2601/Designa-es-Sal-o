using DesignacoesSR.Models;
using DesignacoesSR.Services;

namespace DesignacoesSR.Pages;

public partial class CadastroPage : ContentPage
{
    private readonly DatabaseService _database;

    private readonly ImportacaoExcelService
    _importacaoExcelService;

    public CadastroPage(
    DatabaseService database,
    ImportacaoExcelService importacaoExcelService)
    {
        InitializeComponent();

        _database = database;

        _importacaoExcelService =
            importacaoExcelService;
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

    private async void ImportarExcel_Clicked(
    object sender,
    EventArgs e)
    {
        try
        {
            btnImportarExcel.IsEnabled = false;

            var tiposPermitidos =
                new FilePickerFileType(
                    new Dictionary<DevicePlatform,
                        IEnumerable<string>>
                    {
                    {
                        DevicePlatform.WinUI,
                        new[] { ".xlsx" }
                    },
                    {
                        DevicePlatform.Android,
                        new[]
                        {
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        }
                    },
                    {
                        DevicePlatform.iOS,
                        new[]
                        {
                            "org.openxmlformats.spreadsheetml.sheet"
                        }
                    },
                    {
                        DevicePlatform.MacCatalyst,
                        new[]
                        {
                            "org.openxmlformats.spreadsheetml.sheet"
                        }
                    }
                    });

            var arquivo =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Selecione a planilha de participantes",

                        FileTypes =
                            tiposPermitidos
                    });

            if (arquivo == null)
                return;

            if (!arquivo.FileName.EndsWith(
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert(
                    "Arquivo inválido",
                    "Selecione um arquivo no formato XLSX.",
                    "OK");

                return;
            }

            using var stream =
                await arquivo.OpenReadAsync();

            var resultado =
                await _importacaoExcelService
                    .ImportarAsync(stream);

            var mensagem =
                $"Participantes adicionados: " +
                $"{resultado.ParticipantesAdicionados}\n" +

                $"Participantes atualizados: " +
                $"{resultado.ParticipantesAtualizados}\n" +

                $"Habilitações adicionadas: " +
                $"{resultado.HabilitacoesAdicionadas}\n" +

                $"Partes não encontradas: " +
                $"{resultado.PartesNaoEncontradas}";

            if (resultado.Avisos.Any())
            {
                var primeirosAvisos =
                    resultado.Avisos
                    .Take(8);

                mensagem +=
                    "\n\nAvisos:\n" +
                    string.Join(
                        "\n",
                        primeirosAvisos);

                if (resultado.Avisos.Count > 8)
                {
                    mensagem +=
                        $"\n... e mais " +
                        $"{resultado.Avisos.Count - 8} avisos.";
                }
            }

            await DisplayAlert(
                "Importação concluída",
                mensagem,
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erro na importação",
                $"Não foi possível importar a planilha.\n\n" +
                $"{ex.Message}",
                "OK");
        }
        finally
        {
            btnImportarExcel.IsEnabled = true;
        }
    }
}