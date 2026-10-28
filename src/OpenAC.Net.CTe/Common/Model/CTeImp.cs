// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-15-2016
// ***********************************************************************
// <copyright file="CTeImp.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeImp : GenericClone<CTeImp>
{
    #region Events


    #endregion Events

    #region Constructors

    public CTeImp()
    {
        ICMSUFFim = new CTeICMSUFFim();
        ICMS = new CTeICMS();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement("ICMS", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 10)]
    public CTeICMS ICMS { get; set; }

    [DFeElement(TipoCampo.De2, "vTotTrib", Id = "#250", Min = 1, Max = 15, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 20)]
    public decimal? VTotTrib { get; set; }

    [DFeElement(TipoCampo.Str, "infAdFisco", Id = "#251", Min = 1, Max = 2000, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 30)]
    public string InfAdFisco { get; set; }

    [DFeElement("ICMSUFFim", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 40)]
    public CTeICMSUFFim ICMSUFFim { get; set; }

    #endregion Propriedades

    [DFeElement("IBSCBS", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 60)]
    public CTeIBSCBS IBSCBS { get; set; }

    [DFeElement(TipoCampo.De2, "vTotDFe", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 70)]
    public decimal? VTotDFe { get; set; }

    [DFeElement("infTribFed", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 50)]
    public CTeTributosFederais InfTribFed { get; set; }

    #region Methods

    internal bool ShouldSerializeICMSUFFim()
    {
        return ICMSUFFim != null && ICMSUFFim.PICMSInterPart > 0;
    }

    #endregion Methods
    [DFeIgnore]
    internal CTeVersao Versao { get; set; }
    internal bool ShouldSerializeIBSCBS() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeVTotDFe() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeInfTribFed() => Versao >= CTeVersao.v400;
}