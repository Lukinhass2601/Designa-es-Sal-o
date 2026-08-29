using DesignacoesSR.Models;
using HtmlAgilityPack;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DesignacoesSR.Services;

public class JwProgramacaoService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseService _database;

    public JwProgramacaoService(
        HttpClient httpClient,
        DatabaseService database)
    {
        _httpClient = httpClient;
        _database = database;
    }

    public async Task<List<ParteSemanaImportacao>>
        BuscarProgramacaoAsync(
            string url,
            DateTime dataSemana)
    {
        ValidarUrl(url);

        using var requisicao =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        requisicao.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 " +
            "(Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 " +
            "Chrome/140.0 Safari/537.36");

        requisicao.Headers.AcceptLanguage.ParseAdd(
            "pt-BR,pt;q=0.9");

        using var resposta =
            await _httpClient.SendAsync(
                requisicao);

        resposta.EnsureSuccessStatusCode();

        var html =
            await resposta.Content
                .ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException(
                "A página não retornou conteúdo.");
        }

        var documento = new HtmlDocument();

        documento.LoadHtml(html);

        var titulos =
            documento.DocumentNode
                .SelectNodes("//h3");

        if (titulos == null ||
            titulos.Count == 0)
        {
            throw new InvalidOperationException(
                "Nenhuma parte numerada foi encontrada " +
                "na página informada.");
        }

        var partesBase =
            await _database.GetPartesAsync();

        var resultado =
            new List<ParteSemanaImportacao>();

        foreach (var tituloNode in titulos)
        {
            var tituloCompleto =
                LimparTexto(
                    tituloNode.InnerText);

            if (!TentarSepararNumeroETitulo(
                    tituloCompleto,
                    out var numero,
                    out var tituloOriginal))
            {
                continue;
            }

            var textoComplementar =
                ObterTextoAposTitulo(
                    tituloNode);

            var duracao =
                ObterDuracaoMinutos(
                    textoComplementar);

            var descricao =
                RemoverDuracaoInicial(
                    textoComplementar);

            var parteBase =
                EncontrarParteBase(
                    tituloOriginal,
                    partesBase);

            resultado.Add(
                new ParteSemanaImportacao
                {
                    Numero =
                        numero,

                    ParteBaseId =
                        parteBase?.Id ?? 0,

                    TituloOriginal =
                        tituloOriginal,

                    NomeParteBase =
                        parteBase?.Nome
                        ?? ObterNomePadronizado(
                            tituloOriginal),

                    Descricao =
                        descricao,

                    DuracaoMinutos =
                        duracao,

                    DataSemana =
                        dataSemana.Date,

                    UrlOrigem =
                        url,

                    Selecionado =
                        true,

                    ParteBaseEncontrada =
                        parteBase != null
                });
        }

        return resultado
            .OrderBy(x => x.Numero)
            .ToList();
    }

    private static void ValidarUrl(
        string url)
    {
        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out var endereco))
        {
            throw new ArgumentException(
                "O endereço informado não é válido.");
        }

        var pertenceAoJw =
            string.Equals(
                endereco.Host,
                "jw.org",
                StringComparison.OrdinalIgnoreCase) ||

            string.Equals(
                endereco.Host,
                "www.jw.org",
                StringComparison.OrdinalIgnoreCase) ||

            endereco.Host.EndsWith(
                ".jw.org",
                StringComparison.OrdinalIgnoreCase);

        if (!pertenceAoJw)
        {
            throw new ArgumentException(
                "Informe um endereço pertencente ao JW.ORG.");
        }

        if (endereco.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException(
                "O endereço precisa utilizar HTTPS.");
        }
    }

    private static bool TentarSepararNumeroETitulo(
        string texto,
        out int numero,
        out string titulo)
    {
        numero = 0;
        titulo = string.Empty;

        var correspondencia =
            Regex.Match(
                texto,
                @"^\s*(\d+)\s*[.\-–—:]\s*(.+)$");

        if (!correspondencia.Success)
        {
            return false;
        }

        if (!int.TryParse(
                correspondencia.Groups[1].Value,
                out numero))
        {
            return false;
        }

        titulo =
            LimparTexto(
                correspondencia.Groups[2].Value);

        return !string.IsNullOrWhiteSpace(
            titulo);
    }

    private static string ObterTextoAposTitulo(
        HtmlNode tituloNode)
    {
        var trechos =
            new List<string>();

        var atual =
            tituloNode.NextSibling;

        while (atual != null)
        {
            if (atual.NodeType ==
                HtmlNodeType.Element)
            {
                var nomeTag =
                    atual.Name.ToLowerInvariant();

                if (nomeTag == "h2" ||
                    nomeTag == "h3")
                {
                    break;
                }

                if (nomeTag != "script" &&
                    nomeTag != "style")
                {
                    var texto =
                        LimparTexto(
                            atual.InnerText);

                    if (!string.IsNullOrWhiteSpace(
                            texto))
                    {
                        trechos.Add(texto);
                    }
                }
            }

            atual = atual.NextSibling;
        }

        return LimparTexto(
            string.Join(" ", trechos));
    }

    private static int ObterDuracaoMinutos(
        string texto)
    {
        var correspondencia =
            Regex.Match(
                texto,
                @"\(\s*(\d+)\s*min(?:uto)?s?\s*\)",
                RegexOptions.IgnoreCase);

        if (!correspondencia.Success)
        {
            return 0;
        }

        return int.TryParse(
            correspondencia.Groups[1].Value,
            out var duracao)
            ? duracao
            : 0;
    }

    private static string RemoverDuracaoInicial(
        string texto)
    {
        var resultado =
            Regex.Replace(
                texto,
                @"^\s*\(\s*\d+\s*min(?:uto)?s?\s*\)\s*",
                string.Empty,
                RegexOptions.IgnoreCase);

        return LimparTexto(resultado);
    }

    private static Parte? EncontrarParteBase(
        string tituloOriginal,
        List<Parte> partesBase)
    {
        var nomePadronizado =
            ObterNomePadronizado(
                tituloOriginal);

        return partesBase.FirstOrDefault(
            parte =>
                NormalizarTexto(parte.Nome) ==
                NormalizarTexto(nomePadronizado));
    }

    private static string ObterNomePadronizado(
        string titulo)
    {
        var tituloNormalizado =
            NormalizarTexto(titulo);

        var equivalencias =
            new Dictionary<string, string>
            {
                ["LEITURA DA BIBLIA"] =
                    "Leitura da Bíblia",

                ["LEITURA BIBLICA"] =
                    "Leitura da Bíblia",

                ["JOIAS ESPIRITUAIS"] =
                    "Joias Espirituais",

                ["PEROLAS ESPIRITUAIS"] =
                    "Joias Espirituais",

                ["INICIANDO CONVERSAS"] =
                    "Iniciando Conversas",

                ["INICIAR CONVERSAS"] =
                    "Iniciando Conversas",

                ["COMECAR CONVERSAS"] =
                    "Iniciando Conversas",

                ["CULTIVANDO O INTERESSE"] =
                    "Cultivando o Interesse",

                ["MANTER O INTERESSE"] =
                    "Cultivando o Interesse",

                ["FAZENDO DISCIPULOS"] =
                    "Fazendo Discípulos",

                ["FAZER DISCIPULOS"] =
                    "Fazendo Discípulos",

                ["EXPLICANDO SUAS CRENCAS"] =
                    "Explicando suas Crenças",

                ["EXPLICAR SUAS CRENCAS"] =
                    "Explicando suas Crenças",

                ["ESTUDO BIBLICO DE CONGREGACAO"] =
                    "Estudo Bíblico de Congregação"
            };

        return equivalencias.TryGetValue(
            tituloNormalizado,
            out var nomePadronizado)
            ? nomePadronizado
            : titulo.Trim();
    }

    private static string NormalizarTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var normalizado =
            texto
                .Trim()
                .ToUpperInvariant()
                .Normalize(
                    NormalizationForm.FormD);

        var caracteres =
            normalizado.Where(
                caractere =>
                    CharUnicodeInfo.GetUnicodeCategory(
                        caractere) !=
                    UnicodeCategory.NonSpacingMark);

        var semAcentos =
            new string(
                caracteres.ToArray())
                .Normalize(
                    NormalizationForm.FormC);

        return Regex.Replace(
            semAcentos,
            @"\s+",
            " ");
    }

    private static string LimparTexto(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var decodificado =
            WebUtility.HtmlDecode(texto);

        return Regex.Replace(
            decodificado,
            @"\s+",
            " ")
            .Trim();
    }
}