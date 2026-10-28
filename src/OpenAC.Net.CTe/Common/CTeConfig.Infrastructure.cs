using System.ComponentModel;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Storage;

namespace OpenAC.Net.CTe.Configuracao;

public sealed partial class CTeConfig
{
    [Browsable(false)]
    public ICTeHttpTransport HttpTransport { get; set; } = new DesktopCTeHttpTransport();

    [Browsable(false)]
    public ICTeDocumentStore DocumentStore { get; set; } = new DesktopCTeDocumentStore();
}
