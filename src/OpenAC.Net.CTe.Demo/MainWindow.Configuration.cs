using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Extensions;

namespace OpenAC.Net.CTe.Demo;

public partial class MainWindow
{
    private string servicesPath = "";
    private bool updatingEmission;

    private void InitializeSettings(string? configurationFile)
    {
        var legacyPath = Path.GetFullPath("cte.config");
        var home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAC.Net", "CTe.Demo");
        configurationPath = configurationFile ?? (File.Exists(legacyPath) ? legacyPath : Path.Combine(home, "cte.config"));
        Directory.CreateDirectory(Path.GetDirectoryName(configurationPath)!);
        ConfigurationPathText.Text = "Configuração local: " + configurationPath;
        configuration = OpenConfig.CreateOrLoad(configurationPath);
        Fill(VersionCombo, [CTeVersao.v400, CTeVersao.v300, CTeVersao.v200], configuration.Get("Versao", CTeVersao.v400), value => value.GetDescription());
        Fill(EnvironmentCombo, [DFeTipoAmbiente.Homologacao, DFeTipoAmbiente.Producao], configuration.Get("Ambiente", DFeTipoAmbiente.Homologacao), value => value == DFeTipoAmbiente.Homologacao ? "Homologação" : "Produção");
        Fill(StateCombo, Enum.GetValues<DFeCodUF>().Where(value => value is not (DFeCodUF.AN or DFeCodUF.EX or DFeCodUF.SU)).OrderBy(value => value.ToString()), configuration.Get("UF", DFeCodUF.MS));
        Fill(EmissionCombo, [DFeTipoEmissao.Normal, DFeTipoEmissao.Contingencia, DFeTipoEmissao.DPEC, DFeTipoEmissao.FSDA, DFeTipoEmissao.SVCRS, DFeTipoEmissao.SVCSP], configuration.Get("Emissao", DFeTipoEmissao.Normal));
        Fill(PdfOrientationCombo, Enum.GetValues<DACTeLayout>(), configuration.Get("LayoutPDF", DACTeLayout.Retrato));
        CertificatePathText.Text = configuration.Get("Certificado", "");
        CertificateSerialText.Text = configuration.Get("NumeroSerie", "");
        try { CertificatePasswordText.Text = configuration.GetCrypt("Senha", ""); }
        catch { CertificatePasswordText.Text = ""; }
        SchemasPathText.Text = configuration.Get("PathSchemas", Path.Combine(AppContext.BaseDirectory, "Schemas"));
        DocumentsPathText.Text = configuration.Get("PathCTe", Path.Combine(home, "Documentos"));
        SoapPathText.Text = configuration.Get("PathSoap", Path.Combine(home, "SOAP"));
        SaveDocumentsCheck.IsChecked = configuration.Get("SalvarXML", false);
        SaveSoapCheck.IsChecked = configuration.Get("SalvarSoap", false);
        OpenPdfCheck.IsChecked = configuration.Get("AbrirPDF", true);
        TimeoutInput.Value = Math.Clamp(configuration.Get("TimeoutSegundos", 100), 1, 600);
        BatchText.Text = configuration.Get("Lote", "1");
        servicesPath = configuration.Get("PathServicos", "");
        if (File.Exists(servicesPath)) catalog.Load(servicesPath);
    }

