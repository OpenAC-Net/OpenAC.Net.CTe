using System;
using System.IO;
using System.Linq;
using OpenAC.Net.CTe.DACTe.PDFSharp.Configuracao;
using OpenAC.Net.CTe.DACTe.PDFSharp.Layout;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace OpenAC.Net.CTe.DACTe.PDFSharp;

/// <summary>Gera PDFs em memória ou arquivo, sem dependência de impressora ou interface gráfica.</summary>
public sealed class OpenDACTe : DACTeBase
{
    public DACTeConfig Configuracoes { get; } = new();

    public byte[] GerarPDF(params CTeProc[] conhecimentos)
    {
        Validate(conhecimentos, nameof(conhecimentos));
        using var document = CreateDocument("DACTE");
        foreach (var proc in conhecimentos)
        {
            var cte = proc.CTe;
            var info = proc.CTeOS?.InfCTe ??
                       cte?.InfCTe ?? throw new ArgumentException("Informações do CT-e não informadas.");
            var key = Key(info.Id);
            var protocol = proc.ProtCTe?.InfProt;
            var warning = cte?.Cancelada == true || proc.CTeOS?.Cancelada == true || protocol?.CStat == 101 ||
                          protocol?.CStat == 151 || protocol?.CStat == 155
                ? "CT-e CANCELADO - SEM VALOR FISCAL"
                : "";
            if ((int)info.Ide.TpAmb == 2) warning += "  EMITIDO EM AMBIENTE DE HOMOLOGAÇÃO - SEM VALOR FISCAL";
            if (protocol == null || (protocol.CStat != 100 && protocol.CStat != 150))
                warning += "  SEM AUTORIZAÇÃO DE USO";
            using var report = new DACTeReport(document, Configuracoes, LayoutImpressao == DACTeLayout.Paisagem,
                (int)info.Ide.Mod == 67
                    ? "DACTE OS - Documento Auxiliar do CT-e Outros Serviços"
                    : "DACTE - Documento Auxiliar do Conhecimento de Transporte Eletrônico",
                $"CT-e {info.Ide.NCT:D9}  Série {info.Ide.Serie:D3}  Modelo {(int)info.Ide.Mod}  Emissão {info.Ide.DhEmi:dd/MM/yyyy HH:mm:ss}  Chave {key}",
                warning.Trim());
            report.Section("RECIBO DE ENTREGA", [
                "Declaro que recebi os volumes deste conhecimento em perfeito estado.",
                "Nome: ________________________ Documento: ________________________ Data: ____/____/______ Assinatura: ________________________"
            ]);
            report.Logo();
            var address = info.Emit.EnderEmit;
            report.Section("EMITENTE", [
                info.Emit.XNome, $"CNPJ: {info.Emit.CNPJ}   IE: {info.Emit.IE}",
                $"{address?.XLgr}, {address?.Nro} {address?.XCpl} - {address?.XBairro}",
                $"{address?.XMun}/{address?.UF}  CEP: {address?.CEP}  Fone: {address?.Fone}"
            ]);
            report.AccessKey(key, proc.CTeOS?.InfCTeSupl?.QrCode ?? cte?.InfCTeSupl?.QrCode);
            report.Section("IDENTIFICAÇÃO DA PRESTAÇÃO", [
                $"CFOP: {info.Ide.CFOP}  Natureza: {info.Ide.NatOp}",
                $"Tipo: {info.Ide.TpCTe}  Serviço: {info.Ide.TpServ}  Modal: {info.Ide.Modal}  Forma de emissão: {info.Ide.TpEmis}",
                $"Início: {info.Ide.XMunIni}/{info.Ide.UFIni}  Término: {info.Ide.XMunFim}/{info.Ide.UFFim}",
                $"Protocolo: {protocol?.NProt}  Recebimento: {protocol?.DhRecbto:dd/MM/yyyy HH:mm:ss}  Situação: {protocol?.CStat} {protocol?.XMotivo}"
            ]);
            if (!string.IsNullOrEmpty(info.Rem?.XNome) || !string.IsNullOrEmpty(info.Rem?.CNPJ) ||
                !string.IsNullOrEmpty(info.Rem?.CPF))
                report.Section("REMETENTE", DACTeReport.Fields(info.Rem));
            if (!string.IsNullOrEmpty(info.Dest?.XNome) || !string.IsNullOrEmpty(info.Dest?.CNPJ) ||
                !string.IsNullOrEmpty(info.Dest?.CPF))
                report.Section("DESTINATÁRIO", DACTeReport.Fields(info.Dest));
            if (!string.IsNullOrEmpty(info.Exped?.XNome) || !string.IsNullOrEmpty(info.Exped?.CNPJ) ||
                !string.IsNullOrEmpty(info.Exped?.CPF))
                report.Section("EXPEDIDOR", DACTeReport.Fields(info.Exped));
            if (!string.IsNullOrEmpty(info.Receb?.XNome) || !string.IsNullOrEmpty(info.Receb?.CNPJ) ||
                !string.IsNullOrEmpty(info.Receb?.CPF))
                report.Section("RECEBEDOR", DACTeReport.Fields(info.Receb));
            report.Section("TOMADOR DO SERVIÇO", Tomador(info));
            report.Section("VALORES DA PRESTAÇÃO",
                new[]
                {
                    $"Valor total: R$ {DACTeReport.Money(info.VPrest.VTPrest)}  Valor a receber: R$ {DACTeReport.Money(info.VPrest.VRec)}"
                }.Concat(DACTeReport.Fields(info.VPrest.Comp)));
            report.Section("INFORMAÇÕES TRIBUTÁRIAS", DACTeReport.Fields(info.Imp?.ICMS)
                .Concat(info.Imp?.ICMSUFFim?.PICMSInterPart > 0 ? DACTeReport.Fields(info.Imp.ICMSUFFim) : [])
                .Concat([
                    info.Imp?.InfAdFisco,
                    info.Imp?.VTotTrib.HasValue == true
                        ? "Total de tributos: R$ " + DACTeReport.Money(info.Imp.VTotTrib.Value)
                        : null
                ]));
            report.Section("IBS / CBS", DACTeReport.Fields(info.Imp?.IBSCBS));
            report.Section("TRIBUTOS FEDERAIS", DACTeReport.Fields(info.Imp?.InfTribFed));
            if (info.InfoCTe is CTeNormal normal)
            {
                report.Section("INFORMAÇÕES DA CARGA", DACTeReport.Fields(normal.InfCarga));
                report.Section("DOCUMENTOS ORIGINÁRIOS", DACTeReport.Fields(normal.InfDoc));
                report.Section("DOCUMENTOS ANTERIORES", DACTeReport.Fields(normal.DocAnt));
                report.Section("FLUXO DA CARGA", DACTeReport.Fields(info.Compl?.Fluxo));
                report.Section("DADOS DO MODAL " + info.Ide.Modal, DACTeReport.Fields(normal.InfModal?.Modal));
                if (normal.Cobr != null && (!string.IsNullOrEmpty(normal.Cobr.Fat?.NFat) || normal.Cobr.Dup?.Count > 0))
                    report.Section("COBRANÇA", DACTeReport.Fields(normal.Cobr));
            }
            else if (info.InfoCTe is CTeNormalOS os)
            {
                report.Section("INFORMAÇÕES DO SERVIÇO", DACTeReport.Fields(os.InfServico));
                report.Section("DOCUMENTOS REFERENCIADOS", DACTeReport.Fields(os.InfDocRef));
                report.Section("DADOS DO MODAL", DACTeReport.Fields(os.InfModal?.Modal));
                report.Section("SEGURO", DACTeReport.Fields(os.Seg));
            }
            else
                report.Section("DOCUMENTOS COMPLEMENTADOS / SUBSTITUÍDOS", info.Complementos.Count > 0
                    ? DACTeReport.Fields(info.Complementos)
                    : DACTeReport.Fields(info.InfoCTe));

            report.Section("OBSERVAÇÕES GERAIS", new[] { info.Compl?.XObs, info.Compl?.XCaracAd, info.Compl?.XCaracSer }
                .Concat(DACTeReport.Fields(info.Compl?.ObsCont)).Concat(DACTeReport.Fields(info.Compl?.ObsFisco)));
        }

        return Save(document);
    }

    public byte[] GerarEventoPDF(params CTeProcEvento[] eventos)
    {
        Validate(eventos, nameof(eventos));
        using var document = CreateDocument("Eventos CT-e");
        foreach (var evento in eventos)
        {
            var info = evento.EventoCTe?.InfEvento ??
                       throw new ArgumentException("Informações do evento não informadas.");
            var ret = evento.RetEventoCTe?.InfEvento;
            var warning = (int)info.Ambiente == 2 ? "HOMOLOGAÇÃO - SEM VALOR FISCAL" : "";
            if (ret == null || (ret.CStat != 135 && ret.CStat != 136)) warning += " EVENTO SEM REGISTRO";
            using var report = new DACTeReport(document, Configuracoes, false, "REPRESENTAÇÃO DO EVENTO DO CT-e",
                $"Chave: {info.Chave}  Evento: {info.TpEvento}  Sequência: {info.NSeqEvento}", warning);
            report.Section("IDENTIFICAÇÃO", [
                $"CNPJ: {info.CNPJ}  Data: {info.DhEvento:dd/MM/yyyy HH:mm:ss}",
                $"Protocolo: {ret?.Protocolo}  Registro: {ret?.DhRegEvento:dd/MM/yyyy HH:mm:ss}",
                $"Situação: {ret?.CStat} {ret?.Motivo}"
            ]);
            report.Section("DETALHES DO EVENTO", DACTeReport.Fields(info.detEvento?.Evento));
        }

        return Save(document);
    }

