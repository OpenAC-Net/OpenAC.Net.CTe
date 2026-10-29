using System;
using OpenAC.Net.Core.Extensions;

namespace OpenAC.Net.CTe;

internal static class CTeUtils
{
    internal static void ValidarVersaoComunicacao(CTeVersao versao)
    {
        if (versao != CTeVersao.v400)
            throw new NotSupportedException("A comunicação com a SEFAZ exige CT-e 4.00. " +
                "As versões 2.00 e 3.00 estão disponíveis apenas para leitura, geração e impressão.");
    }

    public static string RemoverDeclaracaoXml(this string xml)
    {
        if (xml.IsEmpty()) return xml;

        var posIni = xml.IndexOf("<?", StringComparison.Ordinal);
        if (posIni < 0) return xml;

        var posFinal = xml.IndexOf("?>", StringComparison.Ordinal);
        return posFinal < 0 ? xml : xml.Remove(posIni, (posFinal + 2) - posIni);
    }
}
