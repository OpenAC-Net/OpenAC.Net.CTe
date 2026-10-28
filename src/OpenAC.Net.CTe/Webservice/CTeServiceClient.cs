// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 03-04-2018
//
// Last Modified By : RFTD
// Last Modified On : 03-04-2018
// ***********************************************************************
// <copyright file="CTeServiceClient.cs" company="OpenAC .Net">
//		        		   The MIT License (MIT)
//	     		    Copyright (c) 2016 Grupo OpenAC.Net
//
//	 Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//	 The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//	 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAC.Net.DFe.Core;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core.Extensions;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Services;

/// <inheritdoc />
/// <summary>
/// </summary>
public abstract class CTeServiceClient : DFeSoapServiceClient<CTeConfig, CTeConfigGeral,
    CTeConfigWebServices, CTeConfigCertificados, CTeConfigArquivos>
{
    #region Fields

    protected readonly SemaphoreSlim ServiceGate = new(1, 1);
    private X509Certificate2 operationCertificate;
    private readonly bool ownsCertificate;

    #endregion Fields

    #region Constructors

    /// <inheritdoc />
    ///  <summary>
    ///  </summary>
    ///  <param name="config"></param>
    ///  <param name="service"></param>
    ///  <param name="certificado"></param>
    protected CTeServiceClient(CTeConfig config, TipoServicoCTe service, X509Certificate2 certificado = null) :
        this(config, ResolveAddress(config, service), certificado)
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// </summary>
    /// <param name="config"></param>
    /// <param name="url"></param>
    /// <param name="certificado"></param>
    protected CTeServiceClient(CTeConfig config, string url, X509Certificate2 certificado = null) :
        base(config, url, SoapVersion.Soap12)
    {
        operationCertificate = certificado;
        ownsCertificate = certificado == null;
    }

    #endregion Constructors

    private static string ResolveAddress(CTeConfig config, TipoServicoCTe service)
        => config.WebServices.Enderecos.TryGetValue(service, out var address) ? address
            : CTeServiceManager.GetServiceAndress(config.Geral.VersaoDFe, config.WebServices.UF.ToSiglaUF(),
                service, config.Geral.FormaEmissao, config.WebServices.Ambiente);

    #region Propriedades

    /// <summary>
    /// Schema utilizado para validação da mensagem.
    /// </summary>
    protected SchemaCTe Schema { get; set; }

    #endregion Propriedades

    #region Methods

    /// <summary>
    /// Retorna o cabeçalho para usar no envelope SOAP.
    /// </summary>
    /// <param name="xmlns"></param>
    /// <returns></returns>
    protected virtual string SoapHeader(string xmlns)
    {
        if (Configuracoes.Geral.VersaoDFe == CTeVersao.v400) return string.Empty;
        var header = new StringBuilder();
        header.Append($"<cteCabecMsg xmlns=\"{xmlns}\">");
        header.Append($"<cUF>{(int)Configuracoes.WebServices.UF}</cUF>");
        header.Append($"<versaoDados>{Configuracoes.Geral.VersaoDFe.GetDescription()}</versaoDados>");
        header.Append("</cteCabecMsg>");
        return header.ToString();
    }

    protected void ValidateMessage(string xml)
    {
        ValidateMessage(xml, Configuracoes.Arquivos.GetSchema(Schema));
    }

    protected void ValidateMessage(string xml, SchemaCTe schema)
    {
        ValidateMessage(xml, Configuracoes.Arquivos.GetSchema(schema));
    }

    protected new X509Certificate2 Certificado => operationCertificate ??= Configuracoes.Certificados.ObterCertificado();

    // O DFe.Core ainda expõe estes pontos de extensão síncronos. O CT-e utiliza os caminhos Async.
    protected override string Execute(string soapAction, string message, string soapHeader, params string[] soapNamespaces)
        => throw new NotSupportedException("Utilize ExecuteAsync para comunicação SOAP do CT-e.");

    protected async Task<string> ExecuteAsync(string soapAction, string message, string soapHeader,
        CancellationToken cancellationToken, params string[] soapNamespaces)
    {
        cancellationToken.ThrowIfCancellationRequested();
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        EnvelopeEnvio = $"<soapenv:Envelope xmlns:soapenv=\"{soap}\" {string.Join(" ", soapNamespaces)}><soapenv:Header>{soapHeader}</soapenv:Header><soapenv:Body>{message}</soapenv:Body></soapenv:Envelope>";
        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Content = new StringContent(EnvelopeEnvio, Encoding.UTF8);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"application/soap+xml; charset=utf-8; action=\"{soapAction}\"");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Configuracoes.WebServices.TimeoutOperacao);
        var token = timeout.Token;
        ArquivoEnvio = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{NomeArquivo}_envio.xml";
        await GravarSoapAsync(EnvelopeEnvio, ArquivoEnvio, token).ConfigureAwait(false);
        using var response = await Configuracoes.HttpTransport.SendAsync(request, Configuracoes,
            Certificado, token).ConfigureAwait(false);
        EnvelopeRetorno = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        ArquivoResposta = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{NomeArquivo}_retorno.xml";
        await GravarSoapAsync(EnvelopeRetorno, ArquivoResposta, token).ConfigureAwait(false);
        var document = XDocument.Parse(EnvelopeRetorno);
        var body = document.Root?.Element(soap + "Body")
            ?? throw new OpenDFeCommunicationException("Corpo SOAP não encontrado no retorno.");
        var fault = body.Element(soap + "Fault");
        if (fault != null) throw new OpenDFeCommunicationException(fault.Value);
        response.EnsureSuccessStatusCode();
        return TratarRetorno(body);
    }

    protected override void GravarSoap(string content, string name)
        => throw new NotSupportedException("Utilize GravarSoapAsync para persistência SOAP do CT-e.");

    private Task GravarSoapAsync(string content, string name, CancellationToken token)
        => !Configuracoes.WebServices.Salvar ? Task.CompletedTask
            : Configuracoes.DocumentStore.SaveAsync(Path.Combine(Configuracoes.Arquivos.PathSalvar, name), NormalizeXml(content), token);

    protected Task GravarCTeAsync(string content, string name, DateTime date, string cnpj, ModeloCTe model, CancellationToken token)
        => !Configuracoes.Arquivos.Salvar ? Task.CompletedTask
            : Configuracoes.DocumentStore.SaveAsync(Path.Combine(Configuracoes.Arquivos.GetPathCTe(date, cnpj, model), name), NormalizeXml(content), token);

    protected Task GravarEventoAsync(string content, string name, CTeTipoEvento type, DateTime date, string cnpj, CancellationToken token)
        => !Configuracoes.Arquivos.Salvar ? Task.CompletedTask
            : Configuracoes.DocumentStore.SaveAsync(Path.Combine(Configuracoes.Arquivos.GetPathEvento(type, cnpj, date), name), NormalizeXml(content), token);

    protected Task GravarInutilizacaoAsync(string content, string name, DateTime date, string cnpj, CancellationToken token)
        => !Configuracoes.Arquivos.Salvar ? Task.CompletedTask
            : Configuracoes.DocumentStore.SaveAsync(Path.Combine(Configuracoes.Arquivos.GetPathInu(date, cnpj), name), NormalizeXml(content), token);

    private static string NormalizeXml(string content)
        => "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + content.RemoverDeclaracaoXml();

    protected override void DisposeManaged()
    {
        if (ownsCertificate) operationCertificate?.Dispose();
        ServiceGate.Dispose();
        base.DisposeManaged();
    }

    #endregion Methods
}
