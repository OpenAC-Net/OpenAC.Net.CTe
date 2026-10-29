using System.Net;
using System.Security.Cryptography.X509Certificates;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Http;

namespace OpenAC.Net.CTe.Tests;

public class CadastroContractTests
{
    [Test]
    public async Task CadastroUsesVersion4SoapAndVersion2FiscalXmlWithoutTheOldHeader()
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var capture = new CadastroTransport();
        var config = new CTeConfig { HttpTransport = capture };
        config.WebServices.UF = DFeCodUF.MG;
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        config.Certificados.Senha = "test";
        var response = await new OpenCTe(config).ConsultaCadastroAsync(DFeCodUF.MS, "12345678000195");
        await Assert.That(capture.Url).IsEqualTo("https://hom.nfe.sefaz.ms.gov.br/ws/CadConsultaCadastro4");
        await Assert.That(capture.Action).Contains("CadConsultaCadastro4/consultaCadastro");
        await Assert.That(capture.Xml).Contains("<ConsCad versao=\"2.00\"");
        await Assert.That(capture.Xml).Contains("<UF>MS</UF>");
        await Assert.That(capture.Xml).DoesNotContain("nfeCabecMsg");
        await Assert.That(response.XmlRetorno).Contains("retConsCad");
    }

    private sealed class CadastroTransport : IDFeHttpTransport
    {
        internal string Url = "", Xml = "", Action = "";
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DFeHttpTransportOptions options, CancellationToken cancellationToken = default)
        {
            Url = request.RequestUri!.ToString();
            Xml = await request.Content!.ReadAsStringAsync(cancellationToken);
            Action = request.Content.Headers.ContentType!.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><nfeResultMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/CadConsultaCadastro4\"><retConsCad xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"2.00\"><infCons><cStat>111</cStat><xMotivo>Consulta cadastro com uma ocorrência</xMotivo><UF>MS</UF></infCons></retConsCad></nfeResultMsg></s:Body></s:Envelope>") };
        }
    }
}
