using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Extensions;

// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 30-07-2018
//
// Last Modified By : RFTD
// Last Modified On : 13-01-2019
// ***********************************************************************
// <copyright file="DistribuicaoDFeServiceClient.cs" company="OpenAC .Net">
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

namespace OpenAC.Net.CTe.Services;

public sealed partial class DistribuicaoDFeServiceClient : CTeServiceClient
{
    private readonly DFeCodUF ufAutorizador;

    public DistribuicaoDFeServiceClient(CTeConfig config, DFeCodUF uf, X509Certificate2 certificado = null) :
        base(config, config.ObterEndereco(TipoServicoCTe.DistribuicaoDFe, uf), certificado)
    {
        ufAutorizador = uf;
        Schema = SchemaCTe.DistDFeInt;
        NomeArquivo = "con-dist-dfe";
    }

    public async Task<DistribuicaoDFeResponse> ConsultaDFeAsync(string cpfCNPJ, string nsu = "", string ultNsu = "",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.Against<ArgumentException>(cpfCNPJ.IsEmpty(), "CPF/CNPJ não pode ser vazio.");
        Guard.Against<ArgumentException>(!cpfCNPJ.IsCPF() && !cpfCNPJ.IsCNPJ(), "CPF ou CNPJ invalido.");
        Guard.Against<ArgumentException>(nsu.IsEmpty() && ultNsu.IsEmpty(),
            "Deve ser informado pelo menos 1 filtro de busca (NSU ou Ultimo NSU).");

        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            const string xmlns = "http://www.portalfiscal.inf.br/cte/wsdl/CTeDistribuicaoDFe";
            var request = new StringBuilder();
            request.Append($"<cteDistDFeInteresse xmlns=\"{xmlns}\">");
            request.Append("<cteDadosMsg>");
            request.Append("<distDFeInt versao=\"1.00\" xmlns=\"http://www.portalfiscal.inf.br/cte\">");
            request.Append($"<tpAmb>{Configuracoes.WebServices.Ambiente.GetDFeValue()}</tpAmb>");
            request.Append($"<cUFAutor>{ufAutorizador.GetDFeValue()}</cUFAutor>");
            request.Append(cpfCNPJ.IsCNPJ() ? $"<CNPJ>{cpfCNPJ}</CNPJ>" : $"<CPF>{cpfCNPJ}</CPF>");

            if (nsu.IsEmpty())
            {
                request.Append("<distNSU>");
                request.Append($"<ultNSU>{ultNsu.ZeroFill(15)}</ultNSU>");
                request.Append("</distNSU>");
            }
            else
            {
                request.Append("<consNSU>");
                request.Append($"<NSU>{nsu.ZeroFill(15)}</NSU>");
                request.Append("</consNSU>");
            }


            request.Append("</distDFeInt>");
            request.Append("</cteDadosMsg>");
            request.Append("</cteDistDFeInteresse>");

            var dadosMsg = request.ToString();
            ValidateMessage(XElement.Parse(dadosMsg).Descendants()
                .Single(element => element.Name.LocalName == "distDFeInt").ToString());

            var ret = await ExecuteAsync(
                "http://www.portalfiscal.inf.br/cte/wsdl/CTeDistribuicaoDFe/cteDistDFeInteresse", dadosMsg, "",
                cancellationToken).ConfigureAwait(false);
            var retorno = new DistribuicaoDFeResponse(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
            return retorno;
        }
        finally
        {
            ServiceGate.Release();
        }
    }

    /// <inheritdoc />
    protected override string TratarRetorno(XElement xmlDocument)
    {
        var element = xmlDocument.Descendants().FirstOrDefault(x => x.Name.LocalName == "retDistDFeInt");
        return element != null
            ? element.ToString()
            : throw new OpenDFeCommunicationException("Elemento retDistDFeInt não encontrado no retorno.");
    }
}