using OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;

namespace OpenAC.Net.CTe.DACTe.PDFSharp.Web;

internal sealed class OpenDACTeWeb(OpenAC.Net.CTe.Web.ICTeConfigurationProvider<DACTeConfig> configurations)
    : IOpenDACTeWeb
{
    public async ValueTask<byte[]> GerarPDFAsync(CTeProc conhecimento, string tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Empresa não informada.", nameof(tenantId));
        cancellationToken.ThrowIfCancellationRequested();
        var config = await configurations.GetConfigurationAsync(tenantId, cancellationToken).ConfigureAwait(false)
                     ?? throw new InvalidOperationException("Configuração do DACTE não informada.");
        var renderer = new OpenDACTe();
        renderer.Configuracoes.Logo = config.Logo;
        renderer.Configuracoes.MensagemRodape = config.MensagemRodape;
        renderer.Configuracoes.SenhaUsuario = config.SenhaUsuario;
        renderer.Configuracoes.SenhaProprietario = config.SenhaProprietario;
        renderer.Configuracoes.PermitirCopia = config.PermitirCopia;
        renderer.Configuracoes.PermitirImpressao = config.PermitirImpressao;
        var bytes = renderer.GerarPDF(conhecimento);
        cancellationToken.ThrowIfCancellationRequested();
        return bytes;
    }
}