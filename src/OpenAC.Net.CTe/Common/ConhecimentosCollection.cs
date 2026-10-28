// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 10-15-2016
//
// Last Modified By : RFTD
// Last Modified On : 03-09-2018
// ***********************************************************************
// <copyright file="ConhecimentosCollection.cs" company="OpenAC .Net">
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

public sealed partial class ConhecimentosCollection
{
    #region Fields

    private DFeCollection<CTe> ctes;
    private DFeCollection<CTeOS> cteOs;

    #endregion Fields

    #region Constructors

    internal ConhecimentosCollection(OpenCTe parent)
    {
        Parent = parent;
        ctes = [];
        cteOs = [];
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Retorna a classe OpenCTe parente da coleção.
    /// </summary>
    public OpenCTe Parent { get; }

    public CTe[] CTe => [.. ctes];

    public CTeOS[] CTeOS => [.. cteOs];

    #endregion Properties

    #region Methods

    /// <summary>
    ///   Adiciona um objeto ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <returns><see cref="T:ACBr.Net.CTe.CTe" /></returns>
    public CTe AddNewCTe()
    {
        var instance = ctes.AddNew();
        instance.InfCTe.Versao = Parent.Configuracoes.Geral.VersaoDFe;
        instance.InfCTe.Ide.Mod = Parent.Configuracoes.Geral.Mod;

        return instance;
    }

    /// <summary>
    ///   Adiciona um objeto ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <returns><see cref="T:ACBr.Net.CTe.CTeOS" /></returns>
    public CTeOS AddNewCTeOS()
    {
        var instance = cteOs.AddNew();
        //instance.InfCTe.Versao = Parent.Configuracoes.Geral.VersaoDFe;
        //instance.InfCTe.Ide.Mod = Parent.Configuracoes.Geral.Mod;

        return instance;
    }

    /// <summary>
    ///   Adiciona um objeto ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser adicionado ao final do <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    public void Add(CTe item)
    {
        ctes.Add(item);
    }

    /// <summary>
    ///   Adiciona um objeto ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser adicionado ao final do <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    public void Add(CTeOS item)
    {
        cteOs.Add(item);
    }

    /// <summary>
    ///   Adiciona os elementos da coleção especificada ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="collection">
    ///   A coleção cujos elementos devem ser adicionados ao final do <see cref="T:System.Collections.Generic.List`1" />.
    ///    A coleção em si não pode ser <see langword="null" />, mas pode conter elementos que são <see langword="null" />.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///   <paramref name="collection" /> é <see langword="null" />.
    /// </exception>
    public void AddRange(IEnumerable<CTe> collection)
    {
        ctes.AddRange(collection);
    }

    /// <summary>
    ///   Adiciona os elementos da coleção especificada ao final do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="collection">
    ///   A coleção cujos elementos devem ser adicionados ao final do <see cref="T:System.Collections.Generic.List`1" />.
    ///    A coleção em si não pode ser <see langword="null" />, mas pode conter elementos que são <see langword="null" />.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///   <paramref name="collection" /> é <see langword="null" />.
    /// </exception>
    public void AddRange(IEnumerable<CTeOS> collection)
    {
        cteOs.AddRange(collection);
    }

    /// <summary>
    ///   Determina se um elemento está no <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser localizado no <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   <see langword="true" /> se <paramref name="item" /> for encontrado no <see cref="T:System.Collections.Generic.List`1" />; caso contrário, <see langword="false" />.
    /// </returns>
    public bool Contains(CTe item)
    {
        return ctes.Contains(item);
    }

    /// <summary>
    ///   Determina se um elemento está no <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser localizado no <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   <see langword="true" /> se <paramref name="item" /> for encontrado no <see cref="T:System.Collections.Generic.List`1" />; caso contrário, <see langword="false" />.
    /// </returns>
    public bool Contains(CTeOS item)
    {
        return cteOs.Contains(item);
    }

    /// <summary>
    ///   Pesquisa o objeto especificado e retorna o índice baseado em zero da primeira ocorrência dentro de todo o <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser localizado no <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   O índice baseado em zero da primeira ocorrência de <paramref name="item" /> em todo o <see cref="T:System.Collections.Generic.List`1" />, se encontrado; caso contrário, -1.
    /// </returns>
    public int IndexOf(CTe item)
    {
        return ctes.IndexOf(item);
    }

    /// <summary>
    ///   Pesquisa o objeto especificado e retorna o índice baseado em zero da primeira ocorrência dentro de todo o <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a ser localizado no <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   O índice baseado em zero da primeira ocorrência de <paramref name="item" /> em todo o <see cref="T:System.Collections.Generic.List`1" />, se encontrado; caso contrário, -1.
    /// </returns>
    public int IndexOf(CTeOS item)
    {
        return cteOs.IndexOf(item);
    }

    /// <summary>
    ///   Insere um elemento no <see cref="T:System.Collections.Generic.List`1" />, no índice especificado.
    /// </summary>
    /// <param name="index">
    ///   O índice de base zero no qual o <paramref name="item" /> deve ser inserido.
    /// </param>
    /// <param name="item">
    ///   O objeto a ser inserido.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="index" /> é menor que 0.
    ///
    ///   -ou-
    ///
    ///   <paramref name="index" /> é maior que <see cref="P:System.Collections.Generic.List`1.Count" />.
    /// </exception>
    public void Insert(int index, CTe item)
    {
        ctes.Insert(index, item);
    }

    /// <summary>
    ///   Insere um elemento no <see cref="T:System.Collections.Generic.List`1" />, no índice especificado.
    /// </summary>
    /// <param name="index">
    ///   O índice de base zero no qual o <paramref name="item" /> deve ser inserido.
    /// </param>
    /// <param name="item">
    ///   O objeto a ser inserido.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="index" /> é menor que 0.
    ///
    ///   -ou-
    ///
    ///   <paramref name="index" /> é maior que <see cref="P:System.Collections.Generic.List`1.Count" />.
    /// </exception>
    public void Insert(int index, CTeOS item)
    {
        cteOs.Insert(index, item);
    }

    /// <summary>
    ///   Remove a primeira ocorrência de um objeto específico do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a remover do <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   <see langword="true" /> se <paramref name="item" /> for removido com êxito; caso contrário, <see langword="false" />.
    ///     Esse método também retornará <see langword="false" /> se <paramref name="item" /> não tiver sido encontrado no <see cref="T:System.Collections.Generic.List`1" />.
    /// </returns>
    public bool Remove(CTe item)
    {
        return ctes.Remove(item);
    }

    /// <summary>
    ///   Remove a primeira ocorrência de um objeto específico do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="item">
    ///   O objeto a remover do <see cref="T:System.Collections.Generic.List`1" />.
    ///    O valor pode ser <see langword="null" /> para tipos de referência.
    /// </param>
    /// <returns>
    ///   <see langword="true" /> se <paramref name="item" /> for removido com êxito; caso contrário, <see langword="false" />.
    ///     Esse método também retornará <see langword="false" /> se <paramref name="item" /> não tiver sido encontrado no <see cref="T:System.Collections.Generic.List`1" />.
    /// </returns>
    public bool Remove(CTeOS item)
    {
        return cteOs.Remove(item);
    }

    /// <summary>
    ///   Remove o elemento no índice especificado do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="index">
    ///   O índice de base zero do elemento a ser removido.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="index" /> é menor que 0.
    ///
    ///   -ou-
    ///
    ///   <paramref name="index" /> é igual a ou maior que <see cref="P:System.Collections.Generic.List`1.Count" />.
    /// </exception>
    public void RemoveCTeAt(int index)
    {
        ctes.RemoveAt(index);
    }

    /// <summary>
    ///   Remove o elemento no índice especificado do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    /// <param name="index">
    ///   O índice de base zero do elemento a ser removido.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///   <paramref name="index" /> é menor que 0.
    ///
    ///   -ou-
    ///
    ///   <paramref name="index" /> é igual a ou maior que <see cref="P:System.Collections.Generic.List`1.Count" />.
    /// </exception>
    public void RemoveCTeOSAt(int index)
    {
        cteOs.RemoveAt(index);
    }

    /// <summary>
    ///   Remove todos os elementos do <see cref="T:System.Collections.Generic.List`1" />.
    /// </summary>
    public void Clear()
    {
        ctes.Clear();
        cteOs.Clear();
    }

    /// <summary>
    /// Carrega a CTe informada.
    /// </summary>
    /// <param name="path">Caminho da CTe.</param>
    public void Load(string path)
    {
        Guard.Against<ArgumentNullException>(path.IsEmpty(), nameof(path));

        var xml = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : path;
        LoadXml(xml);
    }

    /// <summary>
    /// Carrega a CTe informada.
    /// </summary>
    /// <param name="stream">Stream da CTe.</param>
    public void Load(Stream stream)
    {
        Guard.Against<ArgumentNullException>(stream == null, nameof(stream));
        Guard.Against<ArgumentException>(stream.Length == 0, "Stream vazio");

        using var reader = new StreamReader(stream);
        LoadXml(reader.ReadToEnd());
    }

    private void LoadXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) throw new ArgumentException("XML não informado.", nameof(xml));
        var root = System.Xml.Linq.XElement.Parse(xml);
        System.Xml.Linq.XNamespace ns = "http://www.portalfiscal.inf.br/cte";
        var document = root.Name == ns + "CTe" || root.Name == ns + "CTeOS" ? root
            : root.Element(ns + "CTe") ?? root.Element(ns + "CTeOS");
        if (document == null) throw new OpenDFeException("XML não contém CT-e ou CT-e OS.");
        if (document.Name == ns + "CTe") Add(OpenAC.Net.CTe.CTe.Load(document.ToString()));
        else Add(OpenAC.Net.CTe.CTeOS.Load(document.ToString()));
    }

    /// <summary>
    /// Assina as CTe não autorizadas.
    /// </summary>
    public void Assinar()
    {
        var cert = Parent.Configuracoes.Certificados.ObterCertificado();

        var saveOptions = DFeSaveOptions.DisableFormatting | DFeSaveOptions.OmitDeclaration;
        if (Parent.Configuracoes.Geral.RetirarAcentos) saveOptions |= DFeSaveOptions.RemoveAccents;
        if (Parent.Configuracoes.Geral.RetirarEspacos) saveOptions |= DFeSaveOptions.RemoveSpaces;

        try
        {
            Assinar(cert, saveOptions);
        }
        finally
        {
            cert.Reset();
        }
    }

    /// <summary>
    /// Assina as CTe não autorizadas.
    /// </summary>
    /// <param name="certificado">O certificado.</param>
    /// <param name="options"></param>
    public void Assinar(X509Certificate2 certificado, DFeSaveOptions options)
    {
        if (CTe.Any())
        {
            foreach (var cte in CTe)
            {
                cte.Assinar(certificado, options);
            }
        }

        if (CTeOS.Any())
        {
            foreach (var cte in CTeOS)
            {
                cte.Assinar(certificado, options);
            }
        }
    }

    /// <summary>
    /// Valida a CTe de acordo com o Schema.
    /// </summary>
    public void Validar()
    {
        foreach (var document in CTe)
            Services.CTeSchemaValidator.Validate(document.GetXml(), Parent.Configuracoes, false);
        foreach (var document in CTeOS)
            Services.CTeSchemaValidator.Validate(document.GetXml(), Parent.Configuracoes, true);
    }

    #endregion Methods
}