# OpenAC.Net.CTe.Web

Integração para ASP.NET Core 8+, APIs e workers com `IHttpClientFactory`, isolamento de
configuração por empresa, certificado no handler TLS, cancelamento, logs e observabilidade.
Suporta CT-e e CT-e OS 4.00, preservando os modelos e serviços antigos.

## Uma empresa

```csharp
using OpenAC.Net.CTe.Web;

builder.Services.AddOpenCTeWeb(config =>
{
    config.Geral.VersaoDFe = CTeVersao.v400;
    config.Certificados.CertificadoBytes = certificadoPfx;
    config.Certificados.Senha = senha;
    config.Arquivos.PathSchemas = caminhoSchemas;
}, options =>
{
    options.TimeoutOperacao = TimeSpan.FromSeconds(60);
    options.PersistenciaHabilitada = true;
    options.DiretorioDocumentos = "/dados/cte";
});

app.MapGet("/cte/status", async (IOpenCTeClientFactory factory, CancellationToken ct) =>
{
    using var client = await factory.CreateAsync(cancellationToken: ct);
    return Results.Ok(await client.ConsultarSituacaoServicoAsync(ct));
});
```

## Múltiplas empresas

Implemente `ICTeConfigurationProvider<CTeConfig>` e `ICTeCertificateProvider`.
O primeiro retorna uma configuração **nova por chamada**; o segundo resolve o certificado
atual da empresa e preenche `CTeConfigCertificados`. Ambos recebem `tenantId` e cancelamento.

```csharp
builder.Services.AddOpenCTeWebMultiTenant<ConfiguracaoPorEmpresa, CertificadoPorEmpresa>();

// tenantId deve ser resolvido pela aplicação a partir do usuário autenticado.
using var client = await factory.CreateAsync(tenantId, cancellationToken);
var retorno = await client.ConsultarAsync(chave, cancellationToken);
```

Não compartilhe configurações mutáveis ou instâncias de `OpenCTe` entre requisições.
O provider é chamado em cada criação; o pool HTTP usa empresa, thumbprint, protocolo TLS
e autoridade do endpoint como identidade. A renovação do certificado cria outro pool.
Certificados sem chave privada, ainda inválidos ou próximos do vencimento são rejeitados.
Os certificados do pool pertencem à infraestrutura, com descarte no encerramento do host.

Os registros de pools permanecem até o encerramento do host; use IDs de empresas cadastradas,
resolvidos e autorizados pela aplicação, sem criar IDs ilimitados a partir de entrada pública.
Redirecionamentos e cookies são desativados. O TLS global não é alterado.

A persistência fica desabilitada por padrão. Quando habilitada, os documentos ficam em
subdiretórios separados pelo hash do ID da empresa. É possível substituir `ICTeDocumentStore`
por um serviço de banco ou storage antes do registro da integração.
A implementação de filesystem escreve em arquivo temporário e faz substituição atômica.

`TimeoutOperacao` limita cada chamada SOAP, incluindo o envio e a leitura da resposta.
O token fornecido pelo chamador permite limitar o fluxo completo, inclusive espera por recibo.
SOAP POST não recebe retries automáticos: uma falha de comunicação não confirma que a
SEFAZ deixou de processar a requisição. Rejeições fiscais continuam nos objetos de retorno.
Falhas de transporte geram `CTeTechnicalException`; cancelamentos do chamador são preservados.

ActivitySource e Meter: `OpenAC.Net.CTe.Web`.
Métricas: `openac.cte.requests` e `openac.cte.request.duration` (ms).
Payloads fiscais e senhas não são incluídos nos logs da integração.

O catálogo de endpoints é `services.cte`, selecionado pela versão da configuração.
O circuit breaker é opcional (`CircuitBreakerHabilitado`), isolado pelo pool da empresa,
certificado, protocolo e endpoint. `TimeoutTentativa` limita a tentativa HTTP.
