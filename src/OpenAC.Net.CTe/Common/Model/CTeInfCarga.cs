// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : marcosgerene
// Last Modified On : 09-01-2019
// ***********************************************************************
// <copyright file="CTeInfCarga.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeInfCarga : GenericClone<CTeInfCarga>
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeInfCarga()
    {
        InfQ = [];
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.De2, "vCarga", Id = "#254", Min = 1, Max = 15, Ocorrencia = Ocorrencia.Obrigatoria)]
    public decimal VCarga { get; set; }

    [DFeElement(TipoCampo.Str, "proPred", Id = "#255", Min = 1, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string ProPred { get; set; }

    [DFeElement(TipoCampo.Str, "xOutCat", Id = "#256", Min = 1, Max = 30, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string XOutCat { get; set; }

    [DFeCollection("infQ", MinSize = 1, MaxSize = 990, Ocorrencia = Ocorrencia.Obrigatoria)]
    public DFeCollection<CTeInfQ> InfQ { get; set; }

    [DFeElement(TipoCampo.De2, "vCargaAverb", Id = "#", Min = 1, Max = 15, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public decimal? VCargaAverb { get; set; }

    #endregion Propriedades
}