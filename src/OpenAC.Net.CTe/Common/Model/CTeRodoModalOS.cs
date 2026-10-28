// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : marcosgerene
// Created          : 09-01-2019
//
// Last Modified By : marcosgerene
// Last Modified On : 09-01-2019
// ***********************************************************************
// <copyright file="CTeRodoModalOS.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Document;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

[DFeRoot("rodoOS")]
public sealed partial class CTeRodoModalOS : DFeDocument<CTeRodoModalOS>, ICTeModal
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeRodoModalOS()
    {
        Veic = [];
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.StrNumber, "TAF", Id = "", Min = 12, Max = 12, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string TAF { get; set; }

    [DFeElement(TipoCampo.StrNumber, "NroRegEstadual", Id = "", Min = 25, Max = 25, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string NroRegEstadual { get; set; }

    [DFeCollection("veic", MinSize = 0, MaxSize = 1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeOSVeic> Veic { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeTAF()
    {
        return !TAF.IsEmpty();
    }

    internal bool ShouldSerializeNroRegEstadual()
    {
        return !NroRegEstadual.IsEmpty();
    }

    #endregion Methods
}