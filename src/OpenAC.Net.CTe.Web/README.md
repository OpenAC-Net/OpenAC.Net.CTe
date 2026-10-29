# OpenAC.Net.CTe.Web

Integração ASP.NET Core com IHttpClientFactory, isolamento por empresa, cancelamento e observabilidade.
O exemplo de uso está no [README principal](../../README.md).

`AddOpenCTeWeb` registra uma configuração fixa. `AddOpenCTeWebMultiTenant` recebe implementações de
`ICTeConfigurationProvider<CTeConfig>` e `ICTeCertificateProvider` para resolver os dados da empresa.
`IOpenCTeClientFactory.CriarAsync` devolve `IOpenCTeClient`. A fábrica captura uma cópia da configuração
antes de aplicar certificado, transporte, timeout e diretório da empresa.

`ICTeCertificadoProvider` fornece certificados por operação e pode ser substituído na injeção de
dependências. Cada certificado retornado pertence à chamada e é descartado ao terminar. O provider
padrão lê PFX de arquivo assincronamente. As configurações e documentos recebidos pelo cliente
permanecem isolados das alterações feitas durante uma operação.

O pool HTTP usa empresa, certificado, protocolo TLS e autoridade do endpoint como identidade.
Redirecionamentos e cookies são desabilitados. Os certificados mantidos no pool pertencem ao host.
Os IDs de empresa devem ser resolvidos e autorizados pela aplicação.

A persistência fica desabilitada por padrão. Quando habilitada, os arquivos ficam separados pelo hash
do ID da empresa. Registre um `IDFeDocumentStore` antes da integração para usar outro armazenamento.
O store local grava em arquivo temporário e faz substituição atômica.

`TimeoutOperacao` limita cada chamada SOAP, incluindo persistência dos envelopes e leitura da resposta.
O CancellationToken da aplicação também alcança a obtenção do certificado e as operações de storage.
POST fiscal não recebe retentativas automáticas. Rejeições fiscais ficam no resultado;
falhas de transporte geram `CTeTechnicalException`. Cancelamentos do chamador são preservados.

ActivitySource e Meter: `OpenAC.Net.CTe.Web`. Métricas: `openac.cte.requests` e
`openac.cte.request.duration`. Payloads fiscais e senhas não são incluídos nos logs da integração.
O circuit breaker é opcional e isolado pelo pool. `TimeoutTentativa` limita a tentativa HTTP.
