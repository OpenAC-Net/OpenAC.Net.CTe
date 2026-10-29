using System.Xml.Linq;
using OpenAC.Net.CTe.Services;
using System.Globalization;
using OpenAC.Net.Core.Extensions;
using Avalonia.Platform.Storage;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Demo;

public partial class MainWindow
{
    private async Task LoadDocumentsAsync(CancellationToken token)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Carregar CT-e ou CT-e OS", AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("Documentos XML") { Patterns = ["*.xml"] }]
        });
        if (files.Count == 0) return;
        var carregados = new List<CTe>();
        var carregadosOS = new List<CTeOS>();
        var xmls = new List<string>();
        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var xml = await reader.ReadToEndAsync(token);
            token.ThrowIfCancellationRequested();
            AdicionarDocumento(xml, carregados, carregadosOS);
            xmls.Add(xml);
        }
        conhecimentos.Clear(); conhecimentosOS.Clear();
        conhecimentos.AddRange(carregados); conhecimentosOS.AddRange(carregadosOS);
        documentPaths.Clear();
        documentPaths.AddRange(files.Select(file => file.Name));
        DocumentNameText.Text = string.Join(", ", documentPaths);
        DocumentCountText.Text = $"{files.Count} XML";
        AccessKeyText.Text = DefaultAccessKey();
        SentXmlText.Text = string.Join("\n\n", xmls.Select(PrettyXml));
        ResultText.Text = $"{conhecimentos.Count} CT-e e {conhecimentosOS.Count} CT-e OS carregados.\n\nValide o documento antes de enviar. A versão e o ambiente de emissão seguem as configurações selecionadas.";
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task ValidateDocumentsAsync(CancellationToken token)
    {
        RequireDocuments();
        await Task.Run(() =>
        {
            foreach (var cte in conhecimentos) { token.ThrowIfCancellationRequested(); CTeSchemaValidator.Validate(cte.GetXml(), settings, false); }
            foreach (var cte in conhecimentosOS) { token.ThrowIfCancellationRequested(); CTeSchemaValidator.Validate(cte.GetXml(), settings, true); }
        }, token);
        ResultText.Text = "Os documentos carregados foram validados com os schemas da versão selecionada.";
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task SendDocumentsAsync(CancellationToken token)
    {
        RequireDocuments();
        foreach (var documento in conhecimentos) ValidarVersaoComunicacao(documento.InfCTe.Versao);
        foreach (var documento in conhecimentosOS) { ValidarVersaoComunicacao(documento.Versao); ValidarVersaoComunicacao(documento.InfCTe.Versao); }
        var autorizacoes = new List<CTeAutorizacaoResposta>();
        foreach (var documento in conhecimentos) autorizacoes.Add(await client.AutorizarAsync(documento, token));
        foreach (var documento in conhecimentosOS) autorizacoes.Add(await client.AutorizarAsync(documento, token));
        var sent = new List<string>();
        var received = new List<string>();
        var requestEnvelopes = new List<string>();
        var responseEnvelopes = new List<string>();
        void Capture<T>(DFeResposta<T> item) where T : class
        {
            sent.Add(item.XmlEnvio);
            received.Add(item.XmlRetorno);
            requestEnvelopes.Add(item.EnvelopeEnvio);
            responseEnvelopes.Add(item.EnvelopeRetorno);
        }
        foreach (var authorization in autorizacoes) Capture(authorization);
        ShowXml(string.Join("\n\n", sent), string.Join("\n\n", received), string.Join("\n\n", requestEnvelopes), string.Join("\n\n", responseEnvelopes));
        ResultText.Text = $"Documentos autorizados: {autorizacoes.Count(item => item.Autorizado)}\n\n" + string.Join("\n\n", received.Select(PrettyXml));
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task QueryAsync(CancellationToken token)
    {
        var key = string.IsNullOrWhiteSpace(AccessKeyText.Text) ? DefaultAccessKey() : AccessKeyText.Text.OnlyNumbers();
        if (key.Length != 44) throw new ArgumentException("Informe uma chave de acesso com 44 dígitos.");
        ShowResponse(await client.ConsultarAsync(key, token));
    }

    private Task ReceiptAsync(CancellationToken token) =>
        Task.FromException(new NotSupportedException("CT-e 4.00 utiliza autorização síncrona e não possui consulta de recibo."));

    private async Task RegistrationAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Consultar cadastro SEFAZ", new InputField("documento", "CPF ou CNPJ do contribuinte"));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        ShowResponse(await client.ConsultaCadastroAsync(settings.WebServices.UF, values["documento"].OnlyNumbers(), cancellationToken: token));
    }

    private async Task DistributionAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Distribuição de documentos",
            new InputField("documento", "CPF ou CNPJ do interessado"),
            new InputField("nsu", "Último NSU (vazio para consultar um NSU específico)", "0", false),
            new InputField("nsuEspecifico", "NSU específico", "", false));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(values["nsu"]) && string.IsNullOrWhiteSpace(values["nsuEspecifico"]))
            throw new ArgumentException("Informe o último NSU ou um NSU específico.");
        ShowResponse(await client.ConsultarDistribuicaoAsync(new CTeDistribuicaoSolicitacao
        {
            UF = settings.WebServices.UF,
            CpfCnpj = values["documento"].OnlyNumbers(),
            Nsu = values["nsuEspecifico"].Length == 0 ? null : values["nsuEspecifico"].OnlyNumbers(),
            UltimoNsu = values["nsu"].Length == 0 ? null : values["nsu"].OnlyNumbers()
        }, token));
    }

    private async Task CancelDocumentAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Cancelar CT-e",
            new InputField("chave", "Chave de acesso", AccessKeyText.Text ?? DefaultAccessKey()),
            new InputField("protocolo", "Protocolo de autorização"),
            new InputField("cnpj", "CNPJ do emitente"),
            new InputField("justificativa", "Justificativa do cancelamento"),
            new InputField("sequencia", "Sequência do evento", "1"));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        var evento = EventoCTe.Cancelamento(values["protocolo"], values["justificativa"]);
        ShowResponse(await client.EnviarEventoAsync(new CTeEventoSolicitacao
        {
            Sequencia = PositiveNumber(values["sequencia"], "sequência"),
            Chave = values["chave"].OnlyNumbers(), CnpjAutor = values["cnpj"].OnlyNumbers(), Evento = evento
        }, token));
    }

    private Task VoidNumbersAsync(CancellationToken token) =>
        Task.FromException(new NotSupportedException("CT-e 4.00 não possui serviço de inutilização."));

    private string DefaultAccessKey()
    {
        var id = conhecimentos.FirstOrDefault()?.InfCTe.Id ?? conhecimentosOS.FirstOrDefault()?.InfCTe.Id ?? "";
        return id.OnlyNumbers();
    }

    private void RequireDocuments()
    {
        if (conhecimentos.Count + conhecimentosOS.Count == 0)
            throw new InvalidOperationException("Carregue um XML de CT-e ou CT-e OS para continuar.");
    }

    internal static void AdicionarDocumento(string xml, List<CTe> ctes, List<CTeOS> cteOS)
    {
        var root = XElement.Parse(xml);
        XNamespace ns = "http://www.portalfiscal.inf.br/cte";
        var documento = root.Name.LocalName is "CTe" or "CTeOS" ? root
            : root.Elements().FirstOrDefault(element => element.Name == ns + "CTe" || element.Name == ns + "CTeOS")
                ?? throw new ArgumentException("XML deve conter CT-e ou CT-e OS, avulso ou processado.");
        if (documento.Name == ns + "CTe") ctes.Add(CTe.Load(documento.ToString()));
        else if (documento.Name == ns + "CTeOS") cteOS.Add(CTeOS.Load(documento.ToString()));
        else throw new ArgumentException("Namespace do documento inválido.");
    }

    private static void ValidarVersaoComunicacao(CTeVersao versao)
    {
        if (versao != CTeVersao.v400)
            throw new NotSupportedException("CT-e 2.00 e 3.00 permitem apenas leitura, geração e impressão.");
    }

    private static int PositiveNumber(string text, string label, bool allowZero = false)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= (allowZero ? 0 : 1)
            ? value : throw new ArgumentException($"Informe um valor válido para {label}.");
}
