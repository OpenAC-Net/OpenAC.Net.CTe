using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;

namespace OpenAC.Net.CTe;

/// <summary>Lê arquivos de certificado de forma assíncrona; suporta também PFX em memória e certificado instalado.</summary>
public sealed class CTeCertificadoProvider : ICTeCertificadoProvider
{
    public async Task<X509Certificate2> ObterAsync(CTeConfigCertificados configuracao, CancellationToken cancellationToken = default)
    {
        if (configuracao == null) throw new ArgumentNullException(nameof(configuracao));
        cancellationToken.ThrowIfCancellationRequested();
        X509Certificate2 certificate;
        if (configuracao.CertificadoBytes?.Length > 0)
            certificate = CertificadoDigital.SelecionarCertificado(configuracao.CertificadoBytes, configuracao.Senha);
        else if (File.Exists(configuracao.Certificado))
        {
            using var stream = new FileStream(configuracao.Certificado, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, 8192, cancellationToken).ConfigureAwait(false);
            certificate = CertificadoDigital.SelecionarCertificado(buffer.ToArray(), configuracao.Senha);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(configuracao.Certificado))
                throw new InvalidOperationException("Certificado digital não configurado.");
            certificate = configuracao.ObterCertificado();
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (certificate is not { HasPrivateKey: true })
                throw new InvalidOperationException("Certificado com chave privada não informado.");
            return certificate;
        }
        catch { certificate?.Dispose(); throw; }
    }
}
