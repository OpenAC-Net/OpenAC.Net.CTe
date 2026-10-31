# OpenAC.Net.CTe

Biblioteca .NET para geração, assinatura, validação, autorização, consulta e eventos de CT-e e CT-e OS.
A comunicação usa os services SOAP e os contratos de transporte e armazenamento do DFe.Core.

A API de comunicação atende ao leiaute 4.00. Os modelos 2.00 e 3.00 permanecem disponíveis para
leitura, geração de XML e PDF. Consulta de recibo, recepção em lote antiga e inutilização não fazem
parte do cliente atual.

## API assíncrona

`OpenCTe` implementa `IOpenCTeClient`. Cada operação recebe seus dados explicitamente e aceita
`CancellationToken`. O cliente captura uma cópia da configuração ao ser criado, prepara uma cópia
do documento para autorização e descarta o certificado de cada chamada ao terminar.
Não há coleção interna de documentos, status compartilhado, eventos de interface gráfica ou impressão
automática. Chamadas independentes podem ocorrer em paralelo. Transportes, stores e provedores de
certificado personalizados devem suportar o uso concorrente.

As operações disponíveis são `AutorizarAsync` (CT-e ou CT-e OS), `ConsultarAsync`,
`ConsultarSituacaoServicoAsync`, `EnviarEventoAsync`, `ConsultaCadastroAsync` e
`ConsultarDistribuicaoAsync`. Eventos recebem `CTeEventoSolicitacao`; distribuição recebe
`CTeDistribuicaoSolicitacao`, com exatamente um filtro: último NSU ou NSU específico. A consulta de situação por chave usa `ConsultarAsync`.

## Exemplo em ASP.NET Core

O documento deste exemplo chega no corpo da requisição como JSON com os dados fiscais já preenchidos.
Configure `CTe:Certificado` e `CTe:Senha` na configuração da aplicação e disponibilize os schemas
no diretório `Schemas` da aplicação.

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.CTe.Web;
using OpenAC.Net.DFe.Core.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenCTeWeb(config =>
{
    config.WebServices.UF = DFeCodUF.MS;
    config.WebServices.Ambiente = DFeTipoAmbiente.Homologacao;
    config.Certificados.Certificado = builder.Configuration["CTe:Certificado"]
        ?? throw new InvalidOperationException("Configure CTe:Certificado.");
    config.Certificados.Senha = builder.Configuration["CTe:Senha"] ?? "";
    config.Arquivos.PathSchemas = Path.Combine(AppContext.BaseDirectory, "Schemas");
});

var app = builder.Build();

app.MapPost("/ctes", async (
    CTe documento,
    IOpenCTeClientFactory fabrica,
    CancellationToken cancellationToken) =>
{
    var cliente = await fabrica.CriarAsync(cancellationToken: cancellationToken);
    var resultado = await cliente.AutorizarAsync(documento, cancellationToken);

    return Results.Ok(new
    {
        resultado.Autorizado,
        resultado.Codigo,
        resultado.Motivo,
        resultado.Protocolo
    });
});

app.Run();
```

Para várias empresas, registre `AddOpenCTeWebMultiTenant<TConfigurationProvider, TCertificateProvider>`
e informe a empresa em `CriarAsync`. Os providers resolvem a configuração e o certificado; a fábrica
retorna `IOpenCTeClient` e captura sua própria configuração. Aplicações fora do ASP.NET Core podem
criar `OpenCTe` diretamente com `CTeConfig` e, opcionalmente, `ICTeCertificadoProvider`.

## Demo em Avalonia

A aplicação `src/OpenAC.Net.CTe.Demo` reúne edição da configuração, carregamento e validação de XML,
autorização, consultas, eventos, edição e importação de endpoints e geração de PDF na mesma interface.
Ela mantém seus documentos e estado de tela localmente e usa a API assíncrona da biblioteca.
Execute `dotnet run --project src/OpenAC.Net.CTe.Demo` a partir da raiz do repositório.

## Pacotes

| Pacote | Responsabilidade | Frameworks |
| --- | --- | --- |
| OpenAC.Net.CTe | Modelos fiscais e cliente assíncrono | net462, net48, netstandard2.0, net8.0, net9.0, net10.0 |
| OpenAC.Net.CTe.Web | IHttpClientFactory, configuração por empresa e observabilidade | net8.0, net9.0, net10.0 |
| OpenAC.Net.CTe.DACTe.PDFSharp | Geração independente de PDF | net462, net48, netstandard2.0, net8.0, net9.0, net10.0 |
| OpenAC.Net.CTe.DACTe.PDFSharp.Web | Serviço de PDF para aplicações web | net8.0, net9.0, net10.0 |

`CTeAutorizacaoResposta.DocumentoProcessado` fornece o documento autorizado para gerar o DACTE.
O renderer recebe o documento diretamente e não depende de `OpenCTe`.

## Comunicação e persistência

O envio usa async de ponta a ponta para HTTP e armazenamento. O provider padrão lê PFX de arquivo
assincronamente e também aceita bytes em memória ou certificado instalado. Assinatura, serialização,
validação XSD e renderização de PDF são processamento síncrono.

A persistência é opcional e fica desabilitada por padrão. `IDFeDocumentStore` pode ser substituído;
o store local mantém a substituição atômica dos XMLs. Configurações de caminho e agrupamento continuam
em `CTeConfig.Arquivos`. Os catálogos personalizados de endpoints são isolados por configuração.

POST fiscal não recebe retentativas automáticas. Rejeições fiscais fazem parte do resultado;
falhas técnicas e cancelamentos são propagados ao chamador. Após uma falha com resultado desconhecido,
a aplicação pode consultar a chave antes de decidir o próximo passo.

## Serialização dos modelos

Os modelos utilizam o DFe.Core 1.8.0-beta.6. A normalização de IE/RNTRC, os indicadores
booleanos (`0`/`1`) e os nomes de homologação são aplicados nas propriedades XML, preservando
os valores públicos informados pela aplicação. A associação do tomador CT-e OS propaga seu `Parent`.

`CTeIde.DhEmi` e `CTeIde.DhCont` usam `DateTimeOffset`: no leiaute 2.00, o XML contém data/hora
sem fuso; a partir do 3.00, preserva o offset informado. O campo de contingência não é emitido no 4.00. A formatação
usa os tipos fiscais do Core e independe da cultura do processo.

O seguro multimodal é opcional. Quando informado, seus campos são escritos conforme os XSDs
locais 3.00/4.00, incluindo `nAver`. `CoreModelContractTests` verifica leitura/escrita, culturas,
fuso, propagação de contexto, assinatura e validação dos casos afetados nos schemas locais.

## Licença

MIT. Consulte [LICENSE](LICENSE).

A revisão do catálogo, a cobertura por UF e a vigência das modalidades de emissão estão em
[Auditoria dos endereços do CT-e](docs/AuditoriaEnderecosCTe.md).
