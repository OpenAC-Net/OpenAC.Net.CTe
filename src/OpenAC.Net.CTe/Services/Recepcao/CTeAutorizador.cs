using OpenAC.Net.DFe.Core.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Storage;

namespace OpenAC.Net.CTe.Services;

internal static class CTeAutorizador
{
    internal static async Task<CTeAutorizacaoResposta> AutorizarAsync(CTeConfig configuracao, CTe cte, CTeOS os,
        X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        var info = cte?.InfCTe ?? os?.InfCTe ?? throw new ArgumentException("Informações do documento não informadas.");
        var version = configuracao.Geral.VersaoDFe;

        CTeUtils.ValidarVersaoComunicacao(info.Versao);

        if (os != null)
            CTeUtils.ValidarVersaoComunicacao(os.Versao);

        info.Ide.Mod = os != null ? ModeloCTe.CTeOS : ModeloCTe.CTe;
        info.Ide.TpAmb = configuracao.WebServices.Ambiente;
        os?.Versao = version;

        var options = DFeSaveOptions.DisableFormatting | DFeSaveOptions.OmitDeclaration;
        if (configuracao.Geral.RetirarAcentos) options |= DFeSaveOptions.RemoveAccents;
        if (configuracao.Geral.RetirarEspacos) options |= DFeSaveOptions.RemoveSpaces;
        var suplementar = cte?.InfCTeSupl ?? os?.InfCTeSupl;
        if (string.IsNullOrEmpty(suplementar?.QrCode))
        {
            // Assinar gera a chave quando ela ainda não existe; o catálogo de QR Code é isolado por configuração.
            var chave = info.Id;
            if (string.IsNullOrEmpty(chave) || chave.Length < 44)
            {
                var gerada = ChaveDFe.Gerar(info.Ide.CUF, info.Ide.DhEmi.DateTime, info.Emit.CNPJ,
                    (int)info.Ide.Mod, info.Ide.Serie, info.Ide.NCT, info.Ide.TpEmis, info.Ide.CCT);
                info.Id = "CTe" + gerada.Chave;
                info.Ide.CDV = gerada.Digito;
            }
            var address = configuracao.ObterEndereco(TipoServicoCTe.QRCode, info.Ide.CUF);
            suplementar = new CTeInfCteSupl { QrCode = address + (address.Contains("?") ? "&" : "?")
                + "chCTe=" + info.Id.Substring(3) + "&tpAmb=" + info.Ide.TpAmb.GetDFeValue() };
            if (cte != null) cte.InfCTeSupl = suplementar;
            else os.InfCTeSupl = suplementar;
        }
        if (cte != null) cte.Assinar(certificate, options);
        else os.Assinar(certificate, options);

        var xml = cte != null ? cte.Xml : os.Xml;

        // A partir da versão 4.00 a consulta por QR Code usa a chave e o ambiente do documento.
        // O XML assinado é preservado, inclusive os namespaces usados na assinatura.
        if (configuracao.Arquivos is { Salvar: true, SalvarApenasCTeProcessados: false })
            await SaveAuthorizedDocumentAsync(configuracao, info, xml, "-cte-env.xml", cancellationToken).ConfigureAwait(false);

        using var client = new CTeAutorizacaoServiceClient(configuracao, os != null, certificate);
        var response = await client.AutorizarAsync(xml, cancellationToken).ConfigureAwait(false);
        var protocol = response.Resultado.ProtCTe;
        if (protocol == null) return response;

        if (protocol.InfProt.ChCTe != info.Id.Substring(3))
            throw new OpenDFeException("O protocolo retornado pertence a outra chave.");

        var original = XElement.Parse(xml);
        XNamespace signature = "http://www.w3.org/2000/09/xmldsig#";
        var digest = original.Descendants(signature + "DigestValue").FirstOrDefault()?.Value;
        if (configuracao.Geral.ValidarDigest && !string.IsNullOrEmpty(protocol.InfProt.DigVal)
                                              && protocol.InfProt.DigVal != digest)
            throw new OpenDFeException("Digest do protocolo diferente do documento enviado.");
        if (!response.Autorizado) return response;

        response.DocumentoProcessado = new CTeProc { Versao = version, CTe = cte, CTeOS = os, ProtCTe = protocol };
        if (!configuracao.Arquivos.Salvar) return response;

        XNamespace ns = "http://www.portalfiscal.inf.br/cte";
        var processed = new XElement(ns + (os == null ? "cteProc" : "cteOSProc"),
            new XAttribute("versao", version.GetDescription()),
            original, protocol.WriteToXml("protCTe", ns.NamespaceName));
        await SaveAuthorizedDocumentAsync(configuracao, info, processed.ToString(SaveOptions.DisableFormatting),
            os == null ? "-cte.xml" : "-cteos.xml", cancellationToken).ConfigureAwait(false);

        return response;
    }

    private static Task SaveAuthorizedDocumentAsync(CTeConfig configuracao, CTeInfCTe info, string xml, string suffix, CancellationToken token)
    {
        var date = configuracao.Arquivos.EmissaoPathCTe ? info.Ide.DhEmi.DateTime : DateTime.Now;
        var directory = configuracao.Arquivos.GetPathCTe(date, info.Emit.CNPJ, info.Ide.Mod);
        return configuracao.DocumentStore.SaveAsync(new DFeDocument(
            Path.GetFullPath(Path.Combine(directory, info.Id.Substring(3) + suffix)), xml, DFeArquivoExiste.Sobrescrever), token);
    }

}
