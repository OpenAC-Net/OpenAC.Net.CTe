using OpenAC.Net.CTe.Configuracao;

namespace OpenAC.Net.CTe.Web;

internal sealed class DefaultCTeConfigurationProvider(Action<CTeConfig> configure)
    : ICTeConfigurationProvider<CTeConfig>
{
    public ValueTask<CTeConfig> GetConfigurationAsync(string tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var config = new CTeConfig();
        configure(config);
        return ValueTask.FromResult(config);
    }
}