namespace OpenAC.Net.CTe;

/// <summary>Dados do evento a registrar para uma chave de CT-e.</summary>
public sealed class CTeEventoSolicitacao
{
    public string Chave { get; set; }
    public string CnpjAutor { get; set; }
    public int Sequencia { get; set; } = 1;
    public IEventoCTe Evento { get; set; }
}
