using SQLite;

namespace DesignacoesSR.Models;

public class ProgramaSemanal
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime DataSemana { get; set; }
}