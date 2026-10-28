// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 03-13-2018
//
// Last Modified By : RFTD
// Last Modified On : 10-10-2018
// ***********************************************************************
// <copyright file="CTeProcEvento.cs" company="OpenAC .Net">
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

using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Document;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

[DFeRoot("procEventoCTe", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTeProcEvento : DFeDocument<CTeProcEvento>
{
    #region Events


    #endregion Events

    #region Constructor

    public CTeProcEvento()
    {
        EventoCTe = new CTeEventoCTe();
        RetEventoCTe = new CTeRetEventoCTe();
    }

    #endregion Constructor

    #region Properties

    [DFeAttribute(TipoCampo.Enum, "versao", Min = 1, Max = 7, Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeVersao Versao { get; set; }

    [DFeAttribute(TipoCampo.Str, "ipTransmissor", Min = 1, Max = 255, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string IpTransmissor { get; set; }

    [DFeElement("eventoCTe", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeEventoCTe EventoCTe { get; set; }

    [DFeElement("retEventoCTe", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeRetEventoCTe RetEventoCTe { get; set; }

    #endregion Properties
}