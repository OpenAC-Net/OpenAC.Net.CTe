using Avalonia;
using Avalonia.Headless;
using OpenAC.Net.CTe.Demo;

namespace OpenAC.Net.CTe.Demo.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
