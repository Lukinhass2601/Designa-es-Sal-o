namespace DesignacoesSR.Models;

public class ParteSemanaImportacao
{
    public int Numero { get; set; }

    public int ParteBaseId { get; set; }

    public string TituloOriginal { get; set; } =
        string.Empty;

    public string NomeParteBase { get; set; } =
        string.Empty;

    public string Descricao { get; set; } =
        string.Empty;

    public int DuracaoMinutos { get; set; }

    public DateTime DataSemana { get; set; }

    public string UrlOrigem { get; set; } =
        string.Empty;

    public bool Selecionado { get; set; } = true;

    public bool ParteBaseEncontrada { get; set; }

    public bool PodeAdicionarParte
    {
        get
        {
            return !ParteBaseEncontrada;
        }
    }

    public string Status
    {
        get
        {
            if (ParteBaseEncontrada)
            {
                return $"Relacionada com: {NomeParteBase}";
            }

            return "Parte base não encontrada";
        }
    }
}