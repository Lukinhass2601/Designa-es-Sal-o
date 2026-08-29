using SQLite;

namespace DesignacoesSR.Models;

public class ParteSemana
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Liga a parte semanal à parte base cadastrada
    public int ParteId { get; set; }

    // Data usada para identificar a semana
    public DateTime DataSemana { get; set; }

    // Número da parte na programação
    public int Numero { get; set; }

    // Título publicado para aquela semana
    public string Titulo { get; set; } = string.Empty;

    // Informações adicionais da parte
    public string Descricao { get; set; } = string.Empty;

    // Duração prevista
    public int DuracaoMinutos { get; set; }

    // Endereço da página de onde a parte foi importada
    public string UrlOrigem { get; set; } = string.Empty;
}