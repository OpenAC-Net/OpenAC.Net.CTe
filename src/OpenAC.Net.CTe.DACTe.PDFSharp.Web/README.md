# OpenAC.Net.CTe.DACTe.PDFSharp.Web

Serviço de PDF para ASP.NET Core, independente de certificado e de conexão com a SEFAZ.


Para configurar por empresa, registre um `ICTeConfigurationProvider<DACTeConfig>`
(namespace `OpenAC.Net.CTe.Web`) antes de `AddOpenDACTeWeb` e informe o ID da empresa na chamada.
Cada operação usa seu próprio renderer, e a geração não grava arquivos no servidor.
O cancelamento é verificado antes e depois da renderização; o desenho do PDF é síncrono.

As limitações de versão fiscal e de conformidade do layout são as mesmas do projeto PDF.

O exemplo de integração está no [README principal](../../README.md).
