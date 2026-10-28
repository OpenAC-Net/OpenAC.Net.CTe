// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-22-2018
// ***********************************************************************
// <copyright file="CTeInfNFe.cs" company="OpenAC .Net">
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
using System;

namespace OpenAC.Net.CTe;

public sealed partial class CTeInfNFe : GenericClone<CTeInfNFe>, ICTeInfDoc
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeInfNFe()
    {
        InfUnidCarga = [];
        InfUnidTransp = [];
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.Str, "chave", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string Chave { get; set; }

    [DFeElement(TipoCampo.Str, "PIN", Min = 2, Max = 9, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string PIN { get; set; }

    [DFeElement(TipoCampo.Dat, "dPrev", Min = 10, Max = 10, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DateTime? DPrev { get; set; }

    [DFeCollection("infUnidCarga", Id = "#279", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeTUnidCarga> InfUnidCarga { get; set; }

    [DFeCollection("infUnidTransp", Id = "#279", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeUnidadeTransp> InfUnidTransp { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeInfUnidCarga()
    {
        return InfUnidTransp.Count == 0;
    }

    internal bool ShouldSerializeInfUnidTransp()
    {
        return InfUnidCarga.Count == 0;
    }

    #endregion Methods
}