namespace OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;

/// <summary>Configuração independente da instância de emissão.</summary>
public sealed class DACTeConfig
{
    public string DiretorioSaida { get; set; } = "PDF";
    public string MensagemRodape { get; set; } = "OpenAC.Net.CTe";
    public byte[] Logo { get; set; }
    public string SenhaUsuario { get; set; }
    public string SenhaProprietario { get; set; }
    public bool PermitirImpressao { get; set; } = true;
    public bool PermitirCopia { get; set; } = true;
}
