using DesignacoesSR.Models;
using SQLite;
using System.Globalization;
using System.Text;


namespace DesignacoesSR.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "designacoes.db3");

        // campo temporário para não salvar os arquivos

        //if (File.Exists(dbPath))
        //{
        //    File.Delete(dbPath);
        //}

        _database = new SQLiteAsyncConnection(dbPath);

        _database.CreateTableAsync<Participante>().Wait();
        _database.CreateTableAsync<Parte>().Wait();
        _database.CreateTableAsync<ProgramaSemanal>().Wait();
        _database.CreateTableAsync<Designacao>().Wait();
        _database.CreateTableAsync<ParticipanteParte>().Wait();
        _database.CreateTableAsync<ParteSemana>().Wait();
        _database
    .CreateTableAsync<DesignacaoParticipante>()
    .Wait();
    }

    // PARTICIPANTES

    public Task<List<Participante>> GetParticipantesAsync()
        => _database.Table<Participante>().ToListAsync();

    public Task<int> SalvarParticipanteAsync(Participante participante)
        => _database.InsertAsync(participante);

    // PARTES

    public Task<List<Parte>> GetPartesAsync()
        => _database.Table<Parte>().ToListAsync();

    public Task<int> SalvarParteAsync(Parte parte)
        => _database.InsertAsync(parte);

    public Task<int> ExcluirParticipanteAsync(Participante participante)
    {
        return _database.DeleteAsync(participante);
    }

    public Task<int> ExcluirParteAsync(Parte parte)
    {
        return _database.DeleteAsync(parte);
    }

    public Task<int> AtualizarParticipanteAsync(
    Participante participante)
    {
        return _database.UpdateAsync(participante);
    }

    public Task<int> AtualizarParteAsync(Parte parte)
    {
        return _database.UpdateAsync(parte);
    }

    public Task<int> SalvarProgramaAsync(
    ProgramaSemanal programa)
    {
        return _database.InsertAsync(programa);
    }

    // DESIGNAÇÕES

    public Task<int> SalvarDesignacaoAsync(Designacao designacao)
    {
        return _database.InsertAsync(designacao);
    }

    public Task<List<Designacao>> GetDesignacoesAsync()
    {
        return _database.Table<Designacao>()
            .OrderByDescending(x => x.DataSemana)
            .ToListAsync();
    }

    public async Task<Participante?> GetParticipantePorNomeAsync(string nome)
    {
        return await _database.Table<Participante>()
            .Where(x => x.Nome == nome)
            .FirstOrDefaultAsync();
    }

    public Task<int> SalvarParticipanteParteAsync(
    ParticipanteParte participanteParte)
    {
        return _database.InsertAsync(participanteParte);
    }

    public Task<List<ParticipanteParte>> GetParticipantePartesAsync()
    {
        return _database.Table<ParticipanteParte>()
            .ToListAsync();
    }

    public Task<int> ExcluirParticipanteParteAsync(
    ParticipanteParte participanteParte)
    {
        return _database.DeleteAsync(participanteParte);
    }

    public async Task RemoverHabilitacoesParticipanteAsync(
    int participanteId)
    {
        var registros = await _database.Table<ParticipanteParte>()
            .Where(x => x.ParticipanteId == participanteId)
            .ToListAsync();

        foreach (var registro in registros)
        {
            await _database.DeleteAsync(registro);
        }
    }

    public Task<List<ParticipanteParte>>
    GetHabilitacoesParticipanteAsync(
        int participanteId)
    {
        return _database.Table<ParticipanteParte>()
            .Where(x => x.ParticipanteId == participanteId)
            .ToListAsync();
    }

    public async Task<List<Participante>> GetParticipantesPorParteAsync(
    int parteId)
    {
        var habilitacoes =
            await _database.Table<ParticipanteParte>()
            .Where(x => x.ParteId == parteId)
            .ToListAsync();

        var participantes = new List<Participante>();

        foreach (var habilitacao in habilitacoes)
        {
            var participante =
                await _database.Table<Participante>()
                .Where(x => x.Id == habilitacao.ParticipanteId)
                .FirstOrDefaultAsync();

            if (participante != null && participante.Ativo)
            {
                participantes.Add(participante);
            }
        }

        return participantes;
    }

    public async Task<int> GetQuantidadeDesignacoesAsync(
    string parte,
    string participante)
    {
        return await _database.Table<Designacao>()
            .Where(x =>
                x.Parte == parte &&
                x.Participante == participante)
            .CountAsync();
    }

    public async Task<List<string>>
    GetParticipantesJaUsadosNaParteAsync(
        string parte)
    {
        var designacoes =
            await _database.Table<Designacao>()
            .Where(x => x.Parte == parte)
            .ToListAsync();

        return designacoes
            .Select(x => x.Participante)
            .Distinct()
            .ToList();
    }

    public async Task<List<DateTime>> GetSemanasAsync()
    {
        var designacoes =
            await _database
                .Table<Designacao>()
                .ToListAsync();

        return designacoes
            .Select(x => x.DataSemana.Date)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();
    }

    public Task<List<Designacao>>
    GetDesignacoesSemanaAsync(
        DateTime dataSemana)
    {
        var inicio =
            dataSemana.Date;

        var fim =
            inicio.AddDays(1);

        return _database
            .Table<Designacao>()
            .Where(x =>
                x.DataSemana >= inicio &&
                x.DataSemana < fim)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task ExcluirSemanaAsync(
    DateTime dataSemana)
    {
        var inicio =
            dataSemana.Date;

        var fim =
            inicio.AddDays(1);

        var registros =
            await _database
                .Table<Designacao>()
                .Where(x =>
                    x.DataSemana >= inicio &&
                    x.DataSemana < fim)
                .ToListAsync();

        foreach (var registro in registros)
        {
            await _database.DeleteAsync(
                registro);
        }
    }

    public async Task<Participante?> GetParticipantePorNomeNormalizadoAsync(
    string nome)
    {
        var participantes =
            await _database.Table<Participante>()
            .ToListAsync();

        var nomeNormalizado = NormalizarTexto(nome);

        return participantes.FirstOrDefault(
            participante =>
                NormalizarTexto(participante.Nome) ==
                nomeNormalizado);
    }


    public async Task<Parte?> GetPartePorNomeNormalizadoAsync(
    string nome)
    {
        var partes =
            await _database.Table<Parte>()
            .ToListAsync();

        var nomeNormalizado = NormalizarTexto(nome);

        return partes.FirstOrDefault(
            parte =>
                NormalizarTexto(parte.Nome) ==
                nomeNormalizado);
    }

    public async Task<bool> ParticipanteParteExisteAsync(
    int participanteId,
    int parteId)
    {
        var registro =
            await _database.Table<ParticipanteParte>()
            .Where(x =>
                x.ParticipanteId == participanteId &&
                x.ParteId == parteId)
            .FirstOrDefaultAsync();

        return registro != null;
    }

    private static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var textoLimpo =
            string.Join(
                " ",
                texto.Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

        var normalizado =
            textoLimpo
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);

        var caracteres =
            normalizado.Where(
                caractere =>
                    CharUnicodeInfo.GetUnicodeCategory(caractere) !=
                    UnicodeCategory.NonSpacingMark);

        return new string(caracteres.ToArray())
            .Normalize(NormalizationForm.FormC);
    }

    public Task<int> SalvarParteSemanaAsync(
    ParteSemana parteSemana)
    {
        parteSemana.DataSemana =
            parteSemana.DataSemana.Date;

        return _database.InsertAsync(parteSemana);
    }

    public Task<int> AtualizarParteSemanaAsync(
    ParteSemana parteSemana)
    {
        parteSemana.DataSemana =
            parteSemana.DataSemana.Date;

        return _database.UpdateAsync(parteSemana);
    }

    public Task<int> ExcluirParteSemanaAsync(
    ParteSemana parteSemana)
    {
        return _database.DeleteAsync(parteSemana);
    }

    public Task<List<ParteSemana>>
    GetTodasPartesSemanaAsync()
    {
        return _database
            .Table<ParteSemana>()
            .OrderBy(x => x.DataSemana)
            .ThenBy(x => x.Numero)
            .ToListAsync();
    }

    public Task<List<ParteSemana>>
    GetPartesDaSemanaAsync(
        DateTime dataSemana)
    {
        var inicio = dataSemana.Date;

        var fim = inicio.AddDays(1);

        return _database
            .Table<ParteSemana>()
            .Where(x =>
                x.DataSemana >= inicio &&
                x.DataSemana < fim)
            .OrderBy(x => x.Numero)
            .ToListAsync();
    }

    public async Task<List<DateTime>>
    GetSemanasComPartesAsync()
    {
        var registros =
            await _database
                .Table<ParteSemana>()
                .ToListAsync();

        return registros
            .Select(x => x.DataSemana.Date)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<int>
    ExcluirPartesDaSemanaAsync(
        DateTime dataSemana)
    {
        var inicio = dataSemana.Date;

        var fim = inicio.AddDays(1);

        var registros =
            await _database
                .Table<ParteSemana>()
                .Where(x =>
                    x.DataSemana >= inicio &&
                    x.DataSemana < fim)
                .ToListAsync();

        var quantidadeExcluida = 0;

        foreach (var registro in registros)
        {
            quantidadeExcluida +=
                await _database.DeleteAsync(registro);
        }

        return quantidadeExcluida;
    }

    public async Task<bool>
    ParteSemanaExisteAsync(
        DateTime dataSemana,
        int numero)
    {
        var inicio = dataSemana.Date;

        var fim = inicio.AddDays(1);

        var registro =
            await _database
                .Table<ParteSemana>()
                .Where(x =>
                    x.DataSemana >= inicio &&
                    x.DataSemana < fim &&
                    x.Numero == numero)
                .FirstOrDefaultAsync();

        return registro != null;
    }

    public async Task SubstituirPartesDaSemanaAsync(
    DateTime dataSemana,
    List<ParteSemana> novasPartes)
    {
        await ExcluirPartesDaSemanaAsync(
            dataSemana);

        foreach (var parte in novasPartes)
        {
            parte.Id = 0;
            parte.DataSemana = dataSemana.Date;

            await _database.InsertAsync(parte);
        }
    }

    public Task<Parte?> GetPartePorIdAsync(
    int parteId)
    {
        return _database
            .Table<Parte>()
            .Where(x => x.Id == parteId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExisteProgramaNaSemanaAsync(
    DateTime dataSemana)
    {
        var inicio = dataSemana.Date;
        var fim = inicio.AddDays(1);

        var registro = await _database
            .Table<Designacao>()
            .Where(x =>
                x.DataSemana >= inicio &&
                x.DataSemana < fim)
            .FirstOrDefaultAsync();

        return registro != null;
    }

    public async Task<int> ExcluirDesignacoesDaSemanaAsync(
    DateTime dataSemana)
    {
        var inicio = dataSemana.Date;
        var fim = inicio.AddDays(1);

        var registros = await _database
            .Table<Designacao>()
            .Where(x =>
                x.DataSemana >= inicio &&
                x.DataSemana < fim)
            .ToListAsync();

        var quantidadeExcluida = 0;

        foreach (var registro in registros)
        {
            quantidadeExcluida +=
                await _database.DeleteAsync(registro);
        }

        return quantidadeExcluida;
    }

    public async Task<int> GetQuantidadeDesignacoesPorParteAsync(
    int parteId,
    string nomeParticipante)
    {
        var designacoes =
            await _database
                .Table<Designacao>()
                .Where(x => x.ParteId == parteId)
                .ToListAsync();

        var quantidade = 0;

        foreach (var designacao in designacoes)
        {
            var nomes = designacao.Participante
                .Split(
                    " e ",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            if (nomes.Any(nome =>
                    string.Equals(
                        nome,
                        nomeParticipante,
                        StringComparison.OrdinalIgnoreCase)))
            {
                quantidade++;
            }
        }

        return quantidade;
    }

    public async Task ExcluirParteCompletaAsync(
    int parteId)
    {
        var habilitacoes =
            await _database
                .Table<ParticipanteParte>()
                .Where(x => x.ParteId == parteId)
                .ToListAsync();

        foreach (var habilitacao in habilitacoes)
        {
            await _database.DeleteAsync(
                habilitacao);
        }

        var partesSemana =
            await _database
                .Table<ParteSemana>()
                .Where(x => x.ParteId == parteId)
                .ToListAsync();

        foreach (var parteSemana in partesSemana)
        {
            await _database.DeleteAsync(
                parteSemana);
        }

        var parte =
            await _database
                .Table<Parte>()
                .Where(x => x.Id == parteId)
                .FirstOrDefaultAsync();

        if (parte != null)
        {
            await _database.DeleteAsync(
                parte);
        }
    }

    public async Task<List<Participante>>
    GetAnciaosAsync()
    {
        var participantes =
            await _database
                .Table<Participante>()
                .Where(x =>
                    x.Ativo &&
                    x.Grupo == "ANCIAO")
                .ToListAsync();

        return participantes
            .OrderBy(x => x.Nome)
            .ToList();
    }


    public async Task<List<Participante>>
    GetServosAsync()
    {
        var participantes =
            await _database
                .Table<Participante>()
                .Where(x =>
                    x.Ativo &&
                    x.Grupo == "SERVO")
                .ToListAsync();

        return participantes
            .OrderBy(x => x.Nome)
            .ToList();
    }

    public async Task<List<Participante>>
    GetAnciaosEServosAsync()
    {
        var participantes =
            await _database
                .Table<Participante>()
                .Where(x =>
                    x.Ativo &&
                    (
                        x.Grupo == "ANCIAO" ||
                        x.Grupo == "SERVO"
                    ))
                .ToListAsync();

        return participantes
            .OrderBy(x => x.Nome)
            .ToList();
    }

    public async Task<int>
    HabilitarGrupoParaParteAsync(
        int parteId,
        string grupo)
    {
        List<Participante> participantes;

        if (grupo == "ANCIAO")
        {
            participantes =
                await GetAnciaosAsync();
        }
        else if (grupo == "SERVO")
        {
            participantes =
                await GetServosAsync();
        }
        else if (grupo == "ANCIAOS_E_SERVOS")
        {
            participantes =
                await GetAnciaosEServosAsync();
        }
        else
        {
            return 0;
        }

        var quantidadeAdicionada = 0;

        foreach (var participante in participantes)
        {
            var relacionamentoExiste =
                await ParticipanteParteExisteAsync(
                    participante.Id,
                    parteId);

            if (relacionamentoExiste)
                continue;

            await SalvarParticipanteParteAsync(
                new ParticipanteParte
                {
                    ParticipanteId =
                        participante.Id,

                    ParteId =
                        parteId
                });

            quantidadeAdicionada++;
        }

        return quantidadeAdicionada;
    }

    public async Task<HashSet<int>>
    GetParticipantesUsadosNosUltimosMesesAsync(
        DateTime dataSemana,
        int quantidadeMeses)
    {
        var dataFinal =
            dataSemana.Date;

        var dataInicial =
            dataFinal.AddMonths(
                -quantidadeMeses);

        var designacoes =
            await _database
                .Table<Designacao>()
                .Where(x =>
                    x.DataSemana >= dataInicial &&
                    x.DataSemana < dataFinal)
                .ToListAsync();

        var participantes =
            await _database
                .Table<Participante>()
                .ToListAsync();

        var participantesUsados =
            new HashSet<int>();

        foreach (var designacao in designacoes)
        {
            if (string.IsNullOrWhiteSpace(
                    designacao.Participante))
            {
                continue;
            }

            var nomes =
                designacao.Participante.Split(
                    " e ",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            foreach (var nome in nomes)
            {
                var participante =
                    participantes.FirstOrDefault(
                        x =>
                            NormalizarNomeParaComparacao(
                                x.Nome) ==
                            NormalizarNomeParaComparacao(
                                nome));

                if (participante != null)
                {
                    participantesUsados.Add(
                        participante.Id);
                }
            }
        }

        return participantesUsados;
    }

    private static string
    NormalizarNomeParaComparacao(
        string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        var nomeLimpo =
            string.Join(
                " ",
                nome.Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

        var nomeDecomposto =
            nomeLimpo
                .ToUpperInvariant()
                .Normalize(
                    System.Text.NormalizationForm.FormD);

        var caracteres =
            nomeDecomposto
                .Where(
                    caractere =>
                        System.Globalization
                            .CharUnicodeInfo
                            .GetUnicodeCategory(
                                caractere) !=
                        System.Globalization
                            .UnicodeCategory
                            .NonSpacingMark)
                .ToArray();

        return new string(caracteres)
            .Normalize(
                System.Text.NormalizationForm.FormC);
    }
    public async Task<List<Participante>>
    OrdenarParticipantesPorRodizioAsync(
        List<Participante> participantes,
        int parteId)
    {
        var historico =
            await _database
                .Table<DesignacaoParticipante>()
                .Where(x => x.ParteId == parteId)
                .ToListAsync();

        var candidatos =
            participantes
                .Select(
                    participante =>
                        new
                        {
                            Participante = participante,

                            Quantidade = historico.Count(
                                x =>
                                    x.ParticipanteId ==
                                    participante.Id),

                            UltimaData = historico
                                .Where(
                                    x =>
                                        x.ParticipanteId ==
                                        participante.Id)
                                .Select(x => x.DataSemana)
                                .DefaultIfEmpty(DateTime.MinValue)
                                .Max()
                        })
                .OrderBy(x => x.Quantidade)
                .ThenBy(x => x.UltimaData)
                .ThenBy(x => Guid.NewGuid())
                .Select(x => x.Participante)
                .ToList();

        return candidatos;
    }
    public Task<int> SalvarDesignacaoParticipanteAsync(
    DesignacaoParticipante registro)
    {
        registro.DataSemana =
            registro.DataSemana.Date;

        return _database.InsertAsync(
            registro);
    }
    public async Task<int>
    ExcluirDesignacoesParticipantesDaSemanaAsync(
        DateTime dataSemana)
    {
        var inicio =
            dataSemana.Date;

        var fim =
            inicio.AddDays(1);

        var registros =
            await _database
                .Table<DesignacaoParticipante>()
                .Where(x =>
                    x.DataSemana >= inicio &&
                    x.DataSemana < fim)
                .ToListAsync();

        var quantidadeExcluida =
            0;

        foreach (var registro in registros)
        {
            quantidadeExcluida +=
                await _database.DeleteAsync(
                    registro);
        }

        return quantidadeExcluida;
    }
    public async Task<List<ItemRelatorioSemana>>
    GetItensRelatorioSemanaAsync(
        DateTime dataSemana)
    {
        var partesSemana =
            await GetPartesDaSemanaAsync(
                dataSemana);

        var designacoes =
            await GetDesignacoesSemanaAsync(
                dataSemana);

        var resultado =
            new List<ItemRelatorioSemana>();

        foreach (var parteSemana in
                 partesSemana.OrderBy(x => x.Numero))
        {
            var designacao =
                designacoes.FirstOrDefault(
                    x =>
                        x.ParteSemanaId ==
                        parteSemana.Id);

            if (designacao == null)
            {
                designacao =
                    designacoes.FirstOrDefault(
                        x =>
                            x.Numero ==
                            parteSemana.Numero &&
                            string.Equals(
                                x.Parte,
                                parteSemana.Titulo,
                                StringComparison.OrdinalIgnoreCase));
            }

            resultado.Add(
                new ItemRelatorioSemana
                {
                    Numero =
                        parteSemana.Numero,

                    ParteId =
                        parteSemana.ParteId,

                    ParteSemanaId =
                        parteSemana.Id,

                    Titulo =
                        parteSemana.Titulo,

                    Descricao =
                        parteSemana.Descricao,

                    DuracaoMinutos =
                        parteSemana.DuracaoMinutos,

                    Participante =
                        designacao?.Participante
                        ?? string.Empty
                });
        }

        return resultado;
    }

}