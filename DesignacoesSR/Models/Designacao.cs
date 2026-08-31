using SQLite;

namespace DesignacoesSR.Models;

public class Designacao
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime DataSemana { get; set; }

    public int ParteId { get; set; }

    public int ParteSemanaId { get; set; }

    public int Numero { get; set; }

    public string Parte { get; set; } =
        string.Empty;

    public string Participante { get; set; } =
        string.Empty;
}