    public byte[] GerarInutilizacaoPDF(InutilizaoResposta inutilizacao)
    {
        if (inutilizacao?.Resultado?.InfInut == null) throw new ArgumentNullException(nameof(inutilizacao));
        using var document = CreateDocument("Inutilização CT-e");
        var info = inutilizacao.Resultado.InfInut;
        using (var report = new DACTeReport(document, Configuracoes, false, "INUTILIZAÇÃO DE NUMERAÇÃO DO CT-e",
                   $"CNPJ: {info.CNPJ}  Série: {info.Serie}  Faixa: {info.NCtIni} a {info.NCtFin}",
                   (int)info.TpAmb == 2 ? "HOMOLOGAÇÃO - SEM VALOR FISCAL" :
                   info.CStat == 102 ? "" : "INUTILIZAÇÃO NÃO HOMOLOGADA"))
            report.Section("PROTOCOLO DE INUTILIZAÇÃO", DACTeReport.Fields(info));
        return Save(document);
    }

    public void GerarPDF(Stream destino, params CTeProc[] conhecimentos)
    {
        if (destino is not { CanWrite: true })
            throw new ArgumentException("Stream de destino deve permitir escrita.", nameof(destino));
        var bytes = GerarPDF(conhecimentos);
        destino.Write(bytes, 0, bytes.Length);
    }

    // O componente produz PDFs. A impressão física fica a cargo do visualizador ou da aplicação.
    public override void Imprimir(CTeProc[] conhecimentos) => ImprimirPDF(conhecimentos);

    public override void ImprimirPDF(CTeProc[] conhecimentos)
    {
        Validate(conhecimentos, nameof(conhecimentos));
        foreach (var proc in conhecimentos)
            Write(Key(proc.CTeOS?.InfCTe?.Id ?? proc.CTe?.InfCTe?.Id) + "-dacte.pdf", GerarPDF(proc));
    }

