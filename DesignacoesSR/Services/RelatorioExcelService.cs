using ClosedXML.Excel;
using CommunityToolkit.Maui.Storage;
using DesignacoesSR.Models;

namespace DesignacoesSR.Services;

public class RelatorioExcelService
{
    private readonly DatabaseService _database;

    public RelatorioExcelService(
        DatabaseService database)
    {
        _database = database;
    }

    public async Task<string> GerarAsync(
        DateTime dataSemana,
        CancellationToken cancellationToken =
            default)
    {
        var itens =
            await _database
                .GetItensRelatorioSemanaAsync(
                    dataSemana);

        if (itens.Count == 0)
        {
            throw new InvalidOperationException(
                "Não existem partes cadastradas para essa semana.");
        }

        var presidente =
            LocalizarParticipante(
                itens,
                "PRESIDENTE");

        var oracao =
            LocalizarParticipante(
                itens,
                "ORACAO");

        var itensNumerados =
            itens
                .Where(x =>
                    !TituloContem(
                        x.Titulo,
                        "PRESIDENTE") &&
                    !TituloContem(
                        x.Titulo,
                        "ORACAO"))
                .OrderBy(x => x.Numero)
                .ToList();

        using var workbook =
            new XLWorkbook();

        var planilha =
            workbook.Worksheets.Add(
                "Programação");

        ConfigurarPagina(
            planilha);

        var linha =
            1;

        CriarCabecalho(
            planilha,
            ref linha,
            dataSemana);

        CriarPresidente(
            planilha,
            ref linha,
            presidente);

        CriarTextoInicial(
            planilha,
            ref linha);

        var tesouros =
            itensNumerados
                .Where(x =>
                    x.Numero >= 1 &&
                    x.Numero <= 3)
                .ToList();

        if (tesouros.Count > 0)
        {
            CriarSecao(
                planilha,
                ref linha,
                "Tesouros da Palavra de Deus");

            CriarItens(
                planilha,
                ref linha,
                tesouros);
        }

        var ministerio =
            itensNumerados
                .Where(EhParteMinisterio)
                .ToList();

        if (ministerio.Count > 0)
        {
            CriarSecao(
                planilha,
                ref linha,
                "Faça seu melhor no ministério");

            CriarItens(
                planilha,
                ref linha,
                ministerio);
        }

        var idsJaInseridos =
            tesouros
                .Concat(ministerio)
                .Select(x => x.ParteSemanaId)
                .ToHashSet();

        var vidaCrista =
            itensNumerados
                .Where(x =>
                    !idsJaInseridos.Contains(
                        x.ParteSemanaId))
                .ToList();

        if (vidaCrista.Count > 0)
        {
            CriarSecao(
                planilha,
                ref linha,
                "Nossa vida cristã");

            planilha
                .Cell(linha, 1)
                .Value =
                    "Cântico";

            planilha
                .Range(linha, 1, linha, 8)
                .Merge();

            AplicarFonteTexto(
                planilha.Range(
                    linha,
                    1,
                    linha,
                    8));

            linha++;

            CriarItens(
                planilha,
                ref linha,
                vidaCrista);
        }

        CriarOracaoFinal(
            planilha,
            ref linha,
            oracao);

        CriarLinhaFinal(
            planilha,
            ref linha);

        AjustarImpressao(
            planilha,
            linha);

        using var stream =
            new MemoryStream();

        workbook.SaveAs(stream);

        stream.Position = 0;

        var nomeArquivo =
            $"Programacao_" +
            $"{dataSemana:yyyy-MM-dd}.xlsx";

        var resultado =
            await FileSaver.Default.SaveAsync(
                nomeArquivo,
                stream,
                cancellationToken);

        if (!resultado.IsSuccessful)
        {
            throw resultado.Exception
                  ?? new InvalidOperationException(
                      "Não foi possível salvar o relatório.");
        }

        return resultado.FilePath;
    }

    private static void ConfigurarPagina(
        IXLWorksheet planilha)
    {
        planilha.ShowGridLines =
            false;

        planilha.Column(1).Width =
            5;

        planilha.Column(2).Width =
            7;

        planilha.Column(3).Width =
            24;

        planilha.Column(4).Width =
            16;

        planilha.Column(5).Width =
            16;

        planilha.Column(6).Width =
            16;

        planilha.Column(7).Width =
            16;

        planilha.Column(8).Width =
            16;

        planilha.PageSetup.PageOrientation =
            XLPageOrientation.Portrait;

        planilha.PageSetup.PaperSize =
            XLPaperSize.A4Paper;

        planilha.PageSetup.Margins.Top =
            0.4;

        planilha.PageSetup.Margins.Bottom =
            0.4;

        planilha.PageSetup.Margins.Left =
            0.45;

        planilha.PageSetup.Margins.Right =
            0.45;

        planilha.PageSetup.CenterHorizontally =
            true;
    }

    private static void CriarCabecalho(
        IXLWorksheet planilha,
        ref int linha,
        DateTime dataSemana)
    {
        var dataFinal =
            dataSemana.AddDays(6);

        string periodo;

        if (dataSemana.Month ==
            dataFinal.Month)
        {
            periodo =
                $"{dataSemana:dd}-{dataFinal:dd} " +
                $"de {dataSemana:MMMM}";
        }
        else
        {
            periodo =
                $"{dataSemana:dd} de " +
                $"{dataSemana:MMMM} - " +
                $"{dataFinal:dd} de " +
                $"{dataFinal:MMMM}";
        }

        var celula =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        celula.Merge();

        celula.Value =
            periodo.ToUpperInvariant();

        celula.Style.Font.Bold =
            true;

        celula.Style.Font.FontSize =
            20;

        celula.Style.Font.FontColor =
            XLColor.Red;

        celula.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        linha += 2;
    }

    private static void CriarPresidente(
    IXLWorksheet planilha,
    ref int linha,
    string presidente)
    {
        var faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Merge();

        var celula =
            planilha.Cell(
                linha,
                1);

        var textoFormatado =
            celula.GetRichText();

        textoFormatado
            .AddText("PRESIDENTE: ")
            .SetBold()
            .SetUnderline();

        textoFormatado
            .AddText(
                string.IsNullOrWhiteSpace(presidente)
                    ? "NÃO DEFINIDO"
                    : presidente)
            .SetBold()
            .SetFontColor(
                XLColor.Red);

        faixa.Style.Font.FontSize =
            15;

        faixa.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        faixa.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        planilha.Row(linha).Height =
            25;

        linha += 2;
    }

    private static void CriarTextoInicial(
        IXLWorksheet planilha,
        ref int linha)
    {
        var faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Merge();

        faixa.Value =
            "Cântico e Oração Inicial";

        AplicarFonteTexto(faixa);

        linha++;

        faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Merge();

        faixa.Value =
            "Comentários iniciais (1 min)";

        AplicarFonteTexto(faixa);

        linha++;
    }

    private static void CriarSecao(
        IXLWorksheet planilha,
        ref int linha,
        string titulo)
    {
        linha++;

        var faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Merge();

        faixa.Value =
            titulo;

        faixa.Style.Font.Bold =
            true;

        faixa.Style.Font.Underline =
            XLFontUnderlineValues.Single;

        faixa.Style.Font.FontColor =
            XLColor.DarkRed;

        faixa.Style.Font.FontSize =
            16;

        faixa.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        linha++;

        planilha.Row(linha - 1).Height =
            25;
    }

    private static void CriarItens(
    IXLWorksheet planilha,
    ref int linha,
    List<ItemRelatorioSemana> itens)
    {
        foreach (var item in itens)
        {
            var faixa =
                planilha.Range(
                    linha,
                    1,
                    linha,
                    8);

            faixa.Merge();

            var celula =
                planilha.Cell(
                    linha,
                    1);

            var titulo =
                $"{item.Numero}. {item.Titulo}";

            if (item.DuracaoMinutos > 0)
            {
                titulo +=
                    $" ({item.DuracaoMinutos} min)";
            }

            var textoFormatado =
                celula.GetRichText();

            textoFormatado
                .AddText(titulo + " ")
                .SetBold()
                .SetFontColor(
                    XLColor.Black);

            textoFormatado
                .AddText(
                    string.IsNullOrWhiteSpace(
                        item.Participante)
                        ? "NÃO DEFINIDO"
                        : item.Participante)
                .SetBold()
                .SetFontColor(
                    XLColor.Red);

            faixa.Style.Font.FontSize =
                14;

            faixa.Style.Alignment.WrapText =
                true;

            faixa.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;

            faixa.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Left;

            planilha.Row(linha).Height =
                24;

            linha++;
        }
    }

    private static void CriarOracaoFinal(
    IXLWorksheet planilha,
    ref int linha,
    string oracao)
    {
        var faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Merge();

        var celula =
            planilha.Cell(
                linha,
                1);

        var textoFormatado =
            celula.GetRichText();

        textoFormatado
            .AddText(
                "Cântico e oração final: ")
            .SetBold()
            .SetFontColor(
                XLColor.Black);

        textoFormatado
            .AddText(
                string.IsNullOrWhiteSpace(oracao)
                    ? "NÃO DEFINIDO"
                    : oracao)
            .SetBold()
            .SetFontColor(
                XLColor.Red);

        faixa.Style.Font.FontSize =
            14;

        faixa.Style.Alignment.WrapText =
            true;

        faixa.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        planilha.Row(linha).Height =
            24;

        linha += 2;
    }
    private static void CriarLinhaFinal(
        IXLWorksheet planilha,
        ref int linha)
    {
        var faixa =
            planilha.Range(
                linha,
                1,
                linha,
                8);

        faixa.Style.Border.BottomBorder =
            XLBorderStyleValues.Medium;
    }

    private static void AplicarFonteTexto(
        IXLRange faixa)
    {
        faixa.Style.Font.Bold =
            true;

        faixa.Style.Font.FontSize =
            14;
    }

    private static void AjustarImpressao(
        IXLWorksheet planilha,
        int ultimaLinha)
    {
        planilha.PageSetup.PrintAreas.Clear();

        planilha.PageSetup.PrintAreas.Add(
            $"A1:H{ultimaLinha}");

        planilha.PageSetup.PagesWide =
            1;

        planilha.PageSetup.PagesTall =
            1;

        planilha.SheetView.FreezeRows(
            0);
    }

    private static bool EhParteMinisterio(
        ItemRelatorioSemana item)
    {
        var titulo =
            Normalizar(item.Titulo);

        return titulo.Contains(
                   "INICIANDO CONVERSAS") ||

               titulo.Contains(
                   "COMECAR CONVERSAS") ||

               titulo.Contains(
                   "CULTIVANDO O INTERESSE") ||

               titulo.Contains(
                   "MANTER O INTERESSE") ||

               titulo.Contains(
                   "FAZENDO DISCIPULOS") ||

               titulo.Contains(
                   "FAZER DISCIPULOS") ||

               titulo.Contains(
                   "EXPLICANDO SUAS CRENCAS") ||

               titulo.Contains(
                   "DISCURSO");
    }

    private static string LocalizarParticipante(
        IEnumerable<ItemRelatorioSemana> itens,
        string titulo)
    {
        return itens
                   .FirstOrDefault(
                       x =>
                           TituloContem(
                               x.Titulo,
                               titulo))
                   ?.Participante
               ?? string.Empty;
    }

    private static bool TituloContem(
        string texto,
        string valor)
    {
        return Normalizar(texto)
            .Contains(
                Normalizar(valor));
    }

    private static string Normalizar(
        string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var normalizado =
            texto
                .Trim()
                .ToUpperInvariant()
                .Normalize(
                    System.Text
                        .NormalizationForm.FormD);

        var caracteres =
            normalizado
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

        return new string(caracteres);
    }
}