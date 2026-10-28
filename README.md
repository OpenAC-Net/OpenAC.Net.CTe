# OpenAC.Net.CTe

[![NuGet Version](https://img.shields.io/nuget/v/OpenAC.Net.CTe.svg)](https://www.nuget.org/packages/OpenAC.Net.CTe/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenAC.Net.CTe.svg)](https://www.nuget.org/packages/OpenAC.Net.CTe/)
[![Discord](https://img.shields.io/badge/Chat%20on-Discord-purple.svg)](https://discord.com/invite/brdmJ7Yv6w)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Framework-4.6.2%20%7C%204.8-blue.svg)](https://dotnet.microsoft.com/)
[![Target Frameworks](https://img.shields.io/badge/.NET%20Core%20%2F%20Standard-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

Biblioteca .NET multiplataforma para geração, assinatura, validação, transmissão, consulta e impressão (DACTE) de **CT-e (Conhecimento de Transporte Eletrônico - Modelo 57)** e **CT-e OS (Outros Serviços - Modelo 67)**.

Desenvolvida pelo time **OpenAC .Net**, a biblioteca segue as especificações oficiais do Portal Nacional do CT-e (SEFAZ / ENCAT), com API 100% assíncrona (`async`/`await`), suporte a múltiplos modais, geração de DACTE em PDF sem dependências gráficas nativas e pacotes dedicados para ASP.NET Core / Multi-Tenant.

---

## 🚀 Funcionalidades

- **Emissão e Transmissão de CT-e e CT-e OS**:
  - Autorização **Síncrona** no leiaute **4.00** (compressão GZip/Base64 em `CTeRecepcaoSincV4` e `CTeRecepcaoOSV4`).
  - Os leiautes **2.00** e **3.00** permitem apenas leitura, geração e impressão; a transmissão é exclusiva do leiaute **4.00**, inclusive para CT-e OS.
  - Suporte aos modais **Rodoviário**, **Aéreo**, **Aquaviário**, **Ferroviário**, **Dutoviário** e **Multimodal**.
- **Eventos do CT-e**:
  - Cancelamento (`CTeEvCancCTe`).
  - Carta de Correção Eletrônica - CC-e (`CTeEvCCeCTe`).
  - Prestação de Serviço em Desacordo (`CTeEvPrestDesacordo`).
  - EPEC - Evento Prévio de Emissão em Contingência (`CTeEvEPEC`).
  - Registro Multimodal (`CTeEvRegMultimodal`), Comprovante de Entrega, Cancelamento do Comprovante de Entrega e Insucesso na Entrega.
- **Consultas e Distribuição DF-e**:
  - Consulta de Status do Serviço (`ConsultarSituacaoServicoAsync`).
  - Consulta de Situação do CT-e por Chave de Acesso (`ConsultarAsync`).
  - Consulta de Cadastro de Contribuinte (`ConsultaCadastroAsync`).
  - Distribuição DF-e por último NSU, NSU específico ou Chave de Acesso (`DistribuicaoDFePorUltNSUAsync`, `DistribuicaoDFePorNSUAsync`, `DistribuicaoDFePorChaveAsync`).
  - Recepção em lote, consulta de recibo e inutilização de numeração pertencem aos serviços descontinuados e são bloqueadas.
- **Geração de DACTE em PDF (`OpenAC.Net.CTe.DACTe.PDFSharp`)**:
  - Geração de PDF em memória (`byte[]`), `Stream` ou arquivo para CT-e, CT-e OS, Eventos e Inutilização.
  - Baseado em **PDFsharp 6.2.4** com fontes *Liberation Sans* embutidas (funciona em Windows, Linux, macOS e containers Docker sem necessidade de GDI+ ou fontes instaladas).
- **Integração Web & Multi-Tenant (`OpenAC.Net.CTe.Web` e `OpenAC.Net.CTe.DACTe.PDFSharp.Web`)**:
  - Injeção de dependência para ASP.NET Core 8+ e Workers com `IHttpClientFactory`, isolamento de certificados e configurações por empresa (`tenantId`), métricas (`Meter`) e tracing (`ActivitySource`).
- **Assinatura, Validação e Reforma Tributária**:
  - Assinatura digital XML com suporte a certificados A1 e A3 (`DFeSignature`) e validação de `DigestValue` do protocolo.
  - Validação contra os schemas XSD oficiais (**v3.00** e **v4.00**).
  - Suporte aos grupos da **Reforma Tributária (IBS / CBS / Imposto Seletivo)**, Compra Governamental e CRT no leiaute 4.00.

---

## 📦 Pacotes do Ecossistema

| Pacote | Descrição | Target Frameworks |
| :--- | :--- | :--- |
| **`OpenAC.Net.CTe`** | Núcleo de modelos, assinatura, validação XSD, serviços SOAP e `IOpenCTeClient`. | `net462`, `net48`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| **`OpenAC.Net.CTe.DACTe.PDFSharp`** | Impressão de DACTE, Eventos e Inutilização em PDF via PDFsharp. | `net462`, `net48`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0` |
| **`OpenAC.Net.CTe.Web`** | Integração ASP.NET Core, `IHttpClientFactory`, multi-tenant e observabilidade. | `net8.0`, `net9.0`, `net10.0` |
| **`OpenAC.Net.CTe.DACTe.PDFSharp.Web`** | Serviço assíncrono de geração de DACTE em PDF para APIs e Workers. | `net8.0`, `net9.0`, `net10.0` |

---

## 📥 Instalação

Adicione os pacotes via NuGet Package Manager ou CLI:

### .NET CLI:
```bash
dotnet add package OpenAC.Net.CTe
dotnet add package OpenAC.Net.CTe.DACTe.PDFSharp
```

Para aplicações **ASP.NET Core / Workers**:
```bash
dotnet add package OpenAC.Net.CTe.Web
dotnet add package OpenAC.Net.CTe.DACTe.PDFSharp.Web
```

### Package Manager Console:
```powershell
Install-Package OpenAC.Net.CTe
Install-Package OpenAC.Net.CTe.DACTe.PDFSharp
```

### PackageReference:
```xml
<PackageReference Include="OpenAC.Net.CTe" Version="1.0.0" />
<PackageReference Include="OpenAC.Net.CTe.DACTe.PDFSharp" Version="1.0.0" />
```

---

## 🛠️ Como Usar

### 1. Inicializando e Configurando o Componente

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.CTe.DACTe.PDFSharp;
using OpenAC.Net.DFe.Core.Common;

using var cte = new OpenCTe();

// Configurações Gerais
cte.Configuracoes.Geral.Ambiente = DFeTipoAmbiente.Homologacao;
cte.Configuracoes.Geral.VersaoDFe = CTeVersao.v400; // padrão; 2.00 e 3.00 somente para operações locais
cte.Configuracoes.Geral.Mod = ModeloCTe.CTe;        // ou ModeloCTe.CTeOS
cte.Configuracoes.Geral.RetirarAcentos = true;
cte.Configuracoes.Geral.RetirarEspacos = true;

// Configurações de WebServices
cte.Configuracoes.WebServices.Ambiente = DFeTipoAmbiente.Homologacao;
cte.Configuracoes.WebServices.UF = DFeSiglaUF.SP;
cte.Configuracoes.WebServices.TimeoutOperacao = TimeSpan.FromSeconds(60);

// Configurações de Arquivos/Diretórios
cte.Configuracoes.Arquivos.Salvar = true;
cte.Configuracoes.Arquivos.PathSalvar = @"C:\CTe\Salvar";
cte.Configuracoes.Arquivos.PathSchemas = @"C:\CTe\Schemas";

// Configurações do Certificado Digital (exemplo com arquivo PFX)
cte.Configuracoes.Certificados.Certificado = @"C:\Certificados\certificado.pfx";
cte.Configuracoes.Certificados.Senha = "123456";

// (Opcional) Vincular o gerador de DACTE em PDF
cte.DACTe = new OpenDACTe
{
    LayoutImpressao = DACTeLayout.Retrato,
    Configuracoes =
    {
        DiretorioSaida = @"C:\CTe\PDF",
        MensagemRodape = "Emitido via OpenAC.Net.CTe"
    }
};
```

---

### 2. Emitindo e Autorizando um CT-e (Leiaute 4.00 Síncrono)

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.DFe.Core.Common;

var documento = cte.Conhecimentos.AddNewCTe();

documento.InfCTe.Ide.CUF = DFeCodUF.SP;
documento.InfCTe.Ide.CFOP = 5353;
documento.InfCTe.Ide.NatOp = "PRESTACAO DE SERVICO DE TRANSPORTE";
documento.InfCTe.Ide.Mod = ModeloCTe.CTe;
documento.InfCTe.Ide.Serie = 1;
documento.InfCTe.Ide.NCT = 100;
documento.InfCTe.Ide.DhEmi = DateTimeOffset.Now;
documento.InfCTe.Ide.TpImp = DFeTipoImpressao.Retrato;
documento.InfCTe.Ide.TpEmis = DFeTipoEmissao.Normal;
documento.InfCTe.Ide.TpAmb = DFeTipoAmbiente.Homologacao;
documento.InfCTe.Ide.TpCTe = CTeTipo.Normal;
documento.InfCTe.Ide.ProcEmi = DFeProcessoEmissao.AplicativoContribuinte;
documento.InfCTe.Ide.VerProc = "1.0.0";
documento.InfCTe.Ide.Modal = CTeModal.Rodoviario;
documento.InfCTe.Ide.TpServ = CTeTipoServico.Normal;
documento.InfCTe.Ide.CMunEnv = 3550308;
documento.InfCTe.Ide.XMunEnv = "SAO PAULO";
documento.InfCTe.Ide.UFEnv = DFeSiglaUF.SP;
documento.InfCTe.Ide.CMunIni = 3550308;
documento.InfCTe.Ide.XMunIni = "SAO PAULO";
documento.InfCTe.Ide.UFIni = DFeSiglaUF.SP;
documento.InfCTe.Ide.CMunFim = 3509502;
documento.InfCTe.Ide.XMunFim = "CAMPINAS";
documento.InfCTe.Ide.UFFim = DFeSiglaUF.SP;
documento.InfCTe.Ide.Retira = CTeRetira.Sim;
documento.InfCTe.Ide.IndIEToma = CTeIndIEToma.ContribuinteICMS;
documento.InfCTe.Ide.Toma03.Toma = CTeTomador.Remetente;

// Emitente, Remetente, Destinatário, Valores e Impostos
documento.InfCTe.Emit.CNPJ = "12345678000195";
documento.InfCTe.Emit.IE = "123456789110";
documento.InfCTe.Emit.XNome = "TRANSPORTADORA EXEMPLO LTDA";
documento.InfCTe.Emit.CRT = CTeCRT.RegimeNormal;
documento.InfCTe.Emit.EnderEmit = new CTeEnderecoEmit
{
    XLgr = "AVENIDA PAULISTA",
    Nro = "1000",
    XBairro = "BELA VISTA",
    CMun = 3550308,
    XMun = "SAO PAULO",
    CEP = "01310100",
    UF = DFeSiglaUF.SP
};

documento.InfCTe.VPrest.VTPrest = 250.00m;
documento.InfCTe.VPrest.VRec = 250.00m;
documento.InfCTe.Imp.ICMS.ICMS = new CTeICMS00
{
    VBC = 250.00m,
    PICMS = 12.00m,
    VICMS = 30.00m
};

documento.InfCTe.InfoCTe = new CTeNormal
{
    InfCarga = new CTeInfCarga
    {
        VCarga = 10000.00m,
        ProPred = "MERCADORIA EM GERAL"
    },
    InfModal = new CTeInfModal
    {
        Modal = new CTeRodoModal { RNTRC = "12345678" }
    }
};

// Autoriza de forma síncrona na SEFAZ (leiaute 4.00)
var resposta = await cte.AutorizarAsync(documento, cancellationToken);

if (resposta.Autorizado)
{
    Console.WriteLine($"CT-e autorizado! Protocolo: {resposta.Resultado?.ProtCTe?.InfProt?.NProt}");
}
else
{
    Console.WriteLine($"Rejeição ({resposta.Resultado?.CStat}): {resposta.Resultado?.XMotivo}");
}
```

---

### 3. Consultas e Geração do DACTE em PDF

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.CTe.DACTe.PDFSharp;

// Consulta de Status do Serviço
var status = await cte.ConsultarSituacaoServicoAsync(cancellationToken);

// Consulta de CT-e pela chave de acesso
var consulta = await cte.ConsultarAsync("35261012345678000195570010000001001000001009", cancellationToken);

// Distribuição DF-e por último NSU
var dist = await cte.DistribuicaoDFePorUltNSUAsync(DFeCodUF.SP, "12345678000195", "0", cancellationToken);

// Gerando o DACTE em PDF a partir de um CT-e processado (procCTe)
var proc = CTeProc.Load(@"C:\CTe\Salvar\35261012345678000195570010000001001000001009-cte.xml");
var dacte = new OpenDACTe();
byte[] pdfBytes = dacte.GerarPDF(proc);
await File.WriteAllBytesAsync(@"C:\CTe\PDF\dacte.pdf", pdfBytes, cancellationToken);
```

---

### 4. Eventos do CT-e (Cancelamento, CC-e e Desacordo)

```csharp
using OpenAC.Net.CTe.Eventos;

var eventoCancelamento = new CTeEvCancCTe
{
    NProt = "135260000000001",
    XJust = "Erro no preenchimento dos valores do conhecimento de transporte."
};

var retornoEvento = await cte.EnviarEventoAsync(
    nSeqEvento: 1,
    chave: "35261012345678000195570010000001001000001009",
    cnpj: "12345678000195",
    evento: eventoCancelamento,
    cancellationToken: cancellationToken);

// Gerar PDF do evento processado
if (retornoEvento.Resultado?.RetEvento?.FirstOrDefault() != null)
{
    var dacte = new OpenDACTe();
    byte[] eventoPdf = dacte.GerarEventoPDF(procEvento);
}
```

---

### 5. Uso em ASP.NET Core / Multi-Tenant (`OpenAC.Net.CTe.Web`)

```csharp
using OpenAC.Net.CTe;
using OpenAC.Net.CTe.Web;
using OpenAC.Net.CTe.DACTe.PDFSharp.Web;

builder.Services.AddOpenCTeWeb(config =>
{
    config.Geral.VersaoDFe = CTeVersao.v400;
    config.Certificados.CertificadoBytes = certificadoPfx;
    config.Certificados.Senha = senha;
    config.Arquivos.PathSchemas = caminhoSchemas;
});

builder.Services.AddOpenDACTeWeb(config =>
{
    config.MensagemRodape = "Emitido via API";
});

app.MapGet("/cte/status", async (IOpenCTeClientFactory factory, CancellationToken ct) =>
{
    using var client = await factory.CreateAsync(cancellationToken: ct);
    return Results.Ok(await client.ConsultarSituacaoServicoAsync(ct));
});

app.MapPost("/cte/pdf", async (CTeProc documento, IOpenDACTeWeb pdf, CancellationToken ct) =>
    Results.File(await pdf.GerarPDFAsync(documento, cancellationToken: ct), "application/pdf", "dacte.pdf"));
```

---

## ⚙️ Catálogo de Serviços (`CTeServiceManager`)

A biblioteca possui um catálogo embutido de endereços SOAP (`Resources/services.cte`) gerenciado pela classe `CTeServiceManager`, separado por versão (`3.00` e `4.00`), UF, ambiente e tipo de emissão (Normal, SVC-RS, SVC-SP, EPEC):

```csharp
var url = CTeServiceManager.GetServiceAndress(
    CTeVersao.v400,
    DFeSiglaUF.SP,
    TipoServicoCTe.CTeRecepcaoSinc,
    DFeTipoEmissao.Normal,
    DFeTipoAmbiente.Homologacao);
```

---

## 🖥️ Aplicação de Demonstração (Avalonia UI)

O repositório inclui uma aplicação desktop multiplataforma em Avalonia (`OpenAC.Net.CTe.Demo`) para testar emissão, consultas, eventos, edição de endpoints e geração de DACTE em PDF:

```bash
dotnet run --project src/OpenAC.Net.CTe.Demo
```

---

## 🤝 Como Contribuir

Contribuições são muito bem-vindas! Se você deseja relatar um problema, sugerir melhorias ou enviar uma contribuição:

1. Faça um Fork do repositório.
2. Crie uma Branch para a sua feature (`git checkout -b feature/nova-funcionalidade`).
3. Faça o commit das suas alterações (`git commit -m 'Adiciona nova funcionalidade'`).
4. Envie para o GitHub (`git push origin feature/nova-funcionalidade`).
5. Abra um Pull Request.

---

## 💖 Ajude o Projeto OpenAC .Net

Se o **OpenAC.Net.CTe** é útil para o seu negócio ou projeto open source e você deseja apoiar o desenvolvimento contínuo, considere fazer uma contribuição:

- Participe da nossa comunidade no [Discord](https://discord.com/invite/brdmJ7Yv6w).
- Contribua com código, testes e documentação no [GitHub](https://github.com/OpenAC-Net/OpenAC.Net.CTe).

---

## 📄 Licença

Este projeto é distribuído sob a licença **MIT**. Consulte o arquivo [LICENSE](LICENSE) para mais detalhes.
