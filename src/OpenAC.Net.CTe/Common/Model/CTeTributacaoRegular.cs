using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeTributacaoRegular
{
    [DFeElement(TipoCampo.Str, "CSTReg", Ordem=1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CSTReg { get; set; }
    [DFeElement(TipoCampo.Str, "cClassTribReg", Ordem=2, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string CClassTribReg { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqEfetRegIBSUF", Ordem=3, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqEfetRegIBSUF { get; set; }
    [DFeElement(TipoCampo.De2, "vTribRegIBSUF", Ordem=4, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribRegIBSUF { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqEfetRegIBSMun", Ordem=5, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqEfetRegIBSMun { get; set; }
    [DFeElement(TipoCampo.De2, "vTribRegIBSMun", Ordem=6, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribRegIBSMun { get; set; }
    [DFeElement(TipoCampo.De4, "pAliqEfetRegCBS", Ordem=7, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? PAliqEfetRegCBS { get; set; }
    [DFeElement(TipoCampo.De2, "vTribRegCBS", Ordem=8, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VTribRegCBS { get; set; }
}
