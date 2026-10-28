using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Demo.Tests;

[NotInParallel]
public class MainWindowTests
{
    [Test]
    public Task ShellNavigatesBetweenOperationsSettingsAndVersionedServices() => RunOnUiThreadAsync(async () =>
    {
        using var scope = new WindowScope();
        var window = scope.Window;
        window.Show();
        await Assert.That(window.Icon).IsNotNull();
        await Assert.That(Control<Grid>(window, "OperationsPage").IsVisible).IsTrue();
        await Assert.That(Control<Button>(window, "ReceiptButton").IsEnabled).IsFalse();
        await Assert.That(Control<TextBlock>(window, "ContextText").Text).Contains("4.00");
        await CaptureAsync(window, "operacoes");
        var navigation = Control<ListBox>(window, "Navigation");
        navigation.SelectedIndex = 1;
        await Assert.That(Control<ScrollViewer>(window, "SettingsPage").IsVisible).IsTrue();
        await Assert.That(Control<Grid>(window, "OperationsPage").IsVisible).IsFalse();
        await CaptureAsync(window, "configuracoes");
        Control<ComboBox>(window, "VersionCombo").SelectedIndex = 1;
        await Assert.That(Control<Button>(window, "ReceiptButton").IsEnabled).IsTrue();
        await Assert.That(Control<TextBlock>(window, "ContextText").Text).Contains("3.00");
        navigation.SelectedIndex = 2;
        await Assert.That(Control<Grid>(window, "ServicesPage").IsVisible).IsTrue();
        var endpoints = Control<ItemsControl>(window, "ServiceList").ItemsSource!.Cast<ServiceEndpoint>().ToArray();
        await Assert.That(endpoints.Any(endpoint => endpoint.Type == TipoServicoCTe.CTeRecepcao && endpoint.Address.StartsWith("https://"))).IsTrue();
        await CaptureAsync(window, "webservices");
    });

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public Task StatusAwaitsTransportAndRestoresControlsOnCompletionOrCancellation(bool cancel) => RunOnUiThreadAsync(async () =>
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Demo Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        var transport = new PendingTransport();
        var config = new CTeConfig { HttpTransport = transport };
        config.Certificados.CertificadoBytes = certificate.Export(X509ContentType.Pfx, "test");
        using var scope = new WindowScope(new OpenCTe(config));
        var window = scope.Window;
        window.Show();
        Control<TextBox>(window, "CertificatePasswordText").Text = "test";
        Click(window, "StatusButton");
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Assert.That(Control<Grid>(window, "Workspace").IsEnabled).IsFalse();
        await Assert.That(Control<ListBox>(window, "Navigation").IsEnabled).IsFalse();
        await Assert.That(Control<Button>(window, "CancelOperationButton").IsVisible).IsTrue();
        if (cancel) Click(window, "CancelOperationButton");
        else transport.Release.TrySetResult(true);
        await WaitUntilAsync(() => Control<Grid>(window, "Workspace").IsEnabled);
        await Assert.That(Control<ListBox>(window, "Navigation").IsEnabled).IsTrue();
        await Assert.That(Control<Button>(window, "CancelOperationButton").IsVisible).IsFalse();
        if (cancel) await Assert.That(Control<TextBlock>(window, "StatusText").Text).Contains("cancelada");
        else await Assert.That(Control<TextBox>(window, "ReceivedXmlText").Text).Contains("107");
    });

    [Test]
    public Task EmptyDocumentValidationShowsErrorAndReenablesWorkspace() => RunOnUiThreadAsync(async () =>
    {
        using var scope = new WindowScope();
        scope.Window.Show();
        Click(scope.Window, "ValidateButton");
        await WaitUntilAsync(() => Control<Grid>(scope.Window, "Workspace").IsEnabled);
        await Assert.That(Control<TextBox>(scope.Window, "ResultText").Text).Contains("Carregue um XML");
    });

    [Test]
    public Task EditedEndpointIsAppliedOnlyToTheSelectedVersionAndSurvivesCatalogExport() => RunOnUiThreadAsync(async () =>
    {
        using var original = new MemoryStream();
        CTeServiceManager.Save(original);
        try
        {
            using var scope = new WindowScope();
            var window = scope.Window;
            window.Show();
            Control<ListBox>(window, "Navigation").SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            var endpoint = Control<ItemsControl>(window, "ServiceList").ItemsSource!.Cast<ServiceEndpoint>()
                .Single(endpoint => endpoint.Type == TipoServicoCTe.CTeStatusServico);
            var editor = Control<ItemsControl>(window, "ServiceList").GetVisualDescendants().OfType<TextBox>()
                .Single(textBox => textBox.Text == endpoint.Address);
            const string address = "https://example.test/cte/status";
            editor.Text = address;
            await Assert.That(endpoint.Address).IsEqualTo(address);
            Click(window, "ApplyServicesButton");
            await WaitUntilAsync(() => Control<Grid>(window, "Workspace").IsEnabled);
            await Assert.That(Control<TextBox>(window, "ResultText").Text).Contains("Endereços aplicados");
            await Assert.That(CTeServiceManager.GetServiceAndress(CTeVersao.v400, DFeSiglaUF.MS,
                TipoServicoCTe.CTeStatusServico, DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao)).IsEqualTo(address);
            await Assert.That(CTeServiceManager.GetServiceAndress(CTeVersao.v300, DFeSiglaUF.MS,
                TipoServicoCTe.CTeStatusServico, DFeTipoEmissao.Normal, DFeTipoAmbiente.Homologacao)).IsNotEqualTo(address);
            using var exported = new MemoryStream();
            CTeServiceManager.Save(exported);
            await Assert.That(System.Text.Encoding.UTF8.GetString(exported.ToArray())).Contains(address);
        }
        finally { original.Position = 0; CTeServiceManager.Load(original); }
    });

    [Test]
    public Task SettingsAreSavedAndLoadedInTheNextSession() => RunOnUiThreadAsync(async () =>
    {
        var directory = Path.Combine(Path.GetTempPath(), "openac-demo-settings-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "cte.config");
        try
        {
            using (var scope = new WindowScope(path: path, ownsDirectory: false))
            {
                var window = scope.Window;
                window.Show();
                Control<TextBox>(window, "CertificateSerialText").Text = "test-serial";
                Control<TextBox>(window, "CertificatePasswordText").Text = "test-password";
                Control<ComboBox>(window, "VersionCombo").SelectedIndex = 1;
                Click(window, "SaveSettingsButton");
                await WaitUntilAsync(() => Control<Grid>(window, "Workspace").IsEnabled);
                await Assert.That(Control<TextBox>(window, "ResultText").Text).Contains("Configurações salvas");
            }
            using var reloaded = new WindowScope(path: path, ownsDirectory: false);
            await Assert.That(Control<TextBox>(reloaded.Window, "CertificateSerialText").Text).IsEqualTo("test-serial");
            await Assert.That(Control<TextBox>(reloaded.Window, "CertificatePasswordText").Text).IsEqualTo("test-password");
            await Assert.That(Control<TextBlock>(reloaded.Window, "ContextText").Text).Contains("3.00");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    });

    private static async Task RunOnUiThreadAsync(Func<Task> test)
    {
        // Dispatch pode completar na thread da sessão; descarte assíncrono evita aguardar essa própria thread.
        await using var session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(async () =>
        {
            await test();
            return true;
        }, TestContext.Current!.Execution.CancellationToken);
    }

    private static T Control<T>(Window window, string name) where T : Avalonia.Controls.Control
        => window.FindControl<T>(name) ?? throw new InvalidOperationException(name);

    private static void Click(Window window, string name)
        => Control<Button>(window, name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!predicate()) await Task.Delay(10, timeout.Token);
    }

    private static async Task CaptureAsync(Window window, string name)
    {
        var directory = Environment.GetEnvironmentVariable("OPENAC_DEMO_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        Dispatcher.UIThread.RunJobs();
        using var frame = window.CaptureRenderedFrame();
        await Assert.That(frame).IsNotNull();
        frame!.Save(Path.Combine(directory, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    private sealed class WindowScope : IDisposable
    {
        private readonly string path;
        private readonly bool ownsDirectory;
        public MainWindow Window { get; }
        public WindowScope(OpenCTe? component = null, string? path = null, bool ownsDirectory = true)
        {
            this.path = path ?? Path.Combine(Path.GetTempPath(), "openac-demo-ui-" + Guid.NewGuid().ToString("N"), "cte.config");
            this.ownsDirectory = ownsDirectory;
            Window = new MainWindow(component ?? new OpenCTe(), this.path);
        }
        public void Dispose()
        {
            Window.Close();
            if (ownsDirectory && Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!, true);
        }
    }

    private sealed class PendingTransport : ICTeHttpTransport
    {
        internal readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CTeConfig configuration,
            X509Certificate2 certificate, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><cteStatusServicoCTResult><retConsStatServCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"4.00\"><tpAmb>2</tpAmb><cStat>107</cStat><xMotivo>Serviço em operação</xMotivo></retConsStatServCTe></cteStatusServicoCTResult></s:Body></s:Envelope>")
            };
        }
    }
}
