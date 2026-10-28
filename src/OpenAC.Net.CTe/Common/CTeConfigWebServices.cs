// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 06-11-2017
//
// Last Modified By : RFTD
// Last Modified On : 06-11-2017
// ***********************************************************************
// <copyright file="CTeConfigWebServices.cs" company="OpenAC .Net">
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
using System.Collections.Generic;
using OpenAC.Net.CTe.Services;
using System.ComponentModel;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Configuracao;

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed partial class CTeConfigWebServices : DFeWebserviceConfigBase, INotifyPropertyChanged
{
    /// <summary>Sobrescritas por instância para endpoints de integração ou autorizadores específicos.</summary>
    public Dictionary<TipoServicoCTe, string> Enderecos { get; } = new();

    #region Fields

    private DFeCodUF uf;

    #endregion Fields

    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CTeConfigWebServices"/> class.
    /// </summary>
    internal CTeConfigWebServices()
    {
        uf = DFeCodUF.MS;
    }

    #endregion Constructor

    /// <summary>Limite por chamada SOAP, incluindo persistência de envelopes.</summary>
    public TimeSpan TimeoutOperacao { get; set; } = TimeSpan.FromSeconds(100);

    #region Properties

    [Browsable(true)]
    public DFeCodUF UF
    {
        get => uf;
        set
        {
            if (value == uf) return;

            Guard.Against<ArgumentException>(value.IsIn(DFeCodUF.EX, DFeCodUF.AN, DFeCodUF.SU), "Estado informado incorreto.");

            uf = value;
        }
    }

    #endregion Properties
}