namespace OpenAC.Net.CTe.Web;

/// <summary>Deve retornar uma configuração nova e independente por chamada.</summary>
public interface ICTeConfigurationProvider<TConfiguration>
{
    ValueTask<TConfiguration> GetConfigurationAsync(string tenantId = "default",
        CancellationToken cancellationToken = default);
}