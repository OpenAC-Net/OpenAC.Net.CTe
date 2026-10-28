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
// <copyright file="CTeRecepcaoServiceClient.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Services;

public sealed partial class CTeRecepcaoServiceClient : CTeServiceClient
{
    #region Constructors

    public CTeRecepcaoServiceClient(CTeConfig config, X509Certificate2 certificado = null) :
        base(config, TipoServicoCTe.CTeRecepcao, certificado)
    {
        Schema = SchemaCTe.EnviCTe;
        NomeArquivo = "env-lot";
    }

    #endregion Constructors

    #region Methods

    public async Task<RecepcaoCTeResposta> RecepcaoLoteAsync(CTe[] ctes, string loteId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.Against<ArgumentNullException>(ctes == null, nameof(ctes));
        Guard.Against<ArgumentNullException>(loteId.IsEmpty(), "Número do lote vazio.");
        Guard.Against<ArgumentException>(!loteId.IsNumeric(), "Número do lote incorreto.");
        Guard.Against<ArgumentException>(ctes.Length < 1, "Precisa de pelo menos 1 conhecimento por lote.");
        Guard.Against<ArgumentException>(ctes.Length > 50, "So pode enviar 50 conhecimentos por lote.");

        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = new StringBuilder();
            request.Append($"<enviCTe xmlns=\"http://www.portalfiscal.inf.br/cte\" versao=\"{Configuracoes.Geral.VersaoDFe.GetDescription()}\">");
            request.Append($"<idLote>{loteId}</idLote>");

            var saveOptions = DFeSaveOptions.DisableFormatting | DFeSaveOptions.OmitDeclaration;
            if (Configuracoes.Geral.RetirarAcentos) saveOptions |= DFeSaveOptions.RemoveAccents;
            if (Configuracoes.Geral.RetirarEspacos) saveOptions |= DFeSaveOptions.RemoveSpaces;

            foreach (var cte in ctes)
            {
                var cteXml = cte.Xml.IsEmpty() ? cte.GetXml(saveOptions) : cte.Xml;
                await GravarCTeAsync(cteXml, cte.GetXmlName(), cte.InfCTe.Ide.DhEmi.DateTime, cte.InfCTe.Emit.CNPJ, cte.InfCTe.Ide.Mod, cancellationToken).ConfigureAwait(false);
                request.Append(cteXml);
            }

            request.Append("</enviCTe>");

            var dadosMsg = request.ToString();

            Guard.Against<OpenDFeException>(dadosMsg.Length > (500 * 1024),
                $"Tamanho do XML de Dados superior a 500 Kbytes. Tamanho atual: {(dadosMsg.Length / 1024M).Trunc()} Kbytes");

            ValidateMessage(dadosMsg);

            const string xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/CteRecepcao";
            var message = $"<cteDadosMsg xmlns=\"{xmlns}\">{dadosMsg}</cteDadosMsg>";
            var ret = await ExecuteAsync("http://www.portalfiscal.inf.br/cte/wsdl/CteRecepcao/cteRecepcaoLote", message, SoapHeader(xmlns), cancellationToken).ConfigureAwait(false);

            var retorno = new RecepcaoCTeResposta(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
            return retorno;
        }
        finally { ServiceGate.Release(); }
    }

    /// <inheritdoc />
    protected override string TratarRetorno(XElement xmlDocument)
    {
        var element = xmlDocument.ElementAnyNs("cteRecepcaoLoteResult");
        if (element != null) return element.FirstNode?.ToString() ?? string.Empty;

        throw new OpenDFeCommunicationException("Elemento cteRecepcaoLoteResult não encontrado no retorno.");
    }

    #endregion Methods
}