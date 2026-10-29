using OpenAC.Net.CTe.Configuracao;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;
using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Demo;

public partial class MainWindow : Window
{
    private readonly CTeConfig settings;
    private IOpenCTeClient client;
    private readonly DemoServiceCatalog catalog = new();
    private readonly List<CTe> conhecimentos = [];
    private readonly List<CTeOS> conhecimentosOS = [];
    private readonly OpenDACTe renderer = new();
    private readonly ObservableCollection<ServiceEndpoint> endpoints = [];
    private readonly List<string> documentPaths = [];
    private CancellationTokenSource? operation;
    private OpenConfig? configuration;
    private string configurationPath = "";
    private bool closeAfterOperation;
    private bool closed;
    private bool initialized;

    public MainWindow() : this(new CTeConfig(), null) { }

    internal MainWindow(CTeConfig configuracoes, string? configurationFile)
    {
        settings = configuracoes.Copiar();
        client = new OpenCTe(settings);
        InitializeComponent();
        ServiceList.ItemsSource = endpoints;
        InitializeSettings(configurationFile);
        InitializeLogging();
        ConnectEvents();
        initialized = true;
        UpdateContext();
        RefreshServices();
    }

    private void ConnectEvents()
    {
        Navigation.SelectionChanged += (_, _) =>
        {
            OperationsPage.IsVisible = Navigation.SelectedIndex == 0;
            SettingsPage.IsVisible = Navigation.SelectedIndex == 1;
            ServicesPage.IsVisible = Navigation.SelectedIndex == 2;
            if (Navigation.SelectedIndex == 2) RefreshServices();
        };
        ConfigureButton.Click += (_, _) => Navigation.SelectedIndex = 1;
        OpenXmlButton.Click += async (_, _) => await RunAsync("Carregar XML", LoadDocumentsAsync);
        ValidateButton.Click += async (_, _) => await RunAsync("Validar XML", ValidateDocumentsAsync);
        SendButton.Click += async (_, _) => await RunAsync("Enviar documento", SendDocumentsAsync);
        StatusButton.Click += async (_, _) => await RunAsync("Status do serviço", async token =>
        {
            var response = await client.ConsultarSituacaoServicoAsync(token);
            ShowResponse(response);
        });
        QueryButton.Click += async (_, _) => await RunAsync("Consultar CT-e", QueryAsync);
        ReceiptButton.Click += async (_, _) => await RunAsync("Consultar recibo", ReceiptAsync);
        RegistrationButton.Click += async (_, _) => await RunAsync("Cadastro SEFAZ", RegistrationAsync);
        DistributionButton.Click += async (_, _) => await RunAsync("Distribuição de documentos", DistributionAsync);
        CancelDocumentButton.Click += async (_, _) => await RunAsync("Cancelar CT-e", CancelDocumentAsync);
        VoidButton.Click += async (_, _) => await RunAsync("Inutilizar numeração", VoidNumbersAsync);
        PdfButton.Click += async (_, _) => await RunAsync("Gerar DACTE PDF", token => ExportPdfAsync(false, token));
        EventPdfButton.Click += async (_, _) => await RunAsync("Gerar PDF de evento", token => ExportPdfAsync(true, token));
        CancelOperationButton.Click += (_, _) => operation?.Cancel();
        CopyButton.Click += async (_, _) => await RunAsync("Copiar retorno", async _ =>
        {
            TextBox[] editors = [ResultText, SentXmlText, ReceivedXmlText, SoapText, LogText];
            if (Clipboard != null) await Clipboard.SetTextAsync(editors[ResponseTabs.SelectedIndex].Text ?? "");
        });
        ClearButton.Click += (_, _) =>
        {
            ResultText.Text = SentXmlText.Text = ReceivedXmlText.Text = SoapText.Text = LogText.Text = "";
            LastOperationText.Text = "";
        };
        Closing += (_, args) =>
        {
            if (operation == null) return;
            args.Cancel = true;
            closeAfterOperation = true;
            operation.Cancel();
        };
        Closed += (_, _) =>
        {
            closed = true;
            LogManager.Shutdown();
        };
        ConnectSettingsEvents();
    }

    private async Task RunAsync(string name, Func<CancellationToken, Task> action)
    {
        if (operation != null) return;
        using var cancellation = new CancellationTokenSource();
        operation = cancellation;
        Workspace.IsEnabled = false;
        Navigation.IsEnabled = false;
        BusyProgress.IsVisible = CancelOperationButton.IsVisible = true;
        StatusText.Text = name + "…";
        AppendLog(name);
        try
        {
            ApplyConfiguration();
            await action(cancellation.Token);
            StatusText.Text = "Pronto para operar";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            StatusText.Text = "Operação cancelada";
            AppendLog("Operação cancelada pelo usuário.");
        }
        catch (Exception exception)
        {
            StatusText.Text = "Não foi possível concluir a operação";
            ResultText.Text = exception.Message;
            ResponseTabs.SelectedIndex = 0;
            AppendLog($"{name}: {exception.Message}");
        }
        finally
        {
            LastOperationText.Text = $"{name} · {DateTime.Now:HH:mm:ss}";
            Workspace.IsEnabled = true;
            Navigation.IsEnabled = true;
            BusyProgress.IsVisible = CancelOperationButton.IsVisible = false;
            operation = null;
            if (closeAfterOperation) Close();
        }
    }

    private void ShowResponse<T>(DFeResposta<T> response) where T : class
    {
        ShowXml(response.XmlEnvio, response.XmlRetorno, response.EnvelopeEnvio, response.EnvelopeRetorno);
        ResultText.Text = PrettyXml(response.XmlRetorno);
        ResponseTabs.SelectedIndex = 0;
    }

    private void ShowXml(string sent, string received, string requestEnvelope = "", string responseEnvelope = "")
    {
        SentXmlText.Text = PrettyXml(sent);
        ReceivedXmlText.Text = PrettyXml(received);
        SoapText.Text = $"ENVIO\n\n{PrettyXml(requestEnvelope)}\n\nRETORNO\n\n{PrettyXml(responseEnvelope)}";
    }

    private static string PrettyXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "";
        try { return XDocument.Parse(xml).ToString(); }
        catch (System.Xml.XmlException) { return xml; }
    }

    private void AppendLog(string message)
    {
        if (closed) return;
        var lines = (LogText.Text + $"{DateTime.Now:HH:mm:ss}  {message}\n").Split('\n');
        LogText.Text = string.Join('\n', lines.TakeLast(500));
    }

    private void InitializeLogging()
    {
        var config = new LoggingConfiguration();
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, new DemoLogTarget(line =>
            Dispatcher.UIThread.Post(() => AppendLog(line))) { Layout = "${message}" });
        config.AddRule(LogLevel.Info, LogLevel.Fatal, new FileTarget("arquivo")
        {
            FileName = Path.Combine(Path.GetDirectoryName(configurationPath)!, "Logs", "OpenCTe.log"),
            Layout = "${longdate}|${level}|${message}",
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 14
        });
        LogManager.Configuration = config;
    }

    private async Task<IStorageFile?> PickFileAsync(string title, string name, params string[] patterns)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            FileTypeFilter = [new FilePickerFileType(name) { Patterns = patterns }]
        });
        return files.FirstOrDefault();
    }
}

public sealed class ServiceEndpoint(TipoServicoCTe type, string name, string address)
{
    public TipoServicoCTe Type { get; } = type;
    public string Name { get; } = name;
    public string Address { get; set; } = address;
}

internal sealed class DemoLogTarget(Action<string> write) : TargetWithLayout
{
    protected override void Write(LogEventInfo logEvent) => write(Layout.Render(logEvent));
}
