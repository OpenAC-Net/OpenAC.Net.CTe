namespace OpenAC.Net.CTe.DACTe.PDFSharp.Web;

public interface IOpenDACTeWeb
{
    ValueTask<byte[]> GerarPDFAsync(CTeProc conhecimento, string tenantId = "default",
        CancellationToken cancellationToken = default);
}