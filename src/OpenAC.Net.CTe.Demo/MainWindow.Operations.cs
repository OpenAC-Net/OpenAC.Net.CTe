using System.Globalization;
using OpenAC.Net.Core.Extensions;
using Avalonia.Platform.Storage;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.DFe.Core.Common;
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
        using var temporary = new OpenCTe();
        var xmls = new List<string>();
        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var xml = await reader.ReadToEndAsync(token);
            token.ThrowIfCancellationRequested();
            temporary.Conhecimentos.Load(xml);
            xmls.Add(xml);
        }
        client.Conhecimentos.Clear();
        client.Conhecimentos.AddRange(temporary.Conhecimentos.CTe);
        client.Conhecimentos.AddRange(temporary.Conhecimentos.CTeOS);
        documentPaths.Clear();
        documentPaths.AddRange(files.Select(file => file.Name));
        DocumentNameText.Text = string.Join(", ", documentPaths);
        DocumentCountText.Text = $"{files.Count} XML";
        AccessKeyText.Text = DefaultAccessKey();
        SentXmlText.Text = string.Join("\n\n", xmls.Select(PrettyXml));
        ResultText.Text = $"{temporary.Conhecimentos.CTe.Length} CT-e e {temporary.Conhecimentos.CTeOS.Length} CT-e OS carregados.\n\nValide o documento antes de enviar. A versão e o ambiente de emissão seguem as configurações selecionadas.";
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task ValidateDocumentsAsync(CancellationToken token)
    {
        RequireDocuments();
        await Task.Run(() => { token.ThrowIfCancellationRequested(); client.Conhecimentos.Validar(); }, token);
        ResultText.Text = "Os documentos carregados foram validados com os schemas da versão selecionada.";
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task SendDocumentsAsync(CancellationToken token)
    {
        RequireDocuments();
        var response = await client.EnviarAsync(BatchText.Text?.Trim() ?? "1", imprimir: false, cancellationToken: token);
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
        if (response.RecepcaoResposta != null) Capture(response.RecepcaoResposta);
        if (response.RetRecepcaoResposta != null) Capture(response.RetRecepcaoResposta);
        foreach (var authorization in response.AutorizacoesSincronas) Capture(authorization);
        ShowXml(string.Join("\n\n", sent), string.Join("\n\n", received), string.Join("\n\n", requestEnvelopes), string.Join("\n\n", responseEnvelopes));
        ResultText.Text = $"Documentos autorizados: {response.CTeAutorizados.Length}\n\n" + string.Join("\n\n", received.Select(PrettyXml));
        ResponseTabs.SelectedIndex = 0;
    }

    private async Task QueryAsync(CancellationToken token)
    {
        var key = string.IsNullOrWhiteSpace(AccessKeyText.Text) ? DefaultAccessKey() : AccessKeyText.Text.OnlyNumbers();
        if (key.Length != 44) throw new ArgumentException("Informe uma chave de acesso com 44 dígitos.");
        ShowResponse(await client.ConsultarAsync(key, token));
    }

    private async Task ReceiptAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Consultar recibo de lote", new InputField("recibo", "Número do recibo"));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        ShowResponse(await client.ConsultaReciboAsync(values["recibo"], token));
    }

    private async Task RegistrationAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Consultar cadastro SEFAZ", new InputField("documento", "CPF ou CNPJ do contribuinte"));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        ShowResponse(await client.ConsultaCadastroAsync(client.Configuracoes.WebServices.UF, values["documento"].OnlyNumbers(), cancellationToken: token));
    }

    private async Task DistributionAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Distribuição de documentos",
            new InputField("documento", "CPF ou CNPJ do interessado"),
            new InputField("nsu", "Último NSU (vazio para consulta por chave)", "0", false),
            new InputField("chave", "Chave de acesso (para consulta por chave)", AccessKeyText.Text ?? "", false));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(values["nsu"]) && string.IsNullOrWhiteSpace(values["chave"]))
            throw new ArgumentException("Informe o último NSU ou uma chave de acesso.");
        ShowResponse(await client.ConsultarDistribuicaoAsync(client.Configuracoes.WebServices.UF,
            values["documento"].OnlyNumbers(), chave: values["nsu"].Length == 0 ? values["chave"].OnlyNumbers() : "",
            ultNsu: values["nsu"].OnlyNumbers(), cancellationToken: token));
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
        ShowResponse(await client.EnviarEventoAsync(PositiveNumber(values["sequencia"], "sequência"),
            values["chave"].OnlyNumbers(), values["cnpj"].OnlyNumbers(), evento, token));
    }

    private async Task VoidNumbersAsync(CancellationToken token)
    {
        var values = await InputDialog.AskAsync(this, "Inutilizar numeração CT-e",
            new InputField("cnpj", "CNPJ do emitente"),
            new InputField("ano", "Ano", DateTime.Now.ToString("yy", CultureInfo.InvariantCulture)),
            new InputField("serie", "Série", "1"),
            new InputField("inicio", "Número inicial"),
            new InputField("fim", "Número final"),
            new InputField("justificativa", "Justificativa da inutilização"));
        if (values == null) return;
        token.ThrowIfCancellationRequested();
        ShowResponse(await client.InutilizacaoAsync(values["cnpj"].OnlyNumbers(), values["justificativa"],
            PositiveNumber(values["ano"], "ano") % 100, ModeloCTe.CTe,
            PositiveNumber(values["serie"], "série", allowZero: true), PositiveNumber(values["inicio"], "número inicial"),
            PositiveNumber(values["fim"], "número final"), token));
    }

    private string DefaultAccessKey()
    {
        var id = client.Conhecimentos.CTe.FirstOrDefault()?.InfCTe.Id ?? client.Conhecimentos.CTeOS.FirstOrDefault()?.InfCTe.Id ?? "";
        return id.OnlyNumbers();
    }

    private void RequireDocuments()
    {
        if (client.Conhecimentos.CTe.Length + client.Conhecimentos.CTeOS.Length == 0)
            throw new InvalidOperationException("Carregue um XML de CT-e ou CT-e OS para continuar.");
    }

    private static int PositiveNumber(string text, string label, bool allowZero = false)
        => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= (allowZero ? 0 : 1)
            ? value : throw new ArgumentException($"Informe um valor válido para {label}.");
}
