// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-12-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-22-2018
// ***********************************************************************
// <copyright file="CteProc.cs" company="OpenAC .Net">
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
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Document;
using OpenAC.Net.DFe.Core.Serializer;

namespace OpenAC.Net.CTe;

[DFeRoot(Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed class CTeProc : DFeDocument<CTeProc>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTeProc()
    {
        CTe = new CTe();
        ProtCTe = new CTeProtCTe();

        Versao = CTeVersao.v300;
    }

    #endregion Constructors

    #region Propriedades

    [DFeAttribute(TipoCampo.Enum, "versao", Min = 4, Max = 4, Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeVersao Versao { get; set; }

    [DFeAttribute(TipoCampo.Str, "ipTransmissor", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public string IpTransmissor { get; set; }

    [DFeElement("CTe", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTe CTe { get; set; }

    [DFeElement("CTeOS", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeOS CTeOS { get; set; }

    [DFeElement("protCTe", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeProtCTe ProtCTe { get; set; }

    [DFeIgnore]
    public bool Processado => ProtCTe?.InfProt?.CStat.IsIn(100, 110, 150, 301, 302) ?? false;

    #endregion Propriedades

    #region Methods

    internal bool ShouldSerializeCTe() => CTeOS == null && CTe != null;
    internal bool ShouldSerializeCTeOS() => CTeOS != null;

    private string GetRootName()
    {
        return CTeOS != null || CTe?.InfCTe?.Ide.Mod == ModeloCTe.CTeOS ? "cteOSProc" : "cteProc";
    }

    private static string[] GetRootNames()
    {
        return ["cteProc", "cteOSProc"];
    }

    #endregion Methods
    public override System.Xml.Linq.XElement WriteToXml(string rootName = null, string rootNamespace = null, SerializerOptions options = null)
    {
        System.Xml.Linq.XNamespace ns = rootNamespace ?? "http://www.portalfiscal.inf.br/cte";
        var root = new System.Xml.Linq.XElement(ns + (rootName ?? GetRootName()),
            new System.Xml.Linq.XAttribute("versao", Versao.GetDescription()));
        if (!string.IsNullOrEmpty(IpTransmissor)) root.SetAttributeValue("ipTransmissor", IpTransmissor);
        if (CTeOS != null) root.Add(CTeOS.WriteToXml("CTeOS", ns.NamespaceName, options));
        else if (CTe != null) root.Add(CTe.WriteToXml("CTe", ns.NamespaceName, options));
        if (ProtCTe != null) root.Add(ProtCTe.WriteToXml("protCTe", ns.NamespaceName, options));
        return root;
    }
    public override void ReadXml(System.Xml.Linq.XElement element, SerializerOptions options = null)
    {
        System.Xml.Linq.XNamespace ns = "http://www.portalfiscal.inf.br/cte";
        Versao = (string)element.Attribute("versao") switch { "2.00" => CTeVersao.v200, "4.00" => CTeVersao.v400, _ => CTeVersao.v300 };
        IpTransmissor = (string)element.Attribute("ipTransmissor");
        CTe = OpenAC.Net.CTe.CTe.ReadFromXml(element.Element(ns + "CTe"), options);
        CTeOS = OpenAC.Net.CTe.CTeOS.ReadFromXml(element.Element(ns + "CTeOS"), options);
        ProtCTe = CTeProtCTe.ReadFromXml(element.Element(ns + "protCTe"), options);
    }
    public static CTeProc ReadFromXml(System.Xml.Linq.XElement element, SerializerOptions options = null)
    {
        if (element == null) return null;
        var document = new CTeProc();
        document.ReadXml(element, options);
        return document;
    }
}