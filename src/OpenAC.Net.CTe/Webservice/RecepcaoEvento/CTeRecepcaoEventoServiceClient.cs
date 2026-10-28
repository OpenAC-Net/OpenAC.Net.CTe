using System.Threading;
using System.Threading.Tasks;
// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 11-10-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-23-2018
// ***********************************************************************
// <copyright file="CTeRecepcaoEventoServiceClient.cs" company="OpenAC .Net">
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

using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Extensions;

namespace OpenAC.Net.CTe.Services;

public sealed partial class CTeRecepcaoEventoServiceClient : CTeServiceClient
{
    #region Constructors

    ///  <summary>
    ///
    ///  </summary>
    ///  <param name="config"></param>
    /// <param name="service"></param>
    /// <param name="certificado"></param>
    public CTeRecepcaoEventoServiceClient(CTeConfig config, TipoServicoCTe service, X509Certificate2 certificado = null) :
        base(config, service, certificado)
    {
        Schema = SchemaCTe.EventoCTe;
        NomeArquivo = "ped-eve";
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    ///
    /// </summary>
    /// <param name="nSeqEvento"></param>
    /// <param name="chave"></param>
    /// <param name="cnpj"></param>
    /// <param name="evento"></param>
    /// <returns></returns>
    public async Task<RecepcaoEventoResposta> RecepcaoEventoAsync(int nSeqEvento, string chave, string cnpj, IEventoCTe evento, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.Against<ArgumentNullException>(chave.IsEmpty(), nameof(chave));
        Guard.Against<ArgumentNullException>(cnpj.IsEmpty(), nameof(cnpj));
        Guard.Against<ArgumentOutOfRangeException>(nSeqEvento < 1 || nSeqEvento > (Configuracoes.Geral.VersaoDFe == CTeVersao.v400 ? 999 : 99), nameof(nSeqEvento));
        Guard.Against<ArgumentNullException>(evento == null, nameof(evento));

        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            const DFeSaveOptions saveOptions = DFeSaveOptions.DisableFormatting |
                                               DFeSaveOptions.OmitDeclaration;

            CTeTipoEvento tipo;
            string xmlEvento;
            var date = DateTimeOffset.Now;
            var versao = Configuracoes.Geral.VersaoDFe.GetDescription();
            switch (evento)
            {
                case CTeEvCancCTe evtCTe:
                    xmlEvento = evtCTe.GetXml(saveOptions);
                    tipo = CTeTipoEvento.Cancelamento;
                    await GravarEventoAsync(xmlEvento, $"{chave}-can-eve.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
                    ValidateMessage(xmlEvento, SchemaCTe.EvCancCTe);
                    break;

                case CTeEvCCeCTe evtCTe:
                    xmlEvento = evtCTe.GetXml(saveOptions);
                    tipo = CTeTipoEvento.CartaCorrecao;
                    await GravarEventoAsync(xmlEvento, $"{chave}-cce-eve.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
                    ValidateMessage(xmlEvento, SchemaCTe.EvCCeCTe);
                    break;

                case CTeEvEPEC evtCTe:
                    xmlEvento = evtCTe.GetXml(saveOptions);
                    tipo = CTeTipoEvento.EPEC;
                    await GravarEventoAsync(xmlEvento, $"{chave}-ped-epec.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
                    ValidateMessage(xmlEvento, SchemaCTe.EvEPECCTe);
                    break;

                case CTeEvRegMultimodal evtCTe:
                    xmlEvento = evtCTe.GetXml(saveOptions);
                    tipo = CTeTipoEvento.RegistroMultiModal;
                    await GravarEventoAsync(xmlEvento, $"{chave}-rmulti-eve.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
                    ValidateMessage(xmlEvento, SchemaCTe.EvRegMultimodal);
                    break;

                case CTeEvPrestDesacordo evtCTe:
                    xmlEvento = evtCTe.GetXml(saveOptions);
                    tipo = CTeTipoEvento.PrestacaoServicoDesacordo;
                    await GravarEventoAsync(xmlEvento, $"{chave}-desa-eve.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
                    ValidateMessage(xmlEvento, SchemaCTe.EvPrestDesacordo);
                    break;

                default:
                    throw new ArgumentException("O evento informado é desconhecido ou não está implementado.");
            }

            var data = Configuracoes.Geral.VersaoDFe == CTeVersao.v200
                ? date.ToString("yyyy-MM-ddTHH:mm:dd")
                : date.ToString("yyyy-MM-ddTHH:mm:sszzz");

            var sequence = nSeqEvento.ToString(Configuracoes.Geral.VersaoDFe == CTeVersao.v400 ? "D3" : "D2");
            var idEvento = $"ID{tipo.GetDFeValue()}{chave}{sequence}";

            var request = new StringBuilder();
            request.Append($"<eventoCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"{versao}\">");
            request.Append($"<infEvento Id=\"{idEvento}\">");
            request.Append($"<cOrgao>{Configuracoes.WebServices.UF.GetDFeValue()}</cOrgao>");
            request.Append($"<tpAmb>{Configuracoes.WebServices.Ambiente.GetDFeValue()}</tpAmb>");
            request.Append($"<CNPJ>{cnpj}</CNPJ>");
            request.Append($"<chCTe>{chave}</chCTe>");
            request.Append($"<dhEvento>{data}</dhEvento>");
            request.Append($"<tpEvento>{tipo.GetDFeValue()}</tpEvento>");
            request.Append($"<nSeqEvento>{nSeqEvento}</nSeqEvento>");
            request.Append($"<detEvento versaoEvento=\"{versao}\">");
            request.Append(xmlEvento.Trim());
            request.Append("</detEvento>");
            request.Append("</infEvento>");
            request.Append("</eventoCTe>");

            var dadosMsg = request.ToString();
            dadosMsg = XmlSigning.AssinarXml(dadosMsg, "eventoCTe", "infEvento", Certificado);
            ValidateMessage(dadosMsg);

            var xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/" + (Configuracoes.Geral.VersaoDFe == CTeVersao.v400 ? "CTeRecepcaoEventoV4" : "CteRecepcaoEvento");
            var message = $"<cteDadosMsg xmlns=\"{xmlns}\">{dadosMsg}</cteDadosMsg>";
            var ret = await ExecuteAsync(xmlns + "/cteRecepcaoEvento", message, SoapHeader(xmlns), cancellationToken).ConfigureAwait(false);

            var retorno = new RecepcaoEventoResposta(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
            XNamespace fiscal = "http://www.portalfiscal.inf.br/cte";
            var processed = new XElement(fiscal + "procEventoCTe", new XAttribute("versao", versao),
                XElement.Parse(dadosMsg), XElement.Parse(ret)).ToString(SaveOptions.DisableFormatting);
            await GravarEventoAsync(processed, $"{idEvento.Substring(2)}-procEventoCTe.xml", tipo, date.DateTime, cnpj, cancellationToken).ConfigureAwait(false);
            return retorno;
        }
        finally { ServiceGate.Release(); }
    }

    /// <inheritdoc />
    protected override string TratarRetorno(XElement xmlDocument)
    {
        var element = xmlDocument.ElementAnyNs("cteRecepcaoEventoResult");
        if (element != null) return element.FirstNode?.ToString() ?? string.Empty;

        throw new OpenDFeCommunicationException("Elemento cteRecepcaoEventoResult não encontrado no retorno.");
    }

    #endregion Methods
}