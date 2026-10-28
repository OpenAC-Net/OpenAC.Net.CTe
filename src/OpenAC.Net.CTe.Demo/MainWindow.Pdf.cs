using Avalonia.Platform.Storage;

namespace OpenAC.Net.CTe.Demo;

public partial class MainWindow
{
    private async Task ExportPdfAsync(bool evento, CancellationToken token)
    {
        var source = await PickFileAsync(evento ? "Selecionar evento processado" : "Selecionar CT-e ou CT-e OS processado", "Documento XML", "*.xml");
        if (source == null) return;
        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar PDF", SuggestedFileName = Path.GetFileNameWithoutExtension(source.Name) + ".pdf",
            DefaultExtension = "pdf", FileTypeChoices = [new FilePickerFileType("Documento PDF") { Patterns = ["*.pdf"] }]
        });
        if (destination == null) return;
        token.ThrowIfCancellationRequested();
        string xml;
        await using (var input = await source.OpenReadAsync())
        {
            using var reader = new StreamReader(input);
            xml = await reader.ReadToEndAsync(token);
        }
        var bytes = await Task.Run(() => evento
            ? renderer.GerarEventoPDF(CTeProcEvento.Load(xml))
            : renderer.GerarPDF(CTeProc.Load(xml)), token);
        token.ThrowIfCancellationRequested();
        await using (var output = await destination.OpenWriteAsync())
        {
            output.SetLength(0);
            await output.WriteAsync(bytes, token);
        }
        ResultText.Text = $"PDF gerado com sucesso.\n\nArquivo: {destination.TryGetLocalPath() ?? destination.Name}\nTamanho: {bytes.Length:N0} bytes\n\nAbra o PDF para visualizar ou imprimir pelo leitor de sua preferência.";
        ResponseTabs.SelectedIndex = 0;
        if (OpenPdfCheck.IsChecked == true && !await Launcher.LaunchFileAsync(destination))
            AppendLog("O PDF foi salvo. Abra o arquivo pelo seu leitor de PDF.");
    }
}
