// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-15-2016
// ***********************************************************************
// <copyright file="CTeEmit.cs" company="OpenAC .Net">
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

using OpenAC.Net.Core.Generics;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeEmit : GenericClone<CTeEmit>
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeEmit()
    {
        EnderEmit = new CTeEnderecoEmit();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.StrNumberFill, "CNPJ", Id = "#098", Min = 14, Max = 14, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 10)]
    public string CNPJ { get; set; }

    [DFeElement(TipoCampo.StrNumber, "IE", Id = "#099", Min = 2, Max = 14, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 20)]
    public string IE { get; set; }

    [DFeElement(TipoCampo.StrNumber, "IEST", Id = "#100", Min = 2, Max = 14, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 30)]
    public string IEST { get; set; }

    [DFeElement(TipoCampo.Str, "xNome", Id = "#101", Min = 2, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 40)]
    public string XNome { get; set; }

    [DFeElement(TipoCampo.Str, "xFant", Id = "#102", Min = 2, Max = 60, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 50)]
    public string XFant { get; set; }

    [DFeElement("enderEmit", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 60)]
    public CTeEnderecoEmit EnderEmit { get; set; }

    #endregion Propriedades
    [DFeElement(TipoCampo.Str, "CPF", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 11)]
    public string CPF { get; set; }
    [DFeElement(TipoCampo.Int, "CRT", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 70)]
    public int CRT { get; set; } = 3;
    [DFeElement(TipoCampo.Str, "ISUFEmit", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 80)]
    public string ISUFEmit { get; set; }
    internal bool ShouldSerializeCNPJ() => !string.IsNullOrEmpty(CNPJ);
    internal bool ShouldSerializeCPF() => string.IsNullOrEmpty(CNPJ) && !string.IsNullOrEmpty(CPF);
    [DFeIgnore]
    internal CTeVersao Versao { get; set; }
    internal bool ShouldSerializeCRT() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeISUFEmit() => Versao >= CTeVersao.v400;
}