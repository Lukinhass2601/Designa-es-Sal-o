using DesignacoesSR.Models;
using MiniExcelLibs;
using System.Collections;
using System.Dynamic;

namespace DesignacoesSR.Services;

public class ImportacaoExcelService
{
    private readonly DatabaseService _database;

    public ImportacaoExcelService(
        DatabaseService database)
    {
        _database = database;
    }

    public async Task<ResultadoImportacao> ImportarAsync(
        Stream arquivoExcel)
    {
        var resultado = new ResultadoImportacao();

        var linhas =
            await MiniExcel.QueryAsync(
                arquivoExcel,
                useHeaderRow: true,
                sheetName: "Importacao");

        foreach (var linha in linhas)
        {
            var dados =
                ConverterParaDicionario(linha);

            var nome = ObterTexto(dados, "Nome");

            if (string.IsNullOrWhiteSpace(nome))
                continue;

            var sexoPlanilha =
                ObterTexto(dados, "Sexo");

            var ativoPlanilha =
                ObterTexto(dados, "Ativo");

            var sexo =
                ConverterSexo(sexoPlanilha);

            if (string.IsNullOrWhiteSpace(sexo))
            {
                resultado.Avisos.Add(
                    $"{nome}: sexo inválido.");

                resultado.ParticipantesIgnorados++;
                continue;
            }

            var participante =
                await _database
                .GetParticipantePorNomeNormalizadoAsync(
                    nome);

            if (participante == null)
            {
                participante = new Participante
                {
                    Nome = FormatarNome(nome),
                    Sexo = sexo,
                    Ativo = ConverterSimNao(
                        ativoPlanilha,
                        valorPadrao: true)
                };

                await _database
                    .SalvarParticipanteAsync(
                        participante);

                resultado.ParticipantesAdicionados++;
            }
            else
            {
                participante.Sexo = sexo;

                participante.Ativo =
                    ConverterSimNao(
                        ativoPlanilha,
                        participante.Ativo);

                await _database
                    .AtualizarParticipanteAsync(
                        participante);

                resultado.ParticipantesAtualizados++;
            }

            var categoriasHabilitadas =
                ObterCategoriasHabilitadas(dados);

            foreach (var categoria in categoriasHabilitadas)
            {
                var nomesPartes =
                    ObterPartesCorrespondentes(categoria);

                foreach (var nomeParte in nomesPartes)
                {
                    var parte =
                        await _database
                        .GetPartePorNomeNormalizadoAsync(
                            nomeParte);

                    if (parte == null)
                    {
                        resultado.PartesNaoEncontradas++;

                        resultado.Avisos.Add(
                            $"{participante.Nome}: " +
                            $"a parte '{nomeParte}' não foi encontrada.");

                        continue;
                    }

                    var jaExiste =
                        await _database
                        .ParticipanteParteExisteAsync(
                            participante.Id,
                            parte.Id);

                    if (jaExiste)
                        continue;

                    await _database
                        .SalvarParticipanteParteAsync(
                            new ParticipanteParte
                            {
                                ParticipanteId =
                                    participante.Id,

                                ParteId =
                                    parte.Id
                            });

                    resultado.HabilitacoesAdicionadas++;
                }
            }
        }

        return resultado;
    }

    private static Dictionary<string, object?>
        ConverterParaDicionario(dynamic linha)
    {
        if (linha is IDictionary<string, object> dados)
        {
            return dados.ToDictionary(
                item => item.Key,
                item => (object?)item.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        if (linha is IDictionary<string, object?> dadosNullable)
        {
            return new Dictionary<string, object?>(
                dadosNullable,
                StringComparer.OrdinalIgnoreCase);
        }

        var resultado =
            new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var propriedade in
                 linha.GetType().GetProperties())
        {
            resultado[propriedade.Name] =
                propriedade.GetValue(linha);
        }

        return resultado;
    }

    private static string ObterTexto(
        Dictionary<string, object?> dados,
        string coluna)
    {
        var item =
            dados.FirstOrDefault(
                x => string.Equals(
                    x.Key.Trim(),
                    coluna,
                    StringComparison.OrdinalIgnoreCase));

        return item.Value?.ToString()?.Trim()
               ?? string.Empty;
    }

    private static List<string>
        ObterCategoriasHabilitadas(
            Dictionary<string, object?> dados)
    {
        var categorias =
            new[]
            {
                "LEITURA BIBLIA",
                "DISCURSO",
                "ESTUDANTE",
                "AJUDANTE",
                "PRESIDENTE",
                "ESTUDO",
                "TESOUROS",
                "JOIAS"
            };

        var selecionadas = new List<string>();

        foreach (var categoria in categorias)
        {
            var valor =
                ObterTexto(dados, categoria);

            if (ConverterSimNao(
                    valor,
                    valorPadrao: false))
            {
                selecionadas.Add(categoria);
            }
        }

        return selecionadas;
    }

    private static IEnumerable<string>
        ObterPartesCorrespondentes(
            string categoria)
    {
        /*
         * Ajuste os nomes abaixo para que fiquem
         * exatamente iguais aos nomes cadastrados
         * na página Partes do aplicativo.
         */

        var mapeamento =
            new Dictionary<string, string[]>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["LEITURA BIBLIA"] =
                    new[]
                    {
                        "Leitura Bíblica"
                    },

                ["DISCURSO"] =
                    new[]
                    {
                        "Discurso"
                    },

                ["PRESIDENTE"] =
                    new[]
                    {
                        "Presidente"
                    },

                ["ESTUDO"] =
                    new[]
                    {
                        "Estudo"
                    },

                ["TESOUROS"] =
                    new[]
                    {
                        "Tesouros da Palavra de Deus"
                    },

                ["JOIAS"] =
                    new[]
                    {
                        "Joias Espirituais"
                    },

                ["ESTUDANTE"] =
                    new[]
                    {
                        "Iniciando Conversas",
                        "Cultivando o Interesse",
                        "Fazendo Discípulos"
                    },

                ["AJUDANTE"] =
                    new[]
                    {
                        "Iniciando Conversas",
                        "Cultivando o Interesse",
                        "Fazendo Discípulos"
                    }
            };

        return mapeamento.TryGetValue(
            categoria,
            out var partes)
            ? partes
            : Array.Empty<string>();
    }

    private static string ConverterSexo(
        string valor)
    {
        var sexo =
            valor.Trim().ToUpperInvariant();

        return sexo switch
        {
            "M" => "M",
            "MASCULINO" => "M",
            "F" => "F",
            "FEMININO" => "F",
            _ => string.Empty
        };
    }

    private static bool ConverterSimNao(
        string valor,
        bool valorPadrao)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return valorPadrao;

        var texto =
            valor.Trim().ToUpperInvariant();

        return texto switch
        {
            "SIM" => true,
            "S" => true,
            "TRUE" => true,
            "VERDADEIRO" => true,
            "1" => true,

            "NÃO" => false,
            "NAO" => false,
            "N" => false,
            "FALSE" => false,
            "FALSO" => false,
            "0" => false,

            _ => valorPadrao
        };
    }

    private static string FormatarNome(
        string nome)
    {
        nome =
            string.Join(
                " ",
                nome.Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));

        return nome.ToUpperInvariant();
    }
}