using SQLite;

namespace DesignacoesSR.Models;

public class Parte
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int QuantidadeParticipantes { get; set; } = 1;

    public string SexoPermitido { get; set; } = "M";
}
