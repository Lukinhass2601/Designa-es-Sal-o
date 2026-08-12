using SQLite;

namespace DesignacoesSR.Models;

public class Participante
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}