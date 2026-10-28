using System;
using System.IO;
using PdfSharp.Fonts;

namespace OpenAC.Net.CTe.DACTe.PDFSharp.Layout;

internal sealed class DACTeFontResolver : IFontResolver
{
    private static readonly object Gate = new();
    internal static void Initialize()
    {
        lock (Gate)
        {
            // Respeitar o resolver da aplicação hospedeira (inclusive DANFSe).
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new DACTeFontResolver();
        }
    }
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        => new("LiberationSans-" + (bold ? (italic ? "BoldItalic" : "Bold") : (italic ? "Italic" : "Regular")));

    public byte[] GetFont(string faceName)
    {
        using var stream = typeof(DACTeFontResolver).Assembly.GetManifestResourceStream(
            "OpenAC.Net.CTe.DACTe.PDFSharp.Resources.Fonts." + faceName + ".ttf")
            ?? throw new InvalidOperationException("Fonte embutida não encontrada: " + faceName);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }
}
