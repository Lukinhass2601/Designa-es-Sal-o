using SQLite;

namespace DesignacoesSR.Models;

public class Parte
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
}