// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-24-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-22-2018
// ***********************************************************************
// <copyright file="CTeInfCteSub.cs" company="OpenAC .Net">
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

using System.ComponentModel;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Core.Generics;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeTomaICMS : GenericClone<CTeTomaICMS>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Propriedades

    [DFeElement(TipoCampo.StrNumber, "refCte", Id = "#395", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string RefCte { get; set; }

    [DFeElement("refNF", Id = "#398", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeRefNF RefNF { get; set; }

    [DFeElement(TipoCampo.StrNumber, "refNFe", Id = "#397", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string RefNFe { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeRefCte()
    {
        return !RefCte.IsEmpty() && RefNFe.IsEmpty();
    }

    internal bool ShouldSerializeRefNF()
    {
        return RefCte.IsEmpty() && RefNFe.IsEmpty();
    }

    internal bool ShouldSerializeRefNFe()
    {
        return RefCte.IsEmpty() && !RefNFe.IsEmpty();
    }

    #endregion Methods
}