namespace OpenAC.Net.CTe.Web;

/// <summary>Fornece a configuração da empresa. A fábrica captura uma cópia antes de configurar a operação.</summary>
public interface ICTeConfigurationProvider<TConfiguration>
{
    ValueTask<TConfiguration> GetConfigurationAsync(string tenantId = "default",
        CancellationToken cancellationToken = default);
}