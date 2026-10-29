# Demo Avalonia do OpenAC.Net.CTe

Aplicação desktop em .NET 8 com Avalonia 12.1.4, tema Fluent em tons de azul e operações
assíncronas com cancelamento.

Execute a partir de `src`:

```bash
dotnet run --project OpenAC.Net.CTe.Demo
```

## Configuração

Na seção **Configurações**, selecione o certificado PFX/P12 ou um certificado com chave
privada do repositório do usuário. Informe a senha, a UF, a versão, o ambiente e os diretórios.
O ambiente inicial é homologação e a versão inicial é 4.00.

Os schemas são copiados para a saída pela referência ao projeto da biblioteca.
O botão **Salvar configurações** grava as preferências e a senha no formato já usado pelo
demo anterior. Um `cte.config` existente no diretório de execução continua sendo carregado;
na ausência dele, o arquivo fica no diretório local de dados do usuário. O caminho completo
aparece na tela de configuração.

## Operações

- Carregar um ou vários XMLs de CT-e ou CT-e OS e validar os documentos. O envio é exclusivo da versão 4.00.
- Consultar situação, status do serviço, cadastro e distribuição de documentos.
- Enviar cancelamento por chave, protocolo, CNPJ, justificativa e sequência do evento.
- As versões 2.00 e 3.00 permitem apenas leitura, geração e impressão, sem comunicação com a SEFAZ.
- Consulta de recibos e inutilização de numeração ficam desabilitadas, pois pertencem aos serviços descontinuados.
- Gerar DACTE e PDF de evento processado, em retrato ou paisagem, e abrir no leitor de PDF
  do sistema para visualizar ou imprimir.

A área de retorno apresenta o resultado, os XMLs, os envelopes SOAP e os logs. Durante uma
operação, os controles ficam desabilitados para evitar chamadas simultâneas na mesma instância.
O botão **Cancelar operação** propaga o cancelamento até a comunicação e a persistência.
Ao fechar a aplicação durante uma operação, o demo solicita cancelamento e aguarda sua conclusão.

O cancelamento local de uma transmissão não desfaz um documento que já tenha sido recebido
pela SEFAZ. Consulte a situação pela chave antes de reenviar.

## Webservices

A lista usa o catálogo único `services.xml` e acompanha versão, ambiente, emissão e UF.
Use **Aplicar endereços** para alterar o catálogo em memória e **Exportar catálogo** para
salvá-lo. Salve as configurações após carregar ou exportar um catálogo para reutilizar seu
caminho na próxima execução. O importador INI antigo (3.00) foi desativado. O catálogo aceita somente serviços 4.00.
As opções de emissão e os serviços exibidos acompanham a UF, o ambiente e a vigência da NT 2026.004.

## Organização

A demo mantém sua configuração editável, listas de documentos, estado de tela e renderer de PDF.
Ao iniciar uma operação, ela cria `OpenCTe` com uma cópia das configurações selecionadas e passa
os documentos explicitamente aos métodos assíncronos. O catálogo editável pertence à demo e não
altera o catálogo padrão da biblioteca nem outras instâncias do cliente.

## Interface

![Tela de operações do demo Avalonia em tons de azul](Docs/preview.png)
