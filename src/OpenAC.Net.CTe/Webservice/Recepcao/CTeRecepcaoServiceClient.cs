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
using System.Xml.Linq;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;

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

    /// <summary>
    /// A recepção em lote das versões 2.00 e 3.00 foi descontinuada.
    /// Utilize OpenCTe.EnviarAsync ou CTeAutorizacaoServiceClient para a versão 4.00.
    /// </summary>
    public Task<RecepcaoCTeResposta> RecepcaoLoteAsync(CTe[] ctes, string loteId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotSupportedException("A recepção em lote de CT-e 2.00 e 3.00 foi descontinuada. " +
            "Utilize a autorização síncrona da versão 4.00.");
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