// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-24-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-24-2016
// ***********************************************************************
// <copyright file="CTeInfCTeSub.cs" company="OpenAC .Net">
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

using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Core.Generics;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

/// <inheritdoc />
public sealed partial class CTeInfCTeSub : GenericClone<CTeInfCTeSub>
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeInfCTeSub()
    {
        IndAlteraToma = CTeIndicador.Nao;
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.Str, "chCte", Id = "", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string ChCte { get; set; }

    [DFeElement(TipoCampo.Str, "refCteAnu", Id = "", Min = 44, Max = 44, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string RefCteAnu { get; set; }

    [DFeElement("tomaICMS", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeTomaICMS TomaIcms { get; set; }

    [DFeElement("indAlteraToma", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeIndicador IndAlteraToma { get; set; }

    #endregion Propriedades


    #region Methods

    internal bool ShouldSerializeRefCteAnu()
    {
        return Versao < CTeVersao.v400 && !RefCteAnu.IsEmpty();
    }

    internal bool ShouldSerializeTomaIcms()
    {
        return Versao < CTeVersao.v400 && RefCteAnu.IsEmpty();
    }

    internal bool ShouldSerializeIndAlteraToma()
    {
        return IndAlteraToma == CTeIndicador.Sim;
    }

    #endregion Methods
    [DFeIgnore]
    internal CTeVersao Versao { get; set; }
}