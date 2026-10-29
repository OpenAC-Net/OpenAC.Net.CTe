using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 30-07-2018
//
// Last Modified By : RFTD
// Last Modified On : 13-01-2019
// ***********************************************************************
// <copyright file="ConsultaCadastroServiceClient.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Extensions;

namespace OpenAC.Net.CTe.Services;

public sealed partial class ConsultaCadastroServiceClient : CTeServiceClient
{
    public ConsultaCadastroServiceClient(CTeConfig config, DFeCodUF uf, X509Certificate2 certificado = null) :
        base(config, config.ObterEndereco(TipoServicoCTe.CTeConsultaCadastro, uf), certificado)
    {
        Schema = SchemaCTe.ConsCad;
        NomeArquivo = "ped-cad";
    }

    public async Task<DFeConsultaCadastroResposta> ConsultaCadastroAsync(DFeSiglaUF uf, DFeCodUF ufConsulta,
        string cpfCNPJ, string IE, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.Against<ArgumentException>(uf.IsIn(DFeSiglaUF.AN, DFeSiglaUF.EX, DFeSiglaUF.SU),
            "UF informada não pode ser AN/EX/SU");
        Guard.Against<ArgumentException>(cpfCNPJ.IsEmpty() && IE.IsEmpty(),
            "É necessário informar pelo menos 1 documento para realizar a consulta.");
        Guard.Against<ArgumentException>(!cpfCNPJ.IsEmpty() && !cpfCNPJ.IsCPF() && !cpfCNPJ.IsCNPJ(),
            "CPF ou CNPJ invalido.");

        await ServiceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var request = new StringBuilder();
            request.Append("<ConsCad versao=\"2.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\">");
            request.Append("<infCons>");
            request.Append("<xServ>CONS-CAD</xServ>");
            request.Append($"<UF>{uf.GetDFeValue()}</UF>");
            if (!IE.IsEmpty())
            {
                request.Append($"<IE>{IE.OnlyNumbers()}</IE>");
            }
            else
            {
                request.Append(cpfCNPJ.IsCNPJ()
                    ? $"<CNPJ>{cpfCNPJ.OnlyNumbers()}</CNPJ>"
                    : $"<CPF>{cpfCNPJ.OnlyNumbers()}</CPF>");
            }

            request.Append("</infCons>");
            request.Append("</ConsCad>");

            var dadosMsg = request.ToString();
            ValidateMessage(dadosMsg);

            const string xmlns = "http://www.portalfiscal.inf.br/nfe/wsdl/CadConsultaCadastro4";
            var message = $"<nfeDadosMsg xmlns=\"{xmlns}\">{dadosMsg}</nfeDadosMsg>";
            var ret = await ExecuteAsync("http://www.portalfiscal.inf.br/nfe/wsdl/CadConsultaCadastro4/consultaCadastro",
                message, "", cancellationToken).ConfigureAwait(false);

            var retorno = new DFeConsultaCadastroResposta(dadosMsg, ret, EnvelopeEnvio, EnvelopeRetorno);
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
        var result = xmlDocument.Descendants().FirstOrDefault(element => element.Name.LocalName == "retConsCad");
        if (result != null) return result.ToString();
        throw new OpenDFeCommunicationException("Elemento retConsCad não encontrado no retorno.");
    }
}