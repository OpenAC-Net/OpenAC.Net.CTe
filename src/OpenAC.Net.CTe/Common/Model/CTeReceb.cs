// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-15-2016
// ***********************************************************************
// <copyright file="CTeReceb.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeReceb : DFeParentItem<CTeReceb, CTeInfCTe>
{
    #region Events

    #endregion Events

    #region Constructors

    public CTeReceb()
    {
        EnderReceb = new CTeEndereco();
    }

    public CTeReceb(CTeInfCTe parent) : this()
    {
        Parent = parent;
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.StrNumberFill, "CPF", Id = "#161", Min = 11, Max = 11, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string CPF { get; set; }

    [DFeElement(TipoCampo.StrNumberFill, "CNPJ", Id = "#162", Min = 14, Max = 14, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string CNPJ { get; set; }

    [DFeIgnore]
    public string IE { get; set; }

    [DFeElement(TipoCampo.Str, "IE", Id = "#163", Min = 0, Max = 14, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private string IEXml
    {
        get => CTeUtils.NormalizarNumeroOuIsento(IE);
        set => IE = value;
    }

    [DFeIgnore]
    public string XNome { get; set; }

    [DFeElement(TipoCampo.Str, "xNome", Id = "#164", Min = 2, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria)]
    private string XNomeXml
    {
        get => Parent?.Ide?.TpAmb == DFeTipoAmbiente.Homologacao ? CTeConst.CTeHomologação : XNome;
        set => XNome = value;
    }

    [DFeElement(TipoCampo.StrNumber, "fone", Id = "#165", Min = 7, Max = 12, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string Fone { get; set; }

    [DFeElement("enderReceb", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeEndereco EnderReceb { get; set; }

    [DFeElement(TipoCampo.Str, "email", Id = "#177", Min = 1, Max = 60, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string Email { get; set; }

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeCPF()
    {
        return CNPJ.IsEmpty();
    }

    internal bool ShouldSerializeCNPJ()
    {
        return CPF.IsEmpty();
    }

    #endregion Methods
    internal bool ShouldSerializeFone() => !string.IsNullOrEmpty(Fone);
}