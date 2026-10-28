using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

public sealed partial class OpenCTe
{
    public Task<CTeAutorizacaoResposta> AutorizarAsync(CTe documento, CancellationToken cancellationToken = default)
    {
        if (documento == null) throw new ArgumentNullException(nameof(documento));
        return AutorizarDocumentoAsync(documento, null, cancellationToken);
    }

    public Task<CTeAutorizacaoResposta> AutorizarAsync(CTeOS documento, CancellationToken cancellationToken = default)
    {
        return documento == null
            ? throw new ArgumentNullException(nameof(documento))
            : AutorizarDocumentoAsync(null, documento, cancellationToken);
    }

    private async Task<CTeAutorizacaoResposta> AutorizarDocumentoAsync(CTe cte, CTeOS os,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = cte?.InfCTe ?? os?.InfCTe ?? throw new ArgumentException("Informações do documento não informadas.");
        var version = Configuracoes.Geral.VersaoDFe;
        if (cte != null && version != CTeVersao.v400)
            throw new NotSupportedException("Utilize EnviarAsync para lotes de CT-e 2.00/3.00.");
        if (os != null && version == CTeVersao.v200)
            throw new NotSupportedException("CT-e OS exige versão 3.00 ou superior.");
        info.Versao = version;
        info.Ide.Mod = os != null ? ModeloCTe.CTeOS : ModeloCTe.CTe;
        info.Ide.TpAmb = Configuracoes.WebServices.Ambiente;
        os?.Versao = version;
        using var certificate = Configuracoes.Certificados.ObterCertificado();
        try
        {
            Status = StatusCTe.Recepcao;
            var options = DFeSaveOptions.DisableFormatting | DFeSaveOptions.OmitDeclaration;
            if (Configuracoes.Geral.RetirarAcentos) options |= DFeSaveOptions.RemoveAccents;
            if (Configuracoes.Geral.RetirarEspacos) options |= DFeSaveOptions.RemoveSpaces;
            if (cte != null) cte.Assinar(certificate, options);
            else os.Assinar(certificate, options);
            var xml = cte != null ? cte.Xml : os.Xml;
            // A partir da versão 4.00 a consulta por QR Code usa a chave e o ambiente do documento.
            // O XML assinado é preservado, inclusive os namespaces usados na assinatura.
            if (Configuracoes.Arquivos.Salvar && !Configuracoes.Arquivos.SalvarApenasCTeProcessados)
                await SaveAuthorizedDocumentAsync(info, xml, "-cte-env.xml", cancellationToken).ConfigureAwait(false);
            using var client = new CTeAutorizacaoServiceClient(Configuracoes, os != null, certificate);
            var response = await client.AutorizarAsync(xml, cancellationToken).ConfigureAwait(false);
            var protocol = response.Resultado.ProtCTe;
            if (protocol != null)
            {
                if (protocol.InfProt.ChCTe != info.Id.Substring(3))
                    throw new OpenDFeException("O protocolo retornado pertence a outra chave.");
                var original = XElement.Parse(xml);
                XNamespace signature = "http://www.w3.org/2000/09/xmldsig#";
                var digest = original.Descendants(signature + "DigestValue").FirstOrDefault()?.Value;
                if (Configuracoes.Geral.ValidarDigest && !string.IsNullOrEmpty(protocol.InfProt.DigVal)
                                                      && protocol.InfProt.DigVal != digest)
                    throw new OpenDFeException("Digest do protocolo diferente do documento enviado.");
                if (response.Autorizado)
                {
                    response.Conhecimento = new CTeProc { Versao = version, CTe = cte, CTeOS = os, ProtCTe = protocol };
                    if (Configuracoes.Arquivos.Salvar)
                    {
                        XNamespace ns = "http://www.portalfiscal.inf.br/cte";
                        var processed = new XElement(ns + (os == null ? "cteProc" : "cteOSProc"),
                            new XAttribute("versao", version.GetDescription()),
                            original, protocol.WriteToXml("protCTe", ns.NamespaceName));
                        await SaveAuthorizedDocumentAsync(info, processed.ToString(SaveOptions.DisableFormatting),
                            os == null ? "-cte.xml" : "-cteos.xml", cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return response;
        }
        finally
        {
            Status = StatusCTe.EmEspera;
        }
    }

    private Task SaveAuthorizedDocumentAsync(CTeInfCTe info, string xml, string suffix, CancellationToken token)
    {
        var date = Configuracoes.Arquivos.EmissaoPathCTe ? info.Ide.DhEmi.DateTime : DateTime.Now;
        var directory = Configuracoes.Arquivos.GetPathCTe(date, info.Emit.CNPJ, info.Ide.Mod);
        return Configuracoes.DocumentStore.SaveAsync(Path.Combine(directory, info.Id.Substring(3) + suffix), xml,
            token);
    }

    private async Task<EnviarCTeResposta> EnviarSincronoAsync(bool imprimir, CancellationToken cancellationToken)
    {
        if (Conhecimentos.CTe.Length + Conhecimentos.CTeOS.Length == 0)
            throw new InvalidOperationException("Nenhum conhecimento carregado.");
        var responses = new System.Collections.Generic.List<CTeAutorizacaoResposta>();
        foreach (var document in Conhecimentos.CTe)
            responses.Add(await AutorizarAsync(document, cancellationToken).ConfigureAwait(false));
        foreach (var document in Conhecimentos.CTeOS)
            responses.Add(await AutorizarAsync(document, cancellationToken).ConfigureAwait(false));
        var result = new EnviarCTeResposta(null, null)
        {
            AutorizacoesSincronas = [.. responses],
            CTeAutorizados =
                [.. responses.Where(response => response.Autorizado).Select(response => response.Conhecimento)]
        };
        if (imprimir && result.CTeAutorizados.Length > 0) DACTe?.Imprimir(result.CTeAutorizados);
        return result;
    }
}
