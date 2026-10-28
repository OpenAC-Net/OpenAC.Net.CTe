// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : marcosgerene
// Created          : 01-09-2019
//
// Last Modified By : marcosgerene
// Last Modified On : 01-09-2019
// ***********************************************************************
// <copyright file="CTeNormalOS.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

[DFeRoot("infCTeNorm", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTeNormalOS : GenericClone<CTeNormalOS>, IInfoCTe, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeNormalOS()
    {
        Cobr = new CTeCobranca();
        RefCTeCanc = string.Empty;
        InfCTeSub = new CTeOSInfCTeSub();
        InfModal = new CTeInfModal();
        Seg = [];
        InfDocRef = [];
        InfServico = new CTeOSInfServico();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement("infServico", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeOSInfServico InfServico { get; set; }

    [DFeCollection("infDocRef", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeOSInfDocRef> InfDocRef { get; set; }

    [DFeCollection("seg", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeOSSeg> Seg { get; set; }

    [DFeElement("infModal", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeInfModal InfModal { get; set; }

    [DFeElement("infCteSub", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeOSInfCTeSub InfCTeSub { get; set; }

    [DFeElement(TipoCampo.Str, "refCTeCanc", Id = "", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string RefCTeCanc { get; set; }

    [DFeElement("cobr", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeCobranca Cobr { get; set; }

    #endregion Propriedades

    [DFeElement("infGTVe", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 80)]
    public CTeGTVeReferenciada InfGTVe { get; set; }

    #region Methods

    internal bool ShouldSerializeInfCTeSub()
    {
        return InfCTeSub != null && !InfCTeSub.ChCte.IsEmpty();
    }

    internal bool ShouldSerializeRefCTeCanc()
    {
        return !RefCTeCanc.IsEmpty();
    }

    internal bool ShouldSerializeCobr()
    {
        return Cobr != null && Cobr.Fat != null && !Cobr.Fat.NFat.IsEmpty();
    }

    #endregion Methods
    [DFeIgnore]
    internal CTeVersao Versao { get; set; }
    internal bool ShouldSerializeInfGTVe() => Versao >= CTeVersao.v400;
}