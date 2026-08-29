namespace DesignacoesSR.Models;

public class ParticipanteImportacao
{
    public string Nome { get; set; } = string.Empty;

    public string Sexo { get; set; } = string.Empty;

    public string Ativo { get; set; } = string.Empty;

    public List<string> CategoriasHabilitadas { get; set; } = new();
}