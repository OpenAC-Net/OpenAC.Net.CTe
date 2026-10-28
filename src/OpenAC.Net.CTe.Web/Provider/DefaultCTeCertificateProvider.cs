using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe.Web;

internal sealed class DefaultCTeCertificateProvider : ICTeCertificateProvider
{
    public ValueTask ConfigureAsync(string tenantId, CTeConfigCertificados configuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}