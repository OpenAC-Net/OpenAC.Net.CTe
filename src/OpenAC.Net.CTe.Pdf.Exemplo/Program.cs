using OpenAC.Net.CTe;
using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.DFe.Core.Common;

if (args.Length > 0 && args[0] != "--exemplos")
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Uso: dotnet run -- <cte-processado.xml> <saida.pdf> | --exemplos [diretorio]");
        return 1;
    }
    var input = CTeProc.Load(args[0]);
    File.WriteAllBytes(args[1], new OpenDACTe().GerarPDF(input));
    Console.WriteLine(Path.GetFullPath(args[1]));
    return 0;
}

var directory = args.Length > 1 ? args[1] : Path.Combine(Environment.CurrentDirectory, "Exemplos_PDF");
Directory.CreateDirectory(directory);
var renderer = new OpenDACTe();
renderer.Configuracoes.MensagemRodape = "OpenAC.Net.CTe - Exemplo com dados fictícios";
var proc = Sample();
Write("DACTe_Homologacao.pdf", renderer.GerarPDF(proc));
renderer.LayoutImpressao = DACTeLayout.Paisagem;
Write("DACTe_Paisagem.pdf", renderer.GerarPDF(proc));
renderer.LayoutImpressao = DACTeLayout.Retrato;
proc.CTe.Cancelada = true;
Write("DACTe_Cancelado.pdf", renderer.GerarPDF(proc));
proc.CTe.Cancelada = false;
proc.CTe.InfCTe.Compl.XObs = string.Join("\n", Enumerable.Range(1, 300).Select(i => $"Observação {i:D3}: Transporte de produtos frágeis. Conferir volumes no recebimento."));
Write("DACTe_Continuacao.pdf", renderer.GerarPDF(proc));
proc.CTe.InfCTe.Compl.XObs = "Entrega agendada. Conferir os volumes no recebimento.";
renderer.Configuracoes.SenhaUsuario = "123456";
renderer.Configuracoes.SenhaProprietario = "exemplo-proprietario";
Write("DACTe_ComSenha_123456.pdf", renderer.GerarPDF(proc));
renderer.Configuracoes.SenhaUsuario = null;
renderer.Configuracoes.SenhaProprietario = null;
var evento = new CTeProcEvento();
evento.EventoCTe.InfEvento.Chave = proc.CTe.InfCTe.Id.Substring(3);
evento.EventoCTe.InfEvento.Ambiente = DFeTipoAmbiente.Homologacao;
evento.EventoCTe.InfEvento.TpEvento = CTeTipoEvento.Cancelamento;
evento.EventoCTe.InfEvento.DhEvento = DateTimeOffset.Now;
evento.EventoCTe.InfEvento.NSeqEvento = 1;
evento.EventoCTe.InfEvento.detEvento = new DetEvento { Evento = new CTeEvCancCTe { DescEvento = "Cancelamento", NProt = "135260000000123", XJust = "Documento emitido com dados incorretos." } };
evento.RetEventoCTe.InfEvento.CStat = 135;
evento.RetEventoCTe.InfEvento.Protocolo = "135260000000124";
Write("Evento_Cancelamento.pdf", renderer.GerarEventoPDF(evento));
Console.WriteLine("Exemplos: " + Path.GetFullPath(directory));
return 0;

void Write(string name, byte[] bytes) => File.WriteAllBytes(Path.Combine(directory, name), bytes);

static CTeProc Sample()
{
    var proc = new CTeProc();
    var info = proc.CTe.InfCTe;
    info.Id = "CTe35261012345678000195570010000001231000001234";
    info.Ide.Mod = ModeloCTe.CTe;
    info.Ide.NCT = 123;
    info.Ide.Serie = 1;
    info.Ide.DhEmi = DateTimeOffset.Parse("2026-10-26T12:00:00-04:00");
    info.Ide.TpAmb = DFeTipoAmbiente.Homologacao;
    info.Ide.CFOP = 6353;
    info.Ide.NatOp = "Prestação de serviço de transporte";
    info.Ide.Modal = CTeModal.Rodoviario;
    info.Ide.XMunIni = "São Paulo";
    info.Ide.UFIni = DFeSiglaUF.SP;
    info.Ide.XMunFim = "Campo Grande";
    info.Ide.UFFim = DFeSiglaUF.MS;
    info.Emit.XNome = "Transportadora Exemplo Ltda";
    info.Emit.CNPJ = "12345678000195";
    info.Emit.IE = "123456789";
    info.Emit.EnderEmit.XLgr = "Rua dos Transportes";
    info.Emit.EnderEmit.Nro = "123";
    info.Emit.EnderEmit.XBairro = "Centro";
    info.Emit.EnderEmit.XMun = "São Paulo";
    info.Emit.EnderEmit.UF = DFeSiglaUF.SP;
    info.Emit.EnderEmit.CEP = "01001000";
    info.Rem.XNome = "Remetente Exemplo Ltda";
    info.Rem.CNPJ = "11222333000181";
    info.Rem.EnderReme.XLgr = "Avenida Brasil";
    info.Rem.EnderReme.Nro = "100";
    info.Rem.EnderReme.XMun = "São Paulo";
    info.Rem.EnderReme.UF = DFeSiglaUF.SP;
    info.Dest.XNome = "Destinatário Exemplo Ltda";
    info.Dest.CNPJ = "44555666000181";
    info.Dest.EnderDest.XLgr = "Rua das Mercadorias";
    info.Dest.EnderDest.Nro = "200";
    info.Dest.EnderDest.XMun = "Campo Grande";
    info.Dest.EnderDest.UF = DFeSiglaUF.MS;
    info.Ide.Tomador = new CTeToma3 { Toma = CTeTomador.Remetente };
    info.VPrest.VTPrest = info.VPrest.VRec = 1500;
    info.VPrest.Comp.Add(new CTePrestComp { XNome = "Frete", VComp = 1500 });
    info.Imp.ICMS.ICMS = new CTeICMS00 { VBC = 1500, PICMS = 12, VICMS = 180 };
    info.InfoCTe = new CTeNormal
    {
        InfCarga = new CTeInfCarga { ProPred = "Produtos diversos", VCarga = 25000 },
        InfModal = new CTeInfModal { Modal = new CTeRodoModal { RNTRC = "12345678" } }
    };
    ((CTeNormal)info.InfoCTe).InfCarga.InfQ.Add(new CTeInfQ { TpMed = "PESO BRUTO", QCarga = 1500 });
    ((CTeNormal)info.InfoCTe).InfDoc.Add(new CTeInfNFe { Chave = "35261011222333000181550010000004561000004567" });
    info.Compl.XObs = "Entrega agendada. Conferir os volumes no recebimento.";
    proc.ProtCTe.InfProt.CStat = 100;
    proc.ProtCTe.InfProt.NProt = "135260000000123";
    proc.ProtCTe.InfProt.DhRecbto = new DateTime(2026, 10, 26, 12, 1, 0);
    proc.CTe.InfCTeSupl = new CTeInfCteSupl { QrCode = "https://example.test/consulta?chCTe=" + info.Id.Substring(3) };
    return proc;
}
