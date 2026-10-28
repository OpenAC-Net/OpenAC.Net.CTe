using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.CTe.DACTe.PDFSharp.Web;
using OpenAC.Net.CTe.Eventos;
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Pdf.IO;

namespace OpenAC.Net.CTe.Tests;

public class PdfTests
{
    public const string Key = "35261012345678000195570010000001231000001234";

    [Test]
    [Arguments(DACTeLayout.Retrato)]
    [Arguments(DACTeLayout.Paisagem)]
    public async Task GeneratesReadablePdfWithExpectedOrientation(DACTeLayout layout)
    {
        var renderer = new OpenDACTe { LayoutImpressao = layout };
        var bytes = renderer.GerarPDF(Sample());
        await Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 5)).IsEqualTo("%PDF-");
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        await Assert.That(document.PageCount > 0).IsTrue();
        await Assert.That(document.Pages[0].Width.Point > document.Pages[0].Height.Point).IsEqualTo(layout == DACTeLayout.Paisagem);
    }

    [Test]
    public async Task LongObservationsContinueAcrossPagesAndOutputStreamStaysOpen()
    {
        var renderer = new OpenDACTe();
        var sample = Sample();
        sample.CTe.InfCTe.Compl.XObs = string.Join("\n", Enumerable.Range(0, 500).Select(i => $"Linha {i:D4}: Mercadoria frágil, transportar com cuidado."));
        using var stream = new MemoryStream();
        renderer.GerarPDF(stream, sample);
        await Assert.That(stream.CanWrite).IsTrue();
        stream.Position = 0;
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        await Assert.That(document.PageCount >= 5).IsTrue();
        await Assert.That(document.PageCount < 30).IsTrue();
    }

    [Test]
    public async Task EncryptsPdfAndRequiresConfiguredPassword()
    {
        var renderer = new OpenDACTe();
        renderer.Configuracoes.SenhaUsuario = "secret";
        renderer.Configuracoes.SenhaProprietario = "owner";
        var bytes = renderer.GerarPDF(Sample());
        using var wrong = new MemoryStream(bytes);
        Assert.ThrowsExactly<PdfReaderException>(() => PdfReader.Open(wrong, "wrong", PdfDocumentOpenMode.Import));
        using var correct = new MemoryStream(bytes);
        using var document = PdfReader.Open(correct, "secret", PdfDocumentOpenMode.Import);
        await Assert.That(document.PageCount > 0).IsTrue();
    }

    [Test]
    public async Task RegistersPdfServiceWithoutDependingOnEmissionOrCertificate()
    {
        var services = new ServiceCollection().AddDACTe();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IOpenDACTeWeb>();
        var bytes = await renderer.GerarPDFAsync(Sample());
        await Assert.That(bytes.Length > 1000).IsTrue();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await renderer.GerarPDFAsync(Sample(), cancellationToken: new CancellationToken(true)));
    }

    [Test]
    public async Task GeneratesCancellationAndCorrectionEvents()
    {
        var renderer = new OpenDACTe();
        var evento = new CTeProcEvento();
        evento.EventoCTe.InfEvento.Chave = Key;
        evento.EventoCTe.InfEvento.detEvento = new DetEvento { Evento = new CTeEvCancCTe { XJust = "Cancelamento de teste", NProt = "135260000000123" } };
        evento.RetEventoCTe.InfEvento.CStat = 135;
        var bytes = renderer.GerarEventoPDF(evento);
        using var stream = new MemoryStream(bytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        await Assert.That(document.PageCount).IsEqualTo(1);
        evento.EventoCTe.InfEvento.detEvento.Evento = new CTeEvCCeCTe { InfCorrecao = [new CTeInfCorrecao { GrupoAlterado = "ide", campoAlterado = "natOp", valorAlterado = "Prestação" }] };
        await Assert.That(renderer.GerarEventoPDF(evento).Length > 1000).IsTrue();
    }

    internal static CTeProc Sample()
    {
        var proc = new CTeProc();
        var info = proc.CTe.InfCTe;
        info.Id = "CTe" + Key;
        info.Ide.NCT = 123;
        info.Ide.Serie = 1;
        info.Ide.Mod = ModeloCTe.CTe;
        info.Ide.DhEmi = DateTimeOffset.Parse("2026-10-26T12:00:00-04:00");
        info.Ide.NatOp = "Prestação de serviço de transporte";
        info.Ide.XMunIni = "São Paulo";
        info.Ide.XMunFim = "Campo Grande";
        info.Emit.XNome = "Transportadora Exemplo Ltda";
        info.Emit.CNPJ = "12345678000195";
        info.Emit.EnderEmit.XLgr = "Rua dos Transportes";
        info.Emit.EnderEmit.Nro = "123";
        info.Emit.EnderEmit.XMun = "São Paulo";
        info.Rem.XNome = "Remetente Exemplo";
        info.Dest.XNome = "Destinatário Exemplo";
        info.VPrest.VTPrest = 1500;
        info.VPrest.VRec = 1500;
        info.InfoCTe = new CTeNormal { InfModal = new CTeInfModal { Modal = new CTeRodoModal { RNTRC = "12345678" } } };
        ((CTeNormal)info.InfoCTe).InfCarga.ProPred = "Produtos diversos";
        ((CTeNormal)info.InfoCTe).InfCarga.VCarga = 25000;
        proc.ProtCTe.InfProt.CStat = 100;
        proc.ProtCTe.InfProt.NProt = "135260000000123";
        proc.CTe.InfCTeSupl = new CTeInfCteSupl { QrCode = "https://example.test/consulta?chCTe=" + Key };
        return proc;
    }
}
