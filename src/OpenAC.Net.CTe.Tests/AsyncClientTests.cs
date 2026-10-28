using System.Net;
using System.Security.Cryptography.X509Certificates;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Tests;

public class AsyncClientTests
{
    [Test]
    [Arguments(CTeVersao.v300)]
    [Arguments(CTeVersao.v400)]
    public async Task PreCanceledOperationsStopBeforeLoadingCertificateOrSendingDocuments(CTeVersao version)
    {
        var config = new CTeConfig();
        config.Geral.VersaoDFe = version;
        using IOpenCTeClient client = new OpenCTe(config);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        Func<Task>[] operations =
        [
            () => client.EnviarAsync(1, cancellationToken: token),
            () => client.EnviarAsync("1", cancellationToken: token),
            () => client.AutorizarAsync(new CTe(), token),
            () => client.AutorizarAsync(new CTeOS(), token),
            () => client.ConsultaReciboAsync("1", token),
            () => client.ConsultarAsync(cancellationToken: token),
            () => client.ConsultarSituacaoServicoAsync(token),
            () => client.InutilizacaoAsync("12345678000195", "Justificativa da inutilização", 26,
                ModeloCTe.CTe, 1, 1, 2, token),
            () => client.EnviarEventoAsync(1, PdfTests.Key, "12345678000195", new CTeEvCancCTe(), token),
            () => client.ConsultaCadastroAsync(DFeCodUF.MS, cancellationToken: token),
            () => client.ConsultarDistribuicaoAsync(DFeCodUF.MS, "12345678000195", cancellationToken: token)
        ];

        foreach (var operation in operations)
        {
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(operation);
            await Assert.That(exception!.CancellationToken).IsEqualTo(token);
        }
    }

    [Test]
    [Arguments("success")]
    [Arguments("cancellation")]
    [Arguments("fault")]
    public async Task StatusReturnsPendingTaskAndRestoresStateAfterCompletion(string outcome)
    {
        using var certificate = InfrastructureTests.CreateCertificate();
        var transport = new PendingTransport();
        var config = new CTeConfig { HttpTransport = transport };
        config.Geral.VersaoDFe = CTeVersao.v400;
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        config.Certificados.Senha = "test";
        config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
        using var component = new OpenCTe(config);
        IOpenCTeClient client = component;
        using var cancellation = new CancellationTokenSource();

        var pending = client.ConsultarSituacaoServicoAsync(cancellation.Token);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(pending.IsCompleted).IsFalse();
        await Assert.That(component.Status).IsEqualTo(StatusCTe.StatusServico);

        if (outcome == "cancellation")
        {
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        else if (outcome == "fault")
        {
            transport.Response.SetResult("<s:Fault><s:Reason><s:Text>Falha de teste</s:Text></s:Reason></s:Fault>");
            var exception = await Assert.ThrowsExactlyAsync<OpenDFeCommunicationException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
            await Assert.That(exception!.Message).Contains("Falha de teste");
        }
        else
        {
            transport.Response.SetResult("<cteStatusServicoCTResult><retConsStatServCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"4.00\"><tpAmb>2</tpAmb><cStat>107</cStat><xMotivo>Serviço em operação</xMotivo></retConsStatServCTe></cteStatusServicoCTResult>");
            var response = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.That(response.Resultado.CStat).IsEqualTo(107);
        }

        await Assert.That(component.Status).IsEqualTo(StatusCTe.EmEspera);
    }

    private sealed class PendingTransport : ICTeHttpTransport
    {
        internal readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<string> Response = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CTeConfig configuration,
            X509Certificate2 certificate, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            var body = await Response.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>{body}</s:Body></s:Envelope>")
            };
        }
    }
}
