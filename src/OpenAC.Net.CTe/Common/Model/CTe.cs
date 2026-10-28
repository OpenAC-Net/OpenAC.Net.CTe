using OpenAC.Net.DFe.Core.Extensions;
// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-12-2016
//
// Last Modified By : RFTD
// Last Modified On : 10-12-2016
// ***********************************************************************
// <copyright file="CTe.cs" company="OpenAC .Net">
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
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Attributes;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Document;

namespace OpenAC.Net.CTe;

[DFeSignInfoElement("infCte")]
[DFeRoot("CTe", Namespace = "http://www.portalfiscal.inf.br/cte")]
public sealed partial class CTe : DFeSignDocument<CTe>, INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion Events

    #region Constructors

    public CTe()
    {
        Signature = new DFeSignature();
        InfCTe = new CTeInfCTe();
    }

    #endregion Constructors

    #region Propriedades

    [DFeElement("infCte", Ocorrencia = Ocorrencia.Obrigatoria)]
    public CTeInfCTe InfCTe { get; set; }

    [DFeElement("infCTeSupl", Ocorrencia = Ocorrencia.NaoObrigatoria)]
    public CTeInfCteSupl InfCTeSupl { get; set; }

    [DFeIgnore]
    public bool Cancelada { get; set; }

    [DFeIgnore]
    public bool Assinado => ShouldSerializeSignature();

    #endregion Propriedades

    #region Methods

    /// <summary>
    /// Assina o CTe.
    /// </summary>
    /// <param name="certificado">The certificado.</param>
    /// <param name="saveOptions">The save options.</param>
    public void Assinar(X509Certificate2 certificado, DFeSaveOptions saveOptions)
    {
        Guard.Against<ArgumentNullException>(certificado == null, "Certificado não pode ser nulo.");

        if (InfCTe.Id.IsEmpty() || InfCTe.Id.Length < 44)
        {
            var chave = ChaveDFe.Gerar(InfCTe.Ide.CUF, InfCTe.Ide.DhEmi.DateTime,
                InfCTe.Emit.CNPJ, (int)InfCTe.Ide.Mod, InfCTe.Ide.Serie,
                InfCTe.Ide.NCT, InfCTe.Ide.TpEmis, InfCTe.Ide.CCT);

            InfCTe.Id = $"CTe{chave.Chave}";
            InfCTe.Ide.CDV = chave.Digito;
        }

        if (InfCTe.Versao == CTeVersao.v400 && string.IsNullOrEmpty(InfCTeSupl?.QrCode))
        {
            var address = Services.CTeServiceManager.GetServiceAndress(InfCTe.Versao, InfCTe.Ide.CUF.ToSiglaUF(),
                Services.TipoServicoCTe.QRCode, DFeTipoEmissao.Normal, InfCTe.Ide.TpAmb);
            InfCTeSupl = new CTeInfCteSupl { QrCode = address + (address.Contains("?") ? "&" : "?")
                + "chCTe=" + InfCTe.Id.Substring(3) + "&tpAmb=" + InfCTe.Ide.TpAmb.GetDFeValue() };
        }
        AssinarDocumento(certificado, saveOptions, false, SignDigest.SHA1);
    }

    /// <summary>
    /// Valida a assinatura da CTe.
    /// </summary>
    /// <param name="gerarXml"></param>
    /// <returns></returns>
    public bool ValidarAssinatura(bool gerarXml = true)
    {
        Guard.Against<OpenDFeException>(!Assinado, "Documento não esta assinado.");
        return XmlSigning.ValidarAssinatura(this, gerarXml);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public string GetXmlName()
    {
        return $"{InfCTe.Id.OnlyNumbers()}-cte.xml";
    }

    #endregion Methods
    internal bool ShouldSerializeInfCTeSupl() => !string.IsNullOrEmpty(InfCTeSupl?.QrCode);
    protected override bool ShouldSerializeSignature() => Signature != null && base.ShouldSerializeSignature();
}