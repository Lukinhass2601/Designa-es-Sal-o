namespace DesignacoesSR.Models;

public class ResultadoImportacao
{
    public int ParticipantesAdicionados { get; set; }

    public int ParticipantesAtualizados { get; set; }

    public int ParticipantesIgnorados { get; set; }

    public int HabilitacoesAdicionadas { get; set; }

    public int PartesNaoEncontradas { get; set; }

    public List<string> Avisos { get; set; } = new();
}
