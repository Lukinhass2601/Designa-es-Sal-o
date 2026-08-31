using SQLite;

namespace DesignacoesSR.Models;

public class DesignacaoParticipante
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ParticipanteId { get; set; }

    public int ParteId { get; set; }

    public int ParteSemanaId { get; set; }

    public DateTime DataSemana { get; set; }

    public int Posicao { get; set; }
}