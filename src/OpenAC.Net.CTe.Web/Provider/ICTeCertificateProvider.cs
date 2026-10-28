using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe.Web;

/// <summary>Permite obter ou renovar o certificado sem acoplar a biblioteca ao repositório de segredos.</summary>
public interface ICTeCertificateProvider
{
    ValueTask ConfigureAsync(string tenantId, CTeConfigCertificados configuration,
        CancellationToken cancellationToken = default);
}