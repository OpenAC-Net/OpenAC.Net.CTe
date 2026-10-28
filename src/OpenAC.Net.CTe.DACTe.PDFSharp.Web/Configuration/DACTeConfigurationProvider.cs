using OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;

namespace OpenAC.Net.CTe.DACTe.PDFSharp.Web;

internal sealed class DACTeConfigurationProvider(Action<DACTeConfig> configure)
    : OpenAC.Net.CTe.Web.ICTeConfigurationProvider<DACTeConfig>
{
    public ValueTask<DACTeConfig> GetConfigurationAsync(string tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = new DACTeConfig();
        configure(options);
        return ValueTask.FromResult(options);
    }
}