// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-12-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-22-2018
// ***********************************************************************
// <copyright file="CTeInfCTe.cs" company="OpenAC .Net">
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
using System.Linq;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Core.Generics;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

public sealed partial class CTeInfCTe : GenericClone<CTeInfCTe>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Fields

    private CTeIde ide;
    private CTeOSToma toma;
    private CTeExped exped;
    private CTeRem rem;
    private CTeReceb receb;
    private CTeDest dest;

    #endregion Fields

    #region Contructors

    public CTeInfCTe()
    {
        InfRespTec = new CTeInfRespTec();
        AutXml = [];
        Imp = new CTeImp();
        VPrest = new CTeVPrest();
        Dest = new CTeDest(this);
        Receb = new CTeReceb(this);
        exped = new CTeExped(this);
        rem = new CTeRem(this);
        Emit = new CTeEmit();
        Compl = new CTeCompl();
        Ide = new CTeIde();
        Versao = CTeVersao.v300;
    }

    #endregion Contructors

    #region Propriedades

    [DFeAttribute(TipoCampo.Enum, "versao", Id = "#002", Min = 4, Max = 4, Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeVersao Versao { get; set; }

    [DFeAttribute(TipoCampo.Str, "Id", Id = "#003", Min = 44, Max = 44, Ocorrencia = Ocorrencia.Obrigatoria)]
    public string Id { get; set; }

    [DFeElement("ide", Id = "#004", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 10)]
    public CTeIde Ide
    {
        get => ide;
        set
        {
            if (value.Parent != this)
            {
                value.Parent = this;
            }

            ide = value;
        }
    }

    [DFeElement("compl", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 20)]
    public CTeCompl Compl { get; set; }

    [DFeElement("emit", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 30)]
    public CTeEmit Emit { get; set; }

    [DFeElement("rem", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 40)]
    public CTeRem Rem
    {
        get => rem;
        set
        {
            value ??= new CTeRem(this);
            if (value.Parent != this)
            {
                value.Parent = this;
            }

            rem = value;
        }
    }

    [DFeElement("exped", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 50)]
    public CTeExped Exped
    {
        get => exped;
        set
        {
            value ??= new CTeExped(this);
            if (value.Parent != this)
            {
                value.Parent = this;
            }

            exped = value;
        }
    }

    [DFeElement("receb", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 60)]
    public CTeReceb Receb
    {
        get => receb;
        set
        {
            value ??= new CTeReceb(this);
            if (value.Parent != this)
            {
                value.Parent = this;
            }

            receb = value;
        }
    }

    [DFeElement("dest", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 70)]
    public CTeDest Dest
    {
        get => dest;
        set
        {
            value ??= new CTeDest(this);
            if (value.Parent != this)
            {
                value.Parent = this;
            }

            dest = value;
        }
    }

    [DFeElement("vPrest", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 90)]
    public CTeVPrest VPrest { get; set; }

    [DFeElement("imp", Ocorrencia = Ocorrencia.Obrigatoria, Ordem = 100)]
    public CTeImp Imp { get; set; }

    [DFeIgnore]
    public IInfoCTe InfoCTe { get; set; }

    [DFeElement("infCTeNorm", Ordem = 120, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeNormal InfoNormal
    {
        get => InfoCTe as CTeNormal;
        set { if (Ide.Mod == ModeloCTe.CTe && value != null) InfoCTe = value; }
    }
    [DFeElement("infCteComp", Ordem = 122, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeComplemento InfoComplemento { get => InfoCTe as CTeComplemento; set { if (value != null) InfoCTe = value; } }
    [DFeElement("infCteAnu", Ordem = 123, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeAnulacao InfoAnulacao { get => InfoCTe as CTeAnulacao; set { if (value != null) InfoCTe = value; } }
    internal bool ShouldSerializeInfoNormal() => ShouldSerializeInfoCTe();
    internal bool ShouldSerializeInfoComplemento() => InfoCTe is CTeComplemento && (Versao < CTeVersao.v400 || Complementos.Count == 0);
    internal bool ShouldSerializeInfoAnulacao() => Versao < CTeVersao.v400 && InfoCTe is CTeAnulacao;

    [DFeCollection("infCteComp", Ordem = 124, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public DFeCollection<CTeComplemento> Complementos { get; set; } = [];
    internal bool ShouldSerializeComplementos() => Versao >= CTeVersao.v400 && Complementos.Count > 0;

    // Mesmo nome XML, selecionado pelo modelo já lido em ide.mod.
    [DFeElement("infCTeNorm", Ordem = 121, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    private CTeNormalOS InfoCTeOS
    {
        get => InfoCTe as CTeNormalOS;
        set { if (Ide.Mod == ModeloCTe.CTeOS) InfoCTe = value; }
    }
    internal bool ShouldSerializeInfoCTeOS()
    {
        if (Ide.Mod != ModeloCTe.CTeOS || !(InfoCTe is CTeNormalOS os)) return false;
        os.Versao = Versao;
        if (os.InfModal != null) os.InfModal.VersaoModal = Versao;
        return true;
    }

    [DFeCollection("autXML", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 130)]
    public DFeCollection<CTeAutXML> AutXml { get; set; }

    [DFeElement("infRespTec", Ocorrencia = Ocorrencia.NaoObrigatoria, Ordem = 140)]
    public CTeInfRespTec InfRespTec { get; set; }

    #endregion Propriedades

    [DFeElement("toma", Ordem = 80, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeOSToma Toma { get; set; }

    [DFeElement("pgtoVinc", Ordem = 110, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTePagamentoVinculado PgtoVinc { get; set; }

    [DFeElement("infSolicNFF", Ordem = 150, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeSolicitacaoNFF InfSolicNFF { get; set; }

    [DFeElement("infPAA", Ordem = 160, Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeProvedorAssinatura InfPAA { get; set; }

    #region Methods

    internal bool ShouldSerializeCompl()
    {
        if (Compl == null) return false;
        return !Compl.XCaracAd.IsEmpty() ||
               !Compl.XCaracSer.IsEmpty() ||
               !Compl.XEmi.IsEmpty() ||
               !Compl.OrigCalc.IsEmpty() ||
               !Compl.DestCalc.IsEmpty() ||
               !Compl.XObs.IsEmpty() ||
               Compl.ShouldSerializeFluxo() ||
               Compl.ShouldSerializeEntrega() ||
               Compl.ObsCont.Any() ||
               Compl.ObsFisco.Any();
    }

    internal bool ShouldSerializeRem()
    {
        return Ide.Mod == ModeloCTe.CTe && (!Rem.CNPJ.IsEmpty() || !Rem.CPF.IsEmpty() || !Rem.XNome.IsEmpty());
    }

    internal bool ShouldSerializeExped()
    {
        return Ide.Mod == ModeloCTe.CTe && (!Exped.CNPJ.IsEmpty() || !Exped.CPF.IsEmpty() || !Exped.XNome.IsEmpty());
    }

    internal bool ShouldSerializeReceb()
    {
        return Ide.Mod == ModeloCTe.CTe && (!Receb.CNPJ.IsEmpty() || !Receb.CPF.IsEmpty() || !Receb.XNome.IsEmpty());
    }

    internal bool ShouldSerializeDest()
    {
        return Ide.Mod == ModeloCTe.CTe && (!Dest.CNPJ.IsEmpty() || !Dest.CPF.IsEmpty() || !Dest.XNome.IsEmpty());
    }

    internal bool ShouldSerializeInfRespTec() => InfRespTec != null && (!string.IsNullOrEmpty(InfRespTec.CNPJ) || !string.IsNullOrEmpty(InfRespTec.XNome));

    internal bool ShouldSerializeInfoCTe()
    {
        if (InfoCTe is CTeNormal normal)
        {
            normal.Versao = Versao;
            if (normal.InfModal != null) normal.InfModal.VersaoModal = Versao;
        }
        return Ide.Mod == ModeloCTe.CTe;
    }

    internal bool ShouldSerializeToma() => Ide.Mod == ModeloCTe.CTeOS;
    internal bool ShouldSerializePgtoVinc() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeInfSolicNFF() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeInfPAA() => Versao >= CTeVersao.v400;
    internal bool ShouldSerializeEmit() { if (Emit != null) Emit.Versao = Versao; return Emit != null; }
    internal bool ShouldSerializeImp() { if (Imp != null) Imp.Versao = Versao; return Imp != null; }

    #endregion Methods
}
