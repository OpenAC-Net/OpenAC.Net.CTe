// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-16-2016
// ***********************************************************************
// <copyright file="CTeNormal.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeNormal : GenericClone<CTeNormal>, IInfoCTe, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeNormal()
    {
        InfServVinc = [];
        InfGlobalizado = new CTeInfGlobalizado();
        InfCTeSub = new CTeInfCTeSub();
        Cobr = new CTeCobranca();
        VeicNovos = [];
        InfModal = new CTeInfModal();
        DocAnt = [];
        InfDoc = [];
        InfCarga = new CTeInfCarga();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement("infCarga", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeInfCarga InfCarga { get; set; }

    [DFeItem(typeof(CTeInfNF), "infNF")]
    [DFeItem(typeof(CTeInfNFe), "infNFe")]
    [DFeItem(typeof(CTeInfOutros), "infOutros")]
    [DFeCollection("infDoc", Ocorrencia = Ocorrencia.Obrigatoria)]
    public DFeCollection<ICTeInfDoc> InfDoc { get; set; }

    [DFeItem(typeof(CTeEmiDocAnt), "emiDocAnt")]
    [DFeCollection("docAnt", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeEmiDocAnt> DocAnt { get; set; }

    [DFeElement("infModal", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeInfModal InfModal { get; set; }

    [DFeCollection("veicNovos", Ocorrencia = Ocorrencia.Obrigatoria)]
    public DFeCollection<CTeVeicNovos> VeicNovos { get; set; }

    [DFeElement("cobr", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeCobranca Cobr { get; set; }

    [DFeElement("infCteSub", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeInfCTeSub InfCTeSub { get; set; }

    [DFeElement("infGlobalizado", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeInfGlobalizado InfGlobalizado { get; set; }

    [DFeItem(typeof(CTeInfCTeMultimodal), "infCTeMultimodal")]
    [DFeCollection("infServVinc", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeInfCTeMultimodal> InfServVinc { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeInfGlobalizado()
    {
        return InfGlobalizado != null && !InfGlobalizado.XObs.IsEmpty();
    }

    internal bool ShouldSerializeInfCTeSub()
    {
        if (InfCTeSub != null) InfCTeSub.Versao = Versao;
        return InfCTeSub != null && !InfCTeSub.ChCte.IsEmpty();
    }

    #endregion Methods
    [DFeIgnore]
    internal CTeVersao Versao { get; set; }
    internal bool ShouldSerializeInfDoc() => InfDoc?.Count > 0;
    internal bool ShouldSerializeDocAnt() => DocAnt?.Count > 0;
    internal bool ShouldSerializeCobr() => Cobr != null && (!string.IsNullOrEmpty(Cobr.Fat?.NFat) || Cobr.Dup?.Count > 0);
    internal bool ShouldSerializeInfServVinc() => InfServVinc?.Count > 0;
}