# Modernização do OpenAC.Net.CTe

Os projetos desta pasta oferecem uma API assíncrona, transporte HTTP substituível,
persistência substituível, integração ASP.NET Core
e geração de DACTE em PDF.

| Projeto | Uso |
| --- | --- |
| `OpenAC.Net.CTe` | Modelos, assinatura, validação, serviços SOAP e `IOpenCTeClient` |
| `OpenAC.Net.CTe.Web` | DI, configuração por empresa, certificados, pool HTTP, logs e métricas |
| `OpenAC.Net.CTe.DACTe.PDFSharp` | PDF em memória, stream ou arquivo; eventos e inutilização |
| `OpenAC.Net.CTe.DACTe.PDFSharp.Web` | Serviço de PDF por empresa para APIs e workers |
| `OpenAC.Net.CTe.Tests` | Testes locais sem acesso à SEFAZ ou certificado real |
| `OpenAC.Net.CTe.Demo` | Aplicação Avalonia com tema azul, operações assíncronas e exportação de PDF |
| `OpenAC.Net.CTe.Demo.Tests` | Testes da interface Avalonia sem servidor gráfico |

A biblioteca e o PDF suportam net462, net48, netstandard2.0, net8.0, net9.0 e net10.0.
As integrações web suportam .NET 8, 9 e 10. O demo Avalonia usa .NET 8.

## Organização das pastas

A estrutura segue o padrão do NFSe.Nacional, preservando os namespaces e a API pública.

| Projeto | Pastas |
| --- | --- |
| `OpenAC.Net.CTe` | `Common/Model` para modelos e retornos, `Common/Types` para enums, `Common` para configurações e utilitários, `Webservice` para comunicação e `Storage` para persistência |
| `OpenAC.Net.CTe.Web` | `Client`, `Configuration`, `Exception`, `Extensions`, `Provider` e `Transport` |
| `OpenAC.Net.CTe.DACTe.PDFSharp` | `Common`, `Configuracao`, `Layout` e `Resources` |
| `OpenAC.Net.CTe.DACTe.PDFSharp.Web` | `Configuration`, `Extensions` e `Service` |

## Versões e modelos

Há um único modelo `CTe` e um único `CTeOS`, compartilhados entre as versões suportadas.
Configure `Configuracoes.Geral.VersaoDFe = CTeVersao.v400` para emissão 4.00.
Os atributos e métodos `ShouldSerialize` dos modelos controlam os campos de cada versão;
a serialização, leitura e assinatura continuam utilizando o DFe.Core.

CT-e 4.00 e CT-e OS utilizam autorização síncrona, um documento por requisição.
O envio 4.00 comprime o XML assinado em GZip/Base64. O protocolo é associado ao documento
somente quando chave e digest correspondem. Rejeições fiscais não entram na lista de autorizados.
O catálogo único `Resources/services.cte` contém os endereços por versão, UF, ambiente e emissão.

Os modelos incluem CRT, tomador e percurso do CT-e OS, tributos federais, IBS/CBS, compra
governamental, pagamentos e os grupos adicionais dos XSDs presentes no projeto.
Os testes validam XML, assinatura, schemas, transporte simulado e PDF; não transmitem à SEFAZ.

## Desktop

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.CTe.DACTe.PDFSharp;

using var cte = new OpenCTe();
cte.Configuracoes.Geral.VersaoDFe = CTeVersao.v400;
cte.Configuracoes.Certificados.Certificado = @"C:\certificados\empresa.pfx";
cte.Configuracoes.Certificados.Senha = senha;
cte.Configuracoes.Arquivos.PathSchemas = @"C:\CTe\Schemas";

var status = await cte.ConsultarSituacaoServicoAsync(cancellationToken);

var impressao = new OpenDACTe();
impressao.Configuracoes.DiretorioSaida = @"C:\CTe\PDF";
cte.DACTe = impressao;

var processado = CTeProc.Load(@"C:\CTe\autorizado-cte.xml");
byte[] pdf = impressao.GerarPDF(processado);
impressao.ImprimirPDF(new[] { processado });
```

Use uma instância de `OpenCTe` por fluxo de operações. A coleção de documentos e o status
são mutáveis; não compartilhe a mesma instância entre operações concorrentes.
As operações de comunicação são expostas apenas como métodos `Async`, com retorno `Task`
e suporte a `CancellationToken`. Chamadas antigas como `Consultar(chave)` devem ser migradas
para `await ConsultarAsync(chave, cancellationToken)`. A sobrecarga `EnviarAsync` aceita lote
como `int` ou `string`, inclusive pelo contrato `IOpenCTeClient`.

O timeout SOAP é configurável em `Configuracoes.WebServices.TimeoutOperacao`.
Os métodos `Async` propagam `CancellationToken` até o envio e a persistência.
A espera por recibo usa `Task.Delay` e só repete a consulta enquanto o lote está em processamento
(cStat 105). Transmissões SOAP não são repetidas automaticamente em falhas de rede.

Para injetar infraestrutura própria, atribua `CTeConfig.HttpTransport` e
`CTeConfig.DocumentStore`. Com um store externo, desative `Arquivos.CriarDiretorios`.
A implementação desktop reutiliza clientes por certificado e TLS por até dez minutos.
O projeto web usa `IHttpClientFactory` com isolamento por empresa. Nenhum altera o TLS global.

## Autorização 4.00

```csharp
// O mesmo modelo usado nas versões anteriores.
var documento = new CTe();
// Preencha InfCTe.Ide, Emit, Rem, Dest, VPrest, Imp e InfoCTe.
var resposta = await cte.AutorizarAsync(documento, cancellationToken);
if (resposta.Autorizado)
{
    byte[] pdf = impressao.GerarPDF(resposta.Conhecimento);
}

var outrosServicos = new CTeOS();
outrosServicos.InfCTe.Toma = new CTeOSToma();
outrosServicos.InfCTe.InfoCTe = new CTeNormalOS();
// Preencha o tomador, os dados de serviço e o modal.
var respostaOS = await cte.AutorizarAsync(outrosServicos, cancellationToken);
```

`EnviarAsync` também encaminha os documentos carregados para autorização síncrona na versão 4.00.
Cada documento gera uma chamada separada; os resultados ficam em `AutorizacoesSincronas`.
As versões antigas de CT-e continuam usando o fluxo de lote e recibo.
CT-e 4.00 rejeita chamadas a consulta de recibo e inutilização, serviços ausentes nesse protocolo.

Para gerar exemplos PDF com dados fictícios:

```bash
dotnet run --project OpenAC.Net.CTe.Pdf.Exemplo -- --exemplos Exemplos_PDF
```

O mesmo programa converte XML processado em PDF:

```bash
dotnet run --project OpenAC.Net.CTe.Pdf.Exemplo -- autorizado-cte.xml dacte.pdf
```

## Validação

Os dois projetos de testes usam TUnit 1.65.68 e Microsoft.Testing.Platform, seguindo o
padrão do NFSe.Nacional. Os casos parametrizados usam `[Arguments]`, e as verificações
usam as asserções do TUnit.

Use o SDK .NET 10. O `global.json` seleciona o runner nativo do
[Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test).

```bash
dotnet build OpenAC.Net.CTe.sln -m:1
dotnet test --solution OpenAC.Net.CTe.sln --no-build
```

Para executar um projeto isoladamente, use `dotnet test --project caminho/do/projeto.csproj`.

A validação do PDF inclui abertura pelo leitor PDFsharp, senha, orientação, múltiplas páginas,
eventos e integração por DI. Os testes de infraestrutura usam respostas SOAP simuladas.

Para abrir o demo desktop:

```bash
dotnet run --project OpenAC.Net.CTe.Demo
```

Veja o [guia do demo Avalonia](OpenAC.Net.CTe.Demo/README.md) para configurar certificado,
ambiente, arquivos e impressão.
