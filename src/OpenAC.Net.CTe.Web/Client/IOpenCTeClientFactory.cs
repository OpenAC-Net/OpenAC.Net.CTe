namespace OpenAC.Net.CTe.Web;

public interface IOpenCTeClientFactory
{
    ValueTask<OpenCTe> CreateAsync(string tenantId = "default", CancellationToken cancellationToken = default);
}