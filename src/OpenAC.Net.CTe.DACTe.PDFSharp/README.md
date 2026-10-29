# OpenAC.Net.CTe.DACTe.PDFSharp

Geração de PDF em C# com PDFsharp **6.2.4**, fixado por compatibilidade e licença MIT.
As fontes Liberation Sans são embutidas; a licença OFL acompanha o pacote.
Não depende de fontes instaladas, GDI+, impressora ou interface gráfica.


`GerarPDF(params CTeProc[])` reúne documentos no mesmo PDF.
`ImprimirPDF(CTeProc[])` grava um arquivo por documento em `Configuracoes.DiretorioSaida`.
Os métodos `Imprimir` produzem o mesmo PDF; o envio a uma impressora física cabe à aplicação.

`GerarEventoPDF` e `GerarInutilizacaoPDF` aceitam os documentos processados correspondentes.

O PDF inclui canhoto, emitente, chave Code 128 C, QR Code quando informado no XML,
participantes, tomador, valores, impostos, carga/serviço, documentos, dados dos modais,
cobrança e observações. Textos longos continuam em novas páginas com identificação e numeração.
Homologação, cancelamento e ausência de autorização recebem avisos visíveis.

O QR Code utiliza o conteúdo de `CTe.InfCTeSupl.QrCode`; o gerador não inventa URL ou assinatura.
A chave de acesso deve ter 44 dígitos, com ou sem o prefixo `CTe`.

Para proteger o arquivo, configure `SenhaUsuario`, `SenhaProprietario`, `PermitirImpressao`
e `PermitirCopia`. Gere uma instância de renderer por operação/empresa.
O resolver de fontes da aplicação hospedeira é preservado, quando já estiver configurado;
nesse caso ele deve conseguir resolver a família `Liberation Sans`.

O renderer aceita CT-e e CT-e OS processados nas versões suportadas pela biblioteca,
incluindo 4.00. Os campos de IBS/CBS aparecem em um bloco próprio quando informados.
Contingência não gera uma chave adicional de segurança. Valide o formulário com documentos
reais e o manual aplicável antes do uso fiscal.

Referências consultadas para a organização em blocos (implementação original em C#):

- [ACBrCTeDACTeFPDF.pas](https://svn.code.sf.net/p/acbr/code/trunk2/Fontes/ACBrDFe/ACBrCTe/DACTE/FPDF/ACBrCTeDACTeFPDF.pas)
- [ACBrCTeUtilsFPDF.pas](https://svn.code.sf.net/p/acbr/code/trunk2/Fontes/ACBrDFe/ACBrCTe/DACTE/FPDF/ACBrCTeUtilsFPDF.pas)

O exemplo de integração está no [README principal](../../README.md).
