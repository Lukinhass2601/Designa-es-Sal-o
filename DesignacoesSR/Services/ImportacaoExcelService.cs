using DesignacoesSR.Models;
using MiniExcelLibs;
using System.Collections;
using System.Globalization;
using System.Text;

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

        var linhas = await MiniExcel.QueryAsync(
            arquivoExcel,
            useHeaderRow: true,
            sheetName: "Importacao");

        foreach (var linha in linhas)
        {
            var dados = ConverterParaDicionario(linha);

            var nome = ObterTexto(
                dados,
                "Nome");

            var sexoPlanilha = ObterTexto(
                dados,
                "Sexo");

            var ativoPlanilha = ObterTexto(
                dados,
                "Ativo");

            if (string.IsNullOrWhiteSpace(nome))
            {
                resultado.ParticipantesIgnorados++;
                continue;
            }

            var sexo = ConverterSexo(
                sexoPlanilha);

            if (string.IsNullOrWhiteSpace(sexo))
            {
                resultado.ParticipantesIgnorados++;

                resultado.Avisos.Add(
                    $"{nome}: sexo não reconhecido.");

                continue;
            }

            var participante = await _database
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
                        true)
                };

                await _database
                    .SalvarParticipanteAsync(
                        participante);

                resultado.ParticipantesAdicionados++;
            }
            else
            {
                participante.Nome = FormatarNome(
                    nome);

                participante.Sexo = sexo;

                participante.Ativo = ConverterSimNao(
                    ativoPlanilha,
                    participante.Ativo);

                await _database
                    .AtualizarParticipanteAsync(
                        participante);

                resultado.ParticipantesAtualizados++;
            }

            await ImportarHabilitacoesAsync(
                participante,
                dados,
                resultado);
        }

        return resultado;
    }

    private async Task ImportarHabilitacoesAsync(
        Participante participante,
        Dictionary<string, object?> dados,
        ResultadoImportacao resultado)
    {
        await _database
            .RemoverHabilitacoesParticipanteAsync(
                participante.Id);

        var mapeamentos = ObterMapeamentos();

        foreach (var mapeamento in mapeamentos)
        {
            var valorPlanilha = ObterTexto(
                dados,
                mapeamento.ColunaPlanilha);

            var habilitado = ConverterSimNao(
                valorPlanilha,
                false);

            if (!habilitado)
                continue;

            var parte = await LocalizarParteAsync(
                mapeamento.NomesPossiveisParte);

            if (parte == null)
            {
                resultado.PartesNaoEncontradas++;

                resultado.Avisos.Add(
                    $"{participante.Nome}: " +
                    $"não foi encontrada uma parte para " +
                    $"a coluna '{mapeamento.ColunaPlanilha}'.");

                continue;
            }

            var relacionamentoExiste = await _database
                .ParticipanteParteExisteAsync(
                    participante.Id,
                    parte.Id);

            if (relacionamentoExiste)
                continue;

            await _database
                .SalvarParticipanteParteAsync(
                    new ParticipanteParte
                    {
                        ParticipanteId = participante.Id,
                        ParteId = parte.Id
                    });

            resultado.HabilitacoesAdicionadas++;
        }
    }

    private async Task<Parte?> LocalizarParteAsync(
        IEnumerable<string> nomesPossiveis)
    {
        foreach (var nomeParte in nomesPossiveis)
        {
            var parte = await _database
                .GetPartePorNomeNormalizadoAsync(
                    nomeParte);

            if (parte != null)
                return parte;
        }

        return null;
    }

    private static List<MapeamentoHabilitacao>
        ObterMapeamentos()
    {
        return new List<MapeamentoHabilitacao>
        {
            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "Leitura da Bíblia",

                NomesPossiveisParte = new[]
                {
                    "Leitura da Bíblia",
                    "Leitura Bíblia",
                    "Leitura Bíblica"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "DISCURSO",

                NomesPossiveisParte = new[]
                {
                    "Discurso",
                    "Explicando suas Crenças"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "Iniciando Conversas",

                NomesPossiveisParte = new[]
                {
                    "Iniciando Conversas",
                    "Iniciar Conversas",
                    "Começar Conversas"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "Fazendo Discípulos",

                NomesPossiveisParte = new[]
                {
                    "Fazendo Discípulos",
                    "Fazer Discípulos"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "Cultivando o interesse",

                NomesPossiveisParte = new[]
                {
                    "Cultivando o Interesse",
                    "Manter o Interesse"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "PRESIDENTE",

                NomesPossiveisParte = new[]
                {
                    "Presidente"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha =
                    "Estudo bíblico de congregação",

                NomesPossiveisParte = new[]
                {
                    "Estudo Bíblico de Congregação",
                    "Estudo Bíblico",
                    "Estudo"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "TESOUROS",

                NomesPossiveisParte = new[]
                {
                    "Tesouros",
                    "Tesouros da Palavra de Deus"
                }
            },

            new MapeamentoHabilitacao
            {
                ColunaPlanilha = "Joias espirituais",

                NomesPossiveisParte = new[]
                {
                    "Joias Espirituais",
                    "Joias",
                    "Pérolas Espirituais"
                }
            }
        };
    }

    private static Dictionary<string, object?>
        ConverterParaDicionario(
            object linha)
    {
        var resultado =
            new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

        if (linha is IDictionary<string, object> dados)
        {
            foreach (var item in dados)
            {
                resultado[item.Key] = item.Value;
            }

            return resultado;
        }

        if (linha is IDictionary dicionario)
        {
            foreach (DictionaryEntry item in dicionario)
            {
                var chave = item.Key?.ToString();

                if (string.IsNullOrWhiteSpace(chave))
                    continue;

                resultado[chave] = item.Value;
            }

            return resultado;
        }

        var propriedades = linha
            .GetType()
            .GetProperties();

        foreach (var propriedade in propriedades)
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
        var colunaNormalizada = NormalizarTexto(
            coluna);

        foreach (var item in dados)
        {
            var cabecalhoNormalizado =
                NormalizarTexto(item.Key);

            if (cabecalhoNormalizado !=
                colunaNormalizada)
            {
                continue;
            }

            return item.Value?
                       .ToString()?
                       .Trim()
                   ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ConverterSexo(
        string valor)
    {
        var sexo = NormalizarTexto(valor);

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

        var texto = NormalizarTexto(valor);

        return texto switch
        {
            "SIM" => true,
            "S" => true,
            "TRUE" => true,
            "VERDADEIRO" => true,
            "1" => true,
            "X" => true,

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
        return string.Join(
            " ",
            nome.Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static string NormalizarTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var textoSemEspacosExtras = string.Join(
            " ",
            texto.Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));

        var textoDecomposto = textoSemEspacosExtras
            .ToUpperInvariant()
            .Normalize(
                NormalizationForm.FormD);

        var caracteresSemAcentos =
            textoDecomposto.Where(
                caractere =>
                    CharUnicodeInfo.GetUnicodeCategory(
                        caractere) !=
                    UnicodeCategory.NonSpacingMark);

        return new string(
                caracteresSemAcentos.ToArray())
            .Normalize(
                NormalizationForm.FormC);
    }

    private class MapeamentoHabilitacao
    {
        public string ColunaPlanilha { get; set; } =
            string.Empty;

        public string[] NomesPossiveisParte { get; set; } =
            Array.Empty<string>();
    }
}