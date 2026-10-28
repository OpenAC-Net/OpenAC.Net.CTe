using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;

namespace OpenAC.Net.CTe.Services;

/// <summary>Autorização síncrona: um documento por chamada, GZip/Base64 conforme MOC 4.00.</summary>
public sealed class CTeAutorizacaoServiceClient : CTeServiceClient
{
    private readonly bool os;
    public CTeAutorizacaoServiceClient(CTeConfig config, bool cteOS = false, X509Certificate2 certificate = null)
        : base(config, cteOS ? TipoServicoCTe.CTeRecepcaoOS : TipoServicoCTe.CTeRecepcao, certificate)
    {
        os = cteOS;
        NomeArquivo = cteOS ? "cteos-autorizacao" : "cte-autorizacao";
        if (!cteOS && config.Geral.VersaoDFe != CTeVersao.v400)
            throw new NotSupportedException("A autorização síncrona de CT-e exige a versão 4.00.");
    }
    public async Task<CTeAutorizacaoResposta> AutorizarAsync(string xml, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var root = XElement.Parse(xml);
            if (root.Name != XName.Get(os ? "CTeOS" : "CTe", "http://www.portalfiscal.inf.br/cte"))
                throw new ArgumentException("Modelo do XML incompatível com o serviço de autorização.", nameof(xml));
            CTeSchemaValidator.Validate(xml, Configuracoes, os);
            var v4 = Configuracoes.Geral.VersaoDFe == CTeVersao.v400;
            var contract = v4 ? (os ? "CTeRecepcaoOSV4" : "CTeRecepcaoSincV4") : "CteRecepcaoOS";
            var xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/" + contract;
            var method = os ? "cteRecepcaoOS" : "cteRecepcao";
            var payload = v4 ? Compress(xml) : xml;
            if (Encoding.UTF8.GetByteCount(payload) > 500 * 1024) throw new ArgumentException("Mensagem superior a 500 KB.", nameof(xml));
            var message = new XElement(XName.Get("cteDadosMsg", xmlns), v4 ? (object)payload : root).ToString(SaveOptions.DisableFormatting);
            var result = await ExecuteAsync(xmlns + "/" + method, message, SoapHeader(xmlns), cancellationToken).ConfigureAwait(false);
            return new CTeAutorizacaoResposta(xml, result, EnvelopeEnvio, EnvelopeRetorno);
        }
        finally { ServiceGate.Release(); }
    }
    private static string Compress(string xml)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
        {
            var bytes = Encoding.UTF8.GetBytes(xml);
            gzip.Write(bytes, 0, bytes.Length);
        }
        return Convert.ToBase64String(output.ToArray());
    }
    protected override string TratarRetorno(XElement body)
    {
        foreach (var name in new[] { "cteRecepcaoResult", "cteRecepcaoOSResult", "cteOSRecepcaoResult", "cteRecepcaoOSCTResult" })
        {
            var result = System.Linq.Enumerable.FirstOrDefault(body.Descendants(), element => element.Name.LocalName == name);
            if (result != null) return result.FirstNode is XElement child ? child.ToString(SaveOptions.DisableFormatting) : result.Value;
        }
        throw new OpenDFeCommunicationException("Resultado de autorização não encontrado no envelope SOAP.");
    }
}
