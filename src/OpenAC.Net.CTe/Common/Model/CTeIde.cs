// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-14-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-14-2016
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

using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Serializer;
using System;
using System.ComponentModel;
using OpenAC.Net.DFe.Core.Collection;

namespace OpenAC.Net.CTe;

public sealed partial class CTeIde : DFeParentItem<CTeIde, CTeInfCTe>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeIde()
    {
        IndGlobalizado = CTeIndicador.Nao;
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement(TipoCampo.Enum, "cUF", Id = "#005", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 10)]
    public DFeCodUF CUF { get; set; }

    [DFeElement(TipoCampo.Int, "cCT", Id = "#006", Min = 8, Max = 8, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 20)]
    public int CCT { get; set; }

    [DFeElement(TipoCampo.Int, "CFOP", Id = "#007", Min = 4, Max = 4, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 30)]
    public int CFOP { get; set; }

    [DFeElement(TipoCampo.Str, "natOp", Id = "#008", Min = 1, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 40)]
    public string NatOp { get; set; }

    [DFeElement(TipoCampo.Enum, "mod", Id = "#010", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 50)]
    public ModeloCTe Mod { get; set; }

    [DFeElement(TipoCampo.Int, "serie", Id = "#011", Min = 1, Max = 3, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 60)]
    public int Serie { get; set; }

    [DFeElement(TipoCampo.Int, "nCT", Id = "#012", Min = 1, Max = 9, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 70)]
    public int NCT { get; set; }

    [DFeElement(TipoCampo.DatHorTz, "dhEmi", Id = "#013", Min = 25, Max = 25, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 80)]
    public DateTimeOffset DhEmi { get; set; }

    [DFeElement(TipoCampo.Enum, "tpImp", Id = "#014", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 90)]
    public DFeTipoImpressao TpImp { get; set; }

    [DFeElement(TipoCampo.Enum, "tpEmis", Id = "#015", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 100)]
    public DFeTipoEmissao TpEmis { get; set; }

    [DFeElement(TipoCampo.Int, "cDV", Id = "#016", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 110)]
    public int CDV { get; set; }

    [DFeElement(TipoCampo.Enum, "tpAmb", Id = "#017", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 120)]
    public DFeTipoAmbiente TpAmb { get; set; }

    [DFeElement(TipoCampo.Enum, "tpCTe", Id = "#018", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 130)]
    public CTeTipo TpCTe { get; set; }

    [DFeElement(TipoCampo.Enum, "procEmi", Id = "#019", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 140)]
    public DFeProcessoEmissao ProcEmi { get; set; }

    [DFeElement(TipoCampo.Str, "verProc", Id = "#020", Min = 1, Max = 20, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 150)]
    public string VerProc { get; set; }

    [DFeElement(TipoCampo.Enum, "indGlobalizado", Id = "#021", Min = 1, Max = 1, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 160)]
    public CTeIndicador IndGlobalizado { get; set; }

    [DFeElement(TipoCampo.Int, "cMunEnv", Id = "#022", Min = 7, Max = 7, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 170)]
    public int CMunEnv { get; set; }

    [DFeElement(TipoCampo.Str, "xMunEnv", Id = "#023", Min = 2, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 180)]
    public string XMunEnv { get; set; }

    [DFeElement(TipoCampo.Enum, "UFEnv", Id = "#024", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 190)]
    public DFeSiglaUF UFEnv { get; set; }

    [DFeElement(TipoCampo.Enum, "modal", Id = "#025", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 200)]
    public CTeModal Modal { get; set; }

    [DFeElement(TipoCampo.Enum, "tpServ", Id = "#026", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 210)]
    public CTeTipoServico TpServ { get; set; }

    [DFeElement(TipoCampo.Int, "cMunIni", Id = "#027", Min = 7, Max = 7, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 220)]
    public int CMunIni { get; set; }

    [DFeElement(TipoCampo.Str, "xMunIni", Id = "#028", Min = 1, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 230)]
    public string XMunIni { get; set; }

    [DFeElement(TipoCampo.Enum, "UFIni", Id = "#029", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 240)]
    public DFeSiglaUF UFIni { get; set; }

    [DFeElement(TipoCampo.Int, "cMunFim", Id = "#030", Min = 7, Max = 7, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 250)]
    public int CMunFim { get; set; }

    [DFeElement(TipoCampo.Str, "xMunFim", Id = "#031", Min = 1, Max = 60, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 260)]
    public string XMunFim { get; set; }

    [DFeElement(TipoCampo.Enum, "UFFim", Id = "#032", Min = 2, Max = 2, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 270)]
    public DFeSiglaUF UFFim { get; set; }

    [DFeElement(TipoCampo.Enum, "retira", Id = "#033", Min = 1, Max = 1, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 280)]
    public CTeRetira Retira { get; set; }

    [DFeElement(TipoCampo.Str, "xDetRetira", Id = "#034", Min = 1, Max = 160, Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 290)]
    public string XDetRetira { get; set; }

    [DFeIgnore]
    public CTeIndIEToma IndIEToma { get; set; }

    [DFeIgnore]
    public ICTeTomador Tomador { get; set; }
    [DFeElement("toma3", Ordem = 300, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeToma3 Tomador3
    {
        get => Tomador is CTeToma3 toma ? toma : Tomador is CTeToma03 old ? new CTeToma3 { Toma = old.Toma } : null;
        set { if (value != null) Tomador = value; }
    }
    [DFeElement("toma03", Ordem = 300, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeToma03 Tomador03
    {
        get => Tomador is CTeToma03 toma ? toma : Tomador is CTeToma3 newer ? new CTeToma03 { Toma = newer.Toma } : null;
        set { if (value != null) Tomador = value; }
    }
    [DFeElement("toma4", Ordem = 300, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeToma4 Tomador4 { get => Tomador as CTeToma4; set { if (value != null) Tomador = value; } }
    internal bool ShouldSerializeTomador3() => Mod == ModeloCTe.CTe && Parent.Versao >= CTeVersao.v300;
    internal bool ShouldSerializeTomador03() => Mod == ModeloCTe.CTe && Parent.Versao == CTeVersao.v200;
    internal bool ShouldSerializeTomador4() => Mod == ModeloCTe.CTe;

    [DFeElement(TipoCampo.Custom, "dhCont", Id = "#057", Min = 1, Max = 21, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 310)]
    public DateTime DhCont { get; set; }

    [DFeElement(TipoCampo.Str, "xJust", Id = "#058", Min = 15, Max = 256, Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 320)]
    public string XJust { get; set; }

    #endregion Propriedades

    [DFeElement("gCompraGov", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 400)]
    public CTeCompraGovernamental CompraGovernamental { get; set; }

    [DFeElement(TipoCampo.Enum, "tpPagAnt", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 410)]
    public CTeTipoPagamentoAntecipado? TipoPagamentoAntecipado { get; set; }

    [DFeElement("gPagAntecipado", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 420)]
    public CTePagamentoAntecipado PagamentoAntecipado { get; set; }

    [DFeCollection("infPercurso", Ordem = 350, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTePercurso> InfPercurso { get; set; } = [];

    #region Methods

    internal bool ShouldSerializeIndGlobalizado()
    {
        return Parent.Versao >= CTeVersao.v300 && Mod == ModeloCTe.CTe && IndGlobalizado == CTeIndicador.Sim;
    }

    internal bool ShouldSerializeRetira()
    {
        return Mod == ModeloCTe.CTe;
    }

    internal bool ShouldSerializeXDetRetira()
    {
        return Mod == ModeloCTe.CTe;
    }

    internal bool ShouldSerializeTomador()
    {
        return Mod == ModeloCTe.CTe;
    }

    internal bool ShouldSerializeDhCont()
    {
        return Parent.Versao < CTeVersao.v400 && TpEmis == DFeTipoEmissao.FSDA;
    }

    internal bool ShouldSerializeXJust()
    {
        return Parent.Versao < CTeVersao.v400 && TpEmis == DFeTipoEmissao.FSDA;
    }

    private string SerializeDhCont()
    {
        return Parent.Versao == CTeVersao.v300
            ? DhCont.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz")
            : DhCont.ToString("s");
    }

    private object DeserializeDhCont(string value)
    {
        return value.ToData();
    }

    #endregion Methods
    internal bool ShouldSerializeCompraGovernamental() => Parent.Versao >= CTeVersao.v400;
    internal bool ShouldSerializeTipoPagamentoAntecipado() => Parent.Versao >= CTeVersao.v400;
    internal bool ShouldSerializePagamentoAntecipado() => Parent.Versao >= CTeVersao.v400;
    [DFeElement(TipoCampo.Enum, "indIEToma", Ordem = 295, Ocorrencia = Ocorrencia.Obrigatoria)]
    private CTeIndIEToma IndIETomaCTe { get => IndIEToma; set => IndIEToma = value; }
    [DFeElement(TipoCampo.Enum, "indIEToma", Ordem = 215, Ocorrencia = Ocorrencia.Obrigatoria)]
    private CTeIndIEToma IndIETomaOS { get => IndIEToma; set => IndIEToma = value; }
    internal bool ShouldSerializeIndIETomaCTe() => Mod == ModeloCTe.CTe && Parent.Versao >= CTeVersao.v300;
    internal bool ShouldSerializeIndIETomaOS() => Mod == ModeloCTe.CTeOS;
    internal bool ShouldSerializeInfPercurso() => Mod == ModeloCTe.CTeOS;
    [DFeElement(TipoCampo.Int, "forPag", Ordem = 45, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public int ForPag { get; set; }
    [DFeElement(TipoCampo.Str, "refCTE", Ordem = 165, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string RefCTe { get; set; }
    internal bool ShouldSerializeForPag() => Parent.Versao == CTeVersao.v200;
    internal bool ShouldSerializeRefCTe() => Parent.Versao == CTeVersao.v200;
}