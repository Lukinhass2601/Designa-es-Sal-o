using SQLite;

namespace DesignacoesSR.Models;

public class ParticipanteParte
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ParticipanteId { get; set; }

    public int ParteId { get; set; }
}