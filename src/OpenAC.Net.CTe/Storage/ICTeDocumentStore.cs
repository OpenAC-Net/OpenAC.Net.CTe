using System.Threading;
using System.Threading.Tasks;

namespace OpenAC.Net.CTe.Storage;

/// <summary>Persistência substituível para XML fiscal e envelopes SOAP.</summary>
public interface ICTeDocumentStore
{
    Task SaveAsync(string path, string content, CancellationToken cancellationToken = default);
}
