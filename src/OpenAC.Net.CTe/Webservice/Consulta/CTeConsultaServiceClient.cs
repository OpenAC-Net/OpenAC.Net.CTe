using System.Threading;
using System.Threading.Tasks;
// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 11-10-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-22-2017
// ***********************************************************************
// <copyright file="CTeConsultaServiceClient.cs" company="OpenAC .Net">
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Extensions;

namespace OpenAC.Net.CTe.Services;

public sealed partial class CTeConsultaServiceClient : CTeServiceClient
{
    #region Constructors

    public CTeConsultaServiceClient(CTeConfig config, X509Certificate2 certificado = null) :
        base(config, TipoServicoCTe.CTeConsultaProtocolo, certificado)
    {
        Schema = SchemaCTe.ConsSitCTe;
        NomeArquivo = "ped-sit";
    }

    #endregion Constructors

    #region Methods

    public async Task<ConsultaCTeResposta> ConsultaAsync(string chave, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.Against<ArgumentNullException>(chave.IsEmpty(), nameof(chave));

        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = new StringBuilder();
            request.Append($"<consSitCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"{Configuracoes.Geral.VersaoDFe.GetDescription()}\">");
            request.Append($"<tpAmb>{Configuracoes.WebServices.Ambiente.GetDFeValue()}</tpAmb>");
            request.Append("<xServ>CONSULTAR</xServ>");
            request.Append($"<chCTe>{chave}</chCTe>");
            request.Append("</consSitCTe>");

            var dadosMsg = request.ToString();
            ValidateMessage(dadosMsg);

            var xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/" + (Configuracoes.Geral.VersaoDFe == CTeVersao.v400 ? "CTeConsultaV4" : "CteConsulta");
            var message = $"<cteDadosMsg xmlns=\"{xmlns}\">{dadosMsg}</cteDadosMsg>";
            var ret = await ExecuteAsync(xmlns + "/cteConsultaCT", message, SoapHeader(xmlns), cancellationToken).ConfigureAwait(false);

            var retorno = new ConsultaCTeResposta(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
            return retorno;
        }
        finally { ServiceGate.Release(); }
    }

    /// <inheritdoc />
    protected override string TratarRetorno(XElement xmlDocument)
    {
        var element = xmlDocument.ElementAnyNs("cteConsultaCTResult");
        if (element != null) return element.FirstNode?.ToString() ?? string.Empty;

        throw new OpenDFeCommunicationException("Elemento cteConsultaCTResult não encontrado no retorno.");
    }

    #endregion Methods
}