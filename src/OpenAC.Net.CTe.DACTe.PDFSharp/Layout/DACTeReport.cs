using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;

namespace OpenAC.Net.CTe.DACTe.PDFSharp.Layout;

/// <summary>Composição por blocos, com continuação automática e identidade repetida por página.</summary>
internal sealed class DACTeReport : IDisposable
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
    private readonly PdfDocument document;
    private readonly DACTeConfig config;
    private readonly bool landscape;
    private readonly string title;
    private readonly string identity;
    private readonly string warning;
    private readonly XFont normal;
    private readonly XFont bold;
    private readonly XFont small;
    private XGraphics graphics;
    private PdfPage page;
    private double y;
    private double Width => page.Width.Point - 32;
    private double Bottom => page.Height.Point - 40;

    internal DACTeReport(PdfDocument document, DACTeConfig config, bool landscape, string title, string identity, string warning)
    {
        DACTeFontResolver.Initialize();
        this.document = document;
        this.config = config;
        this.landscape = landscape;
        this.title = title;
        this.identity = identity;
        this.warning = warning;
        normal = new XFont("Liberation Sans", 8);
        bold = new XFont("Liberation Sans", 8, XFontStyleEx.Bold);
        small = new XFont("Liberation Sans", 6);
        NewPage();
    }

    private void NewPage()
    {
        graphics?.Dispose();
        page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = landscape ? PageOrientation.Landscape : PageOrientation.Portrait;
        graphics = XGraphics.FromPdfPage(page);
        y = 16;
        graphics.DrawString(title, bold, XBrushes.Black, new XRect(16, y, Width, 13), XStringFormats.TopLeft);
        y += 15;
        graphics.DrawString(identity, small, XBrushes.Black, new XRect(16, y, Width, 11), XStringFormats.TopLeft);
        y += 13;
        if (!string.IsNullOrEmpty(warning))
        {
            foreach (var line in Wrap(warning, Width - 8, bold))
            {
                graphics.DrawString(line, bold, XBrushes.Red, new XRect(20, y, Width - 8, 12), XStringFormats.TopLeft);
                y += 12;
            }
        }
        y += 4;
        graphics.DrawString(config.MensagemRodape ?? "", small, XBrushes.Black,
            new XRect(16, page.Height.Point - 25, Width, 10), XStringFormats.TopLeft);
    }

    private void Ensure(double height)
    {
        if (y + height > Bottom) NewPage();
    }

    internal void Section(string caption, IEnumerable<string> lines)
    {
        var data = lines.Where(line => !string.IsNullOrWhiteSpace(line))
            .SelectMany(line => Wrap(line, Width - 8, normal)).ToArray();
        if (data.Length == 0) return;
        var offset = 0;
        while (offset < data.Length)
        {
            Ensure(41);
            var count = Math.Min(data.Length - offset, (int)((Bottom - y - 19) / 11));
            var height = 17 + count * 11;
            graphics.DrawRectangle(new XPen(XColors.Black, .5), 16, y, Width, height);
            graphics.DrawRectangle(XBrushes.LightGray, 16.3, y + .3, Width - .6, 13);
            graphics.DrawString(caption + (offset > 0 ? " (continuação)" : ""), bold, XBrushes.Black,
                new XRect(20, y + 2, Width - 8, 12), XStringFormats.TopLeft);
            for (var i = 0; i < count; i++)
                graphics.DrawString(data[offset + i], normal, XBrushes.Black,
                    new XRect(20, y + 16 + i * 11, Width - 8, 11), XStringFormats.TopLeft);
            y += height + 3;
            offset += count;
            if (offset < data.Length) NewPage();
        }
    }

    internal void AccessKey(string key, string qrCode)
    {
        Ensure(90);
        Code128.Draw(graphics, key, 20, y + 2, Width - 98, 32);
        graphics.DrawString(string.Join(" ", Enumerable.Range(0, 11).Select(i => key.Substring(i * 4, 4))),
            normal, XBrushes.Black, new XRect(20, y + 37, Width - 98, 12), XStringFormats.TopLeft);
        graphics.DrawString("CHAVE DE ACESSO - Consulte a autenticidade no portal do CT-e", small, XBrushes.Black,
            new XRect(20, y + 51, Width - 98, 10), XStringFormats.TopLeft);
        if (!string.IsNullOrWhiteSpace(qrCode))
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(qrCode, QRCodeGenerator.ECCLevel.M);
            var size = 76.0 / data.ModuleMatrix.Count;
            for (var row = 0; row < data.ModuleMatrix.Count; row++)
                for (var column = 0; column < data.ModuleMatrix.Count; column++)
                    if (data.ModuleMatrix[row][column])
                        graphics.DrawRectangle(XBrushes.Black, page.Width.Point - 94 + column * size, y + row * size, size, size);
        }
        y += 81;
    }

    internal void Logo()
    {
        if (config.Logo == null || config.Logo.Length == 0) return;
        Ensure(45);
        using var stream = new MemoryStream(config.Logo);
        using var image = XImage.FromStream(stream);
        var scale = Math.Min(120 / image.PointWidth, 40 / image.PointHeight);
        graphics.DrawImage(image, 20, y, image.PointWidth * scale, image.PointHeight * scale);
        y += 44;
    }

    // A divisão por caractere também preserva URLs, chaves e palavras maiores que a coluna.
    private IEnumerable<string> Wrap(string text, double width, XFont font)
    {
        foreach (var paragraph in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var rest = paragraph.Replace('\t', ' ');
            if (rest.Length == 0) { yield return ""; continue; }
            while (rest.Length > 0)
            {
                var count = rest.Length;
                if (graphics.MeasureString(rest, font).Width > width)
                {
                    var lo = 1;
                    var hi = count;
                    while (lo < hi)
                    {
                        var mid = (lo + hi + 1) / 2;
                        if (graphics.MeasureString(rest.Substring(0, mid), font).Width <= width) lo = mid;
                        else hi = mid - 1;
                    }
                    count = lo;
                    var space = rest.LastIndexOf(' ', count - 1, count);
                    if (space > 0) count = space;
                }
                yield return rest.Substring(0, count);
                rest = rest.Substring(count).TrimStart();
            }
        }
    }

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["XNome"] = "Nome", ["XFant"] = "Nome fantasia", ["XLgr"] = "Endereço", ["Nro"] = "Número",
        ["XCpl"] = "Complemento", ["XBairro"] = "Bairro", ["XMun"] = "Município", ["CMun"] = "IBGE",
        ["Fone"] = "Telefone", ["XPais"] = "País", ["CPais"] = "Código país", ["XObs"] = "Observações",
        ["VTotTrib"] = "Total de tributos", ["InfAdFisco"] = "Informações ao fisco", ["VBC"] = "Base de cálculo",
        ["PICMS"] = "Alíquota ICMS (%)", ["VICMS"] = "Valor ICMS", ["VCarga"] = "Valor da carga",
        ["ProPred"] = "Produto predominante", ["XOutCat"] = "Outras características", ["VCargaAverb"] = "Valor averbado",
        ["CDFeUnidMed"] = "Unidade", ["TpMed"] = "Medida", ["QCarga"] = "Quantidade", ["XDescServ"] = "Descrição do serviço",
        ["XJust"] = "Justificativa", ["DescEvento"] = "Evento", ["NProt"] = "Protocolo", ["XCondUso"] = "Condições de uso",
        ["GrupoAlterado"] = "Grupo alterado", ["campoAlterado"] = "Campo alterado", ["valorAlterado"] = "Valor corrigido",
        ["nroItemAlterado"] = "Item alterado", ["VComp"] = "Valor", ["NFat"] = "Fatura", ["NDup"] = "Duplicata",
        ["DVenc"] = "Vencimento", ["VDup"] = "Valor", ["XOrig"] = "Origem", ["XDest"] = "Destino", ["XRota"] = "Rota"
    };

    internal static IEnumerable<string> Fields(object value, int depth = 0)
    {
        if (value == null || depth > 12) yield break;
        if (value is string text) { if (text.Length > 0) yield return text; yield break; }
        if (value is IEnumerable collection)
        {
            var index = 0;
            foreach (var item in collection)
            {
                index++;
                foreach (var line in Fields(item, depth + 1)) yield return $"{index}: {line}";
            }
            yield break;
        }
        var scalars = new List<string>();
        var nested = new List<object>();
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (property.Name == "Parent" || !property.CanRead || property.GetIndexParameters().Length != 0) continue;
            var field = property.GetValue(value);
            if (field == null) continue;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type == typeof(string) || type.IsValueType)
            {
                if (field is string s && string.IsNullOrWhiteSpace(s)) continue;
                if (field is DateTime date && date == default || field is DateTimeOffset offset && offset == default) continue;
                var label = Labels.TryGetValue(property.Name, out var name) ? name : property.Name;
                var formatted = field is decimal number && property.Name.StartsWith("V", StringComparison.Ordinal)
                    ? number.ToString("N2", Pt) : Convert.ToString(field, Pt);
                scalars.Add(label + ": " + formatted);
            }
            else nested.Add(field);
        }
        if (scalars.Count > 0) yield return string.Join("    |    ", scalars);
        foreach (var item in nested)
            foreach (var line in Fields(item, depth + 1)) yield return line;
    }

    internal static string Money(decimal value) => value.ToString("N2", Pt);
    public void Dispose() { graphics?.Dispose(); graphics = null; }
}
