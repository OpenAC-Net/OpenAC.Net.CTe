using OpenAC.Net.DFe.Core.Http;

namespace OpenAC.Net.CTe.Services;

/// <summary>Utiliza o pool compartilhado do Core, preservando um único envio por chamada SOAP.</summary>
public class DesktopCTeHttpTransport : DFeHttpTransport
{
    protected override bool ManageRetries => false;
}
