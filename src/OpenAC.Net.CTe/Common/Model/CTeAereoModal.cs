// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-19-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-19-2016
// ***********************************************************************
// <copyright file="CTeInfModal.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Serializer;
using System;
using System.ComponentModel;
using System.Linq;
using OpenAC.Net.DFe.Core.Document;

namespace OpenAC.Net.CTe;

[DFeRoot("aereo")]
public sealed partial class CTeAereoModal : DFeDocument<CTeAereoModal>, ICTeModal, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeAereoModal()
    {
        Peri = [];
        Tarifa = new CTeAereoTarifa();
        NatCarga = new CTeAereoNatCarga();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.Int, "nMinu", Id = "", Min = 9, Max = 9, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public int NMinu { get; set; }

    [DFeElement(TipoCampo.Str, "nOCA", Id = "", Min = 11, Max = 11, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string NOCA { get; set; }

    [DFeElement(TipoCampo.Dat, "dPrevAereo", Id = "", Min = 10, Max = 10, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DateTime? DPrevAereo { get; set; }

    [DFeElement("natCarga", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeAereoNatCarga NatCarga { get; set; }

    [DFeElement("tarifa", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeAereoTarifa Tarifa { get; set; }

    [DFeCollection("peri", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeAereoPeri> Peri { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeNatCarga()
    {
        return NatCarga.CInfManu.Any() || !NatCarga.XDime.IsEmpty();
    }

    #endregion Methods
}