    public override void ImprimirEvento(CTeProcEvento[] eventos) => ImprimirEventoPDF(eventos);

    public override void ImprimirEventoPDF(CTeProcEvento[] eventos)
    {
        Validate(eventos, nameof(eventos));
        foreach (var evento in eventos)
        {
            var info = evento.EventoCTe?.InfEvento ?? throw new ArgumentException("Evento não informado.");
            Write($"{Key(info.Chave)}-{(int)info.TpEvento}-{info.NSeqEvento:D2}-evento.pdf", GerarEventoPDF(evento));
        }
    }

    public override void ImprimirInutilizacao(InutilizaoResposta inutilizao) => ImprimirInutilizacaoPDF(inutilizao);

    public override void ImprimirInutilizacaoPDF(InutilizaoResposta inutilizao)
        => Write("inutilizacao-" + Guid.NewGuid().ToString("N") + ".pdf", GerarInutilizacaoPDF(inutilizao));

    private static System.Collections.Generic.IEnumerable<string> Tomador(CTeInfCTe info)
    {
        if (info.Toma != null) return DACTeReport.Fields(info.Toma);
        if (info.Ide.Tomador is CTeToma4 other) return DACTeReport.Fields(other);
        var type = info.Ide.Tomador is CTeToma3 toma ? toma.Toma
            : info.Ide.Tomador is CTeToma03 toma03 ? toma03.Toma : (CTeTomador?)null;
        return type switch
        {
            CTeTomador.Remetente => DACTeReport.Fields(info.Rem),
            CTeTomador.Destinatario => DACTeReport.Fields(info.Dest),
            CTeTomador.Expedidor => DACTeReport.Fields(info.Exped),
            CTeTomador.Recebedor => DACTeReport.Fields(info.Receb),
            _ => DACTeReport.Fields(info.Ide.Tomador)
        };
    }

    private void Write(string filename, byte[] bytes)
    {
        Directory.CreateDirectory(Configuracoes.DiretorioSaida);
        File.WriteAllBytes(Path.Combine(Configuracoes.DiretorioSaida, filename), bytes);
    }

    private PdfDocument CreateDocument(string title)
    {
        var document = new PdfDocument();
        document.Info.Title = title;
        document.Info.Creator = "OpenAC.Net.CTe.DACTe.PDFSharp";
        if (!string.IsNullOrEmpty(Configuracoes.SenhaUsuario) || !string.IsNullOrEmpty(Configuracoes.SenhaProprietario))
        {
            document.SecuritySettings.UserPassword = Configuracoes.SenhaUsuario ?? "";
            document.SecuritySettings.OwnerPassword = Configuracoes.SenhaProprietario ?? Guid.NewGuid().ToString("N");
            document.SecuritySettings.PermitPrint = Configuracoes.PermitirImpressao;
            document.SecuritySettings.PermitExtractContent = Configuracoes.PermitirCopia;
            document.SecuritySettings.PermitModifyDocument = false;
        }

        return document;
    }

    private static byte[] Save(PdfDocument document)
    {
        var font = new XFont("Liberation Sans", 6);
        for (var i = 0; i < document.PageCount; i++)
        {
            var page = document.Pages[i];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            graphics.DrawString($"Página {i + 1} de {document.PageCount}", font, XBrushes.Black,
                new XRect(page.Width.Point - 105, page.Height.Point - 25, 88, 10), XStringFormats.TopRight);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static string Key(string value)
    {
        if (value?.StartsWith("CTe", StringComparison.Ordinal) == true) value = value.Substring(3);
        if (value is not { Length: 44 } || value.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("Chave de acesso deve conter 44 dígitos.", nameof(value));
        return value;
    }

    private static void Validate<T>(T[] values, string name)
    {
        if (values == null || values.Length == 0 || values.Any(value => value == null))
            throw new ArgumentException("Informe pelo menos um documento válido.", name);
    }
}