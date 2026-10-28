namespace OpenAC.Net.CTe.Web;

public sealed class CTeTechnicalException(string message, Exception innerException)
    : Exception(message, innerException);