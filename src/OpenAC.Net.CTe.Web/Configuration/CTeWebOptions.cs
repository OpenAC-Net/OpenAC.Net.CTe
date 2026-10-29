namespace OpenAC.Net.CTe.Web;

public sealed class CTeWebOptions
{
    public bool CircuitBreakerHabilitado { get; set; }
    
    public TimeSpan TimeoutTentativa { get; set; } = TimeSpan.FromSeconds(60);
    
    public TimeSpan TimeoutOperacao { get; set; } = TimeSpan.FromSeconds(100);
    
    public TimeSpan TempoVidaHandler { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan TempoVidaConexaoPool { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan MargemValidadeCertificado { get; set; } = TimeSpan.FromMinutes(5);

    public bool PersistenciaHabilitada { get; set; }

    public string DiretorioDocumentos { get; set; } = Path.Combine(AppContext.BaseDirectory, "CTe");
}