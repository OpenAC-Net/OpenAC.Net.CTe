// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 09-30-2018
//
// Last Modified By : RFTD
// Last Modified On : 10-16-2018
// ***********************************************************************
// <copyright file="DACTeBase.cs" company="OpenAC .Net">
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

using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

/// <summary>
/// Classe base para os componentes de impressão do OpenCTe.
/// </summary>
// ReSharper disable once InconsistentNaming
public abstract class DACTeBase : DFeOptionsBase<FiltroDFeReport>
{
    #region Fields

    private OpenCTe parent;

    #endregion Fields

    #region Constructors

    protected DACTeBase()
    {
        QuebrarLinhasObservacao = true;
        LayoutImpressao = DACTeLayout.Retrato;
    }

    #endregion Constructors

    #region Properties

    public OpenCTe Parent
    {
        get => parent;
        set
        {
            if (parent == value) return;

            var oldParent = parent;
            parent = value;
            ParentChanged(oldParent, parent);
        }
    }

    public bool QuebrarLinhasObservacao { get; set; }

    public DACTeLayout LayoutImpressao { get; set; }

    #endregion Properties

    #region Methods

    /// <summary>
    /// Imprime o DACTe do CTe.
    /// </summary>
    /// <param name="conhecimentos"></param>
    public abstract void Imprimir(CTeProc[] conhecimentos);

    /// <summary>
    /// Imprime o DACTe do CTe em PDF.
    /// </summary>
    /// <param name="conhecimentos"></param>
    public abstract void ImprimirPDF(CTeProc[] conhecimentos);

    /// <summary>
    /// Imprime um evento do CTe.
    /// </summary>
    /// <param name="eventos"></param>
    public abstract void ImprimirEvento(CTeProcEvento[] eventos);

    /// <summary>
    /// Imprime um evento do CTe em PDF.
    /// </summary>
    /// <param name="eventos"></param>
    public abstract void ImprimirEventoPDF(CTeProcEvento[] eventos);

    /// <summary>
    /// Imprime um evento de inutilizacao do CTe.
    /// </summary>
    /// <param name="inutilizao"></param>
    public abstract void ImprimirInutilizacao(InutilizaoResposta inutilizao);

    /// <summary>
    /// Imprime um evento de inutilizacao do CTe em PDF.
    /// </summary>
    /// <param name="inutilizao"></param>
    public abstract void ImprimirInutilizacaoPDF(InutilizaoResposta inutilizao);

    protected virtual void ParentChanged(OpenCTe oldParent, OpenCTe newParent)
    {
        if (oldParent != null && oldParent.DACTe == this) oldParent.DACTe = null;
        if (newParent != null && newParent.DACTe != this) newParent.DACTe = this;
    }

    #endregion Methods
}