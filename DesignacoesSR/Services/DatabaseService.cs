using SQLite;
using DesignacoesSR.Models;

namespace DesignacoesSR.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        var dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "designacoes.db3");

        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        _database = new SQLiteAsyncConnection(dbPath);

        _database.CreateTableAsync<Participante>().Wait();
        _database.CreateTableAsync<Parte>().Wait();
        _database.CreateTableAsync<ProgramaSemanal>().Wait();
        _database.CreateTableAsync<Designacao>().Wait();
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
}