    private void ConnectSettingsEvents()
    {
        foreach (var combo in new[] { VersionCombo, EnvironmentCombo, StateCombo, EmissionCombo })
            combo.SelectionChanged += (_, _) => { if (initialized && !updatingEmission) { UpdateContext(); RefreshServices(); } };
        BrowseCertificateButton.Click += async (_, _) => await RunAsync("Selecionar certificado", async _ =>
        {
            var file = await PickFileAsync("Selecionar certificado digital", "Certificado PFX/P12", "*.pfx", "*.p12");
            if (file != null) CertificatePathText.Text = file.TryGetLocalPath() ?? throw new InvalidOperationException("Selecione um arquivo local.");
        });
        SelectCertificateButton.Click += async (_, _) => await RunAsync("Selecionar certificado", async _ => await SelectCertificateAsync());
        BrowseSchemasButton.Click += async (_, _) => await SelectFolderAsync(SchemasPathText);
        BrowseDocumentsButton.Click += async (_, _) => await SelectFolderAsync(DocumentsPathText);
        BrowseSoapButton.Click += async (_, _) => await SelectFolderAsync(SoapPathText);
        SaveSettingsButton.Click += async (_, _) => await RunAsync("Salvar configurações", _ =>
        {
            SaveConfiguration();
            ResultText.Text = "Configurações salvas em " + configurationPath;
            return Task.CompletedTask;
        });
        RefreshServicesButton.Click += (_, _) => RefreshServices();
        LoadServicesButton.Click += async (_, _) => await RunAsync("Carregar catálogo", async token =>
        {
            var file = await PickFileAsync("Carregar catálogo de serviços", "Catálogo XML de serviços", "*.xml");
            if (file == null) return;
            await using var stream = await file.OpenReadAsync();
            token.ThrowIfCancellationRequested();
            catalog.Load(stream);
            servicesPath = file.TryGetLocalPath() ?? "";
            RefreshServices();
        });
        ImportServicesButton.IsEnabled = false;
        ToolTip.SetTip(ImportServicesButton, "O importador antigo atendia ao CT-e 3.00, já desativado. Carregue um catálogo CT-e 4.00.");
        ApplyServicesButton.Click += async (_, _) => await RunAsync("Aplicar endereços", _ =>
        {
            ApplyEndpoints();
            ResultText.Text = "Endereços aplicados à configuração selecionada. Exporte o catálogo para reutilizá-los.";
            return Task.CompletedTask;
        });
        SaveServicesButton.Click += async (_, _) => await RunAsync("Exportar catálogo", async token =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar catálogo", SuggestedFileName = "services.xml", DefaultExtension = "xml",
                FileTypeChoices = [new FilePickerFileType("Catálogo XML de serviços") { Patterns = ["*.xml"] }]
            });
            if (file == null) return;
            ApplyEndpoints();
            token.ThrowIfCancellationRequested();
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            catalog.Save(stream);
            servicesPath = file.TryGetLocalPath() ?? "";
        });
    }

    private void ApplyConfiguration()
    {
        var config = settings;
        config.Geral.VersaoDFe = Selected<CTeVersao>(VersionCombo);
        config.WebServices.Enderecos.Clear();
        foreach (var endpoint in endpoints.Where(item => !string.IsNullOrWhiteSpace(item.Address)))
            config.WebServices.Enderecos[endpoint.Type] = endpoint.Address;
        config.Geral.FormaEmissao = Selected<DFeTipoEmissao>(EmissionCombo);
        config.Geral.ExibirErroSchema = true;
        config.WebServices.Ambiente = Selected<DFeTipoAmbiente>(EnvironmentCombo);
        config.WebServices.UF = Selected<DFeCodUF>(StateCombo);
        config.WebServices.TimeoutOperacao = TimeSpan.FromSeconds((double)(TimeoutInput.Value ?? 100));
        config.Certificados.Certificado = !string.IsNullOrWhiteSpace(CertificatePathText.Text) ? CertificatePathText.Text.Trim() : CertificateSerialText.Text?.Trim() ?? "";
        config.Certificados.Senha = CertificatePasswordText.Text ?? "";
        config.Arquivos.PathSchemas = SchemasPathText.Text ?? "";
        config.Arquivos.PathCTe = DocumentsPathText.Text ?? "";
        config.Arquivos.PathEvento = Path.Combine(config.Arquivos.PathCTe, "Eventos");
        config.Arquivos.PathInu = Path.Combine(config.Arquivos.PathCTe, "Inutilizacoes");
        config.Arquivos.PathSalvar = SoapPathText.Text ?? "";
        config.Arquivos.Salvar = SaveDocumentsCheck.IsChecked == true;
        config.WebServices.Salvar = SaveSoapCheck.IsChecked == true;
        renderer.LayoutImpressao = Selected<DACTeLayout>(PdfOrientationCombo);
        client = new OpenCTe(settings);
    }

    private void SaveConfiguration()
    {
        configuration!.Set("Versao", Selected<CTeVersao>(VersionCombo));
        configuration.Set("Ambiente", Selected<DFeTipoAmbiente>(EnvironmentCombo));
        configuration.Set("UF", Selected<DFeCodUF>(StateCombo));
        configuration.Set("Emissao", Selected<DFeTipoEmissao>(EmissionCombo));
        configuration.Set("Certificado", CertificatePathText.Text ?? "");
        configuration.Set("NumeroSerie", CertificateSerialText.Text ?? "");
        configuration.SetCrypt("Senha", CertificatePasswordText.Text ?? "");
        configuration.Set("PathSchemas", SchemasPathText.Text ?? "");
        configuration.Set("PathCTe", DocumentsPathText.Text ?? "");
        configuration.Set("PathSoap", SoapPathText.Text ?? "");
        configuration.Set("SalvarXML", SaveDocumentsCheck.IsChecked == true);
        configuration.Set("SalvarSoap", SaveSoapCheck.IsChecked == true);
        configuration.Set("AbrirPDF", OpenPdfCheck.IsChecked == true);
        configuration.Set("LayoutPDF", Selected<DACTeLayout>(PdfOrientationCombo));
        configuration.Set("TimeoutSegundos", TimeoutInput.Value ?? 100);
        configuration.Set("Lote", BatchText.Text ?? "1");
        configuration.Set("PathServicos", servicesPath);
        configuration.Save();
    }

    private void UpdateContext()
    {
        if (updatingEmission) return;
        var version = Selected<CTeVersao>(VersionCombo);
        var uf = Selected<DFeCodUF>(StateCombo).ToSiglaUF();
        var environment = Selected<DFeTipoAmbiente>(EnvironmentCombo);
        var selectedEmission = Selected<DFeTipoEmissao>(EmissionCombo);
        var allowed = new[] { DFeTipoEmissao.Normal, DFeTipoEmissao.Contingencia, DFeTipoEmissao.DPEC, DFeTipoEmissao.FSDA, DFeTipoEmissao.SVCRS, DFeTipoEmissao.SVCSP }
            .Where(emission =>
            {
                if (version != CTeVersao.v400) return emission == DFeTipoEmissao.Normal;
                try { CTeServiceManager.ValidarConsulta(version, uf, TipoServicoCTe.CTeStatusServico, emission, environment); return true; }
                catch (NotSupportedException) { return false; }
            }).ToArray();
        updatingEmission = true;
        try
        {
            Fill(EmissionCombo, allowed, selectedEmission, emission => emission switch
            {
                DFeTipoEmissao.Contingencia => "Offline (tpEmis=2)",
                DFeTipoEmissao.DPEC => "EPEC / conciliação (tpEmis=4)",
                _ => emission.ToString()
            });
        }
        finally { updatingEmission = false; }
        EnvironmentText.Text = environment == DFeTipoAmbiente.Homologacao ? "Homologação" : "Produção";
        ContextText.Text = $"CT-e {version.GetDescription()} · {Selected<DFeCodUF>(StateCombo)} · {Selected<DFeTipoEmissao>(EmissionCombo)}";
        EnvironmentBadge.Background = Avalonia.Media.Brush.Parse(environment == DFeTipoAmbiente.Producao ? "#FFE8DB" : "#EAF2FF");
        var communicationEnabled = version == CTeVersao.v400;
        SendButton.IsEnabled = StatusButton.IsEnabled = QueryButton.IsEnabled =
            RegistrationButton.IsEnabled = DistributionButton.IsEnabled = CancelDocumentButton.IsEnabled = communicationEnabled;
        ToolTip.SetTip(SendButton, communicationEnabled ? null : "CT-e 2.00 e 3.00 permitem apenas leitura, geração e impressão.");
        ReceiptButton.IsEnabled = VoidButton.IsEnabled = false;
        ApplyServicesButton.IsEnabled = LoadServicesButton.IsEnabled = SaveServicesButton.IsEnabled = communicationEnabled;
        ToolTip.SetTip(ReceiptButton, "A recepção em lote foi descontinuada. CT-e 4.00 usa autorização síncrona.");
        ToolTip.SetTip(VoidButton, "O serviço de inutilização foi descontinuado e não existe no CT-e 4.00.");
    }

    private async Task SelectFolderAsync(TextBox target)
    {
        await RunAsync("Selecionar diretório", async _ =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Selecionar diretório" });
            if (folders.FirstOrDefault() is { } folder) target.Text = folder.TryGetLocalPath() ?? target.Text;
        });
    }

    private async Task SelectCertificateAsync()
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        var certificates = store.Certificates;
        try
        {
            var choices = certificates.Cast<X509Certificate2>().Where(certificate => certificate.HasPrivateKey)
                .Select(certificate => new Choice<string>(certificate.SerialNumber, $"{certificate.GetNameInfo(X509NameType.SimpleName, false)} · {certificate.NotAfter:dd/MM/yyyy}")).ToArray();
            if (choices.Length == 0) throw new InvalidOperationException("Nenhum certificado com chave privada encontrado. Selecione um arquivo PFX/P12.");
            var dialog = new Window { Title = "Certificados do usuário", Width = 620, Height = 390, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var list = new ListBox { ItemsSource = choices, SelectedIndex = 0 };
            var select = new Button { Content = "Usar certificado", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 12, 0, 0) };
            var layout = new DockPanel { Margin = new Avalonia.Thickness(20) };
            DockPanel.SetDock(select, Dock.Bottom);
            layout.Children.Add(select);
            layout.Children.Add(list);
            dialog.Content = layout;
            select.Click += (_, _) => dialog.Close((list.SelectedItem as Choice<string>)?.Value);
            if (await dialog.ShowDialog<string?>(this) is { } serial)
            {
                CertificateSerialText.Text = serial;
                CertificatePathText.Text = "";
            }
        }
        finally { foreach (var certificate in certificates) certificate.Dispose(); }
    }

    private static void Fill<T>(ComboBox combo, IEnumerable<T> values, T selected, Func<T, string>? label = null)
    {
        var choices = values.Select(value => new Choice<T>(value, label?.Invoke(value) ?? value?.ToString() ?? "")).ToArray();
        combo.ItemsSource = choices;
        combo.SelectedItem = choices.FirstOrDefault(choice => EqualityComparer<T>.Default.Equals(choice.Value, selected)) ?? choices.First();
    }

    private static T Selected<T>(ComboBox combo) => ((Choice<T>)combo.SelectedItem!).Value;
    private sealed record Choice<T>(T Value, string Label) { public override string ToString() => Label; }

    private void RefreshServices()
    {
        if (!initialized) return;
        ApplyConfiguration();
        var config = settings;
        endpoints.Clear();
        foreach (var type in Enum.GetValues<TipoServicoCTe>())
        {
            string address;
            try { address = catalog.ObterEndereco(config.Geral.VersaoDFe, config.WebServices.UF.ToSiglaUF(), type, config.Geral.FormaEmissao, config.WebServices.Ambiente); }
            catch (NotSupportedException) { continue; }
            endpoints.Add(new ServiceEndpoint(type, type.GetDescription(), address));
        }
    }

    internal void ExportarCatalogo(Stream destino) => catalog.Save(destino);

    private void ApplyEndpoints()
    {
        foreach (var endpoint in endpoints.Where(endpoint => !string.IsNullOrWhiteSpace(endpoint.Address)))
        {
            var valid = Uri.TryCreate(endpoint.Address.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || endpoint.Type == TipoServicoCTe.QRCode && uri.Scheme == Uri.UriSchemeHttp);
            if (!valid) throw new ArgumentException($"O endereço de {endpoint.Name} deve ser uma URL válida para esse serviço.");
        }
        foreach (var endpoint in endpoints)
            catalog.AplicarEndereco(settings.Geral.VersaoDFe, settings.WebServices.UF.ToSiglaUF(), endpoint.Type,
                settings.Geral.FormaEmissao, settings.WebServices.Ambiente, endpoint.Address);
    }
}
