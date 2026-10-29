namespace OpenAC.Net.CTe.Web;

public interface IOpenCTeClientFactory
{
    ValueTask<IOpenCTeClient> CriarAsync(string tenantId = "default", CancellationToken cancellationToken = default);
}