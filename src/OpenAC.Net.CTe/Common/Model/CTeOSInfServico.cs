// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : marcosgerene
// Created          : 01-09-2019
//
// Last Modified By : marcosgerene
// Last Modified On : 01-09-2019
// ***********************************************************************
// <copyright file="CTeIde.cs" company="OpenAC .Net">
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
using OpenAC.Net.Core.Generics;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeOSInfServico : GenericClone<CTeInfCarga>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeOSInfServico()
    {
        InfQ = [];
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.Str, "xDescServ", Id = "#133", Min = 1, Max = 30, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string XDescServ { get; set; }

    [DFeCollection("infQ", Min = 0, Max = 1, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTEeOSInfQ> InfQ { get; set; }

    #endregion Propriedades

}