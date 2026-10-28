using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

public sealed partial class OpenCTe : IOpenCTeClient
{
    public async Task<DistribuicaoDFeResponse> ConsultarDistribuicaoAsync(DFeCodUF uf, string cpfCNPJ,
        string nsu = "", string chave = "", string ultNsu = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var certificate = Configuracoes.Certificados.ObterCertificado();
        using var service = new DistribuicaoDFeServiceClient(Configuracoes, uf, certificate);
        return await service.ConsultaDFeAsync(cpfCNPJ, nsu, chave, ultNsu, cancellationToken).ConfigureAwait(false);
    }
}
