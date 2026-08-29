namespace DesignacoesSR.Models;

public class DesignacaoResultado
{
    public int ParteSemanaId { get; set; }

    public int ParteId { get; set; }

    public int Numero { get; set; }

    public string Parte { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public int DuracaoMinutos { get; set; }

    public string Participante1 { get; set; } = string.Empty;

    public string Participante2 { get; set; } = string.Empty;
}