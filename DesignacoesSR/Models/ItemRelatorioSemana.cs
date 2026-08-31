namespace DesignacoesSR.Models;

public class ItemRelatorioSemana
{
    public int Numero { get; set; }

    public int ParteId { get; set; }

    public int ParteSemanaId { get; set; }

    public string Titulo { get; set; } =
        string.Empty;

    public string Descricao { get; set; } =
        string.Empty;

    public int DuracaoMinutos { get; set; }

    public string Participante { get; set; } =
        string.Empty;
}