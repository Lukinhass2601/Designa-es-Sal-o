using SQLite;

namespace DesignacoesSR.Models;

public class Participante
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Sexo { get; set; } = "M";

    public bool Ativo { get; set; } = true;

    public DateTime? UltimaParticipacao { get; set; }

    
}