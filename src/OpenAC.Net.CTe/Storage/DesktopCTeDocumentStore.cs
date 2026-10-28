using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAC.Net.CTe.Storage;

/// <summary>Gravação assíncrona com substituição atômica no mesmo diretório.</summary>
public sealed class DesktopCTeDocumentStore : ICTeDocumentStore
{
    public async Task SaveAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temporary = path + "." + System.Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = new UTF8Encoding(false).GetBytes(content);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true))
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else
            {
                try { File.Move(temporary, path); }
                catch (IOException) when (File.Exists(path)) { File.Replace(temporary, path, null); }
            }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
