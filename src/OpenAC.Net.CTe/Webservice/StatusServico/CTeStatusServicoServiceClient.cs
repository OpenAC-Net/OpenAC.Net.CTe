using System.Threading;
using System.Threading.Tasks;
// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 11-10-2016
//
// Last Modified By : RFTD
// Last Modified On : 11-10-2016
// ***********************************************************************
// <copyright file="CTeStatusServicoServiceClient.cs" company="OpenAC .Net">
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

using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Extensions;

namespace OpenAC.Net.CTe.Services;

public sealed partial class CTeStatusServicoServiceClient : CTeServiceClient
{
    #region Constructors

    public CTeStatusServicoServiceClient(CTeConfig config, X509Certificate2 certificado = null) :
        base(config, TipoServicoCTe.CTeStatusServico, certificado)
    {
        Schema = SchemaCTe.ConsStatServCTe;
        NomeArquivo = "ped-sta";
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Consulta a situação do serviço da CTe.
    /// </summary>
    /// <returns></returns>
    public async Task<ConsultaStatusResposta> StatusServicoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = new StringBuilder();
            request.Append($"<consStatServCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"{Configuracoes.Geral.VersaoDFe.GetDescription()}\">");
            request.Append($"<tpAmb>{Configuracoes.WebServices.Ambiente.GetDFeValue()}</tpAmb>");
            request.Append($"<cUF>{Configuracoes.WebServices.UF.GetDFeValue()}</cUF>");
            request.Append("<xServ>STATUS</xServ>");
            request.Append("</consStatServCTe>");

            var dadosMsg = request.ToString();
            ValidateMessage(dadosMsg);

            var xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/" + (Configuracoes.Geral.VersaoDFe == CTeVersao.v400 ? "CTeStatusServicoV4" : "CteStatusServico");
            var message = $"<cteDadosMsg xmlns=\"{xmlns}\">{dadosMsg}</cteDadosMsg>";
            var ret = await ExecuteAsync(xmlns + "/cteStatusServicoCT", message, SoapHeader(xmlns), cancellationToken).ConfigureAwait(false);

            var retorno = new ConsultaStatusResposta(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
            return retorno;
        }
        finally { ServiceGate.Release(); }
    }

    /// <inheritdoc />
    protected override string TratarRetorno(XElement xmlDocument)
    {
        var element = xmlDocument.ElementAnyNs("cteStatusServicoCTResult");
        if (element != null) return element.FirstNode?.ToString() ?? string.Empty;

        throw new OpenDFeCommunicationException("Elemento cteStatusServicoCTResult não encontrado no retorno.");
    }

    #endregion Methods
}