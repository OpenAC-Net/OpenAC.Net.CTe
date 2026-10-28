// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 11-10-2016
//
// Last Modified By : RFTD
// Last Modified On : 06-22-2018
// ***********************************************************************
// <copyright file="CTeServiceManager.cs" company="OpenAC .Net">
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
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core.Collection;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Services;

/// <summary>
/// Classe responsavel pelo gerenciamento das url dos serviço da CTe.
/// </summary>
public static class CTeServiceManager
{
    private static DFeServices<TipoServicoCTe> servicosV400 = new();

    private static DFeServices<TipoServicoCTe> ReadCatalog(XDocument document, string version)
    {
        var selected = document.Root.Elements().Where(element => element.Name.LocalName == "Services"
            && ((string)element.Attribute("Versao") ?? "3.00") == version);
        var root = new XElement(document.Root.Name, document.Root.Attributes(), selected.Select(element => new XElement(element)));
        return DFeServices<TipoServicoCTe>.Load(root.ToString(SaveOptions.DisableFormatting));
    }

    #region Constructors

    static CTeServiceManager()
    {
        Servicos = new DFeServices<TipoServicoCTe>
        {
            Webservices =
            {
                new DFeServiceInfo<TipoServicoCTe>
                {
                    Tipo = DFeTipoServico.CTe,
                    TipoEmissao = DFeTipoEmissao.Normal
                },
                new DFeServiceInfo<TipoServicoCTe>
                {
                    Tipo = DFeTipoServico.CTe,
                    TipoEmissao = DFeTipoEmissao.DPEC
                },
                new DFeServiceInfo<TipoServicoCTe>
                {
                    Tipo = DFeTipoServico.CTe,
                    TipoEmissao = DFeTipoEmissao.SVCRS
                },
                new DFeServiceInfo<TipoServicoCTe>
                {
                    Tipo = DFeTipoServico.CTe,
                    TipoEmissao = DFeTipoEmissao.SVCSP
                }
            }
        };

        var dataSource = (from DFeSiglaUF value in Enum.GetValues(typeof(DFeSiglaUF))
            where !value.IsIn(DFeSiglaUF.EX, DFeSiglaUF.AN, DFeSiglaUF.SU)
            orderby value.GetDescription()
            select value).ToArray();

        var servicoCTes = (from TipoServicoCTe value in Enum.GetValues(typeof(TipoServicoCTe)) select value).ToArray();

        foreach (var serviceInfo in Servicos.Webservices)
        {
            foreach (var siglaUF in dataSource)
            {
                var ambiente = serviceInfo.Ambientes.AddNew();
                ambiente.UF = siglaUF;
                ambiente.Ambiente = DFeTipoAmbiente.Homologacao;

                foreach (var servicoCTe in servicoCTes)
                {
                    ambiente.Enderecos.Add(servicoCTe, "");
                }

                ambiente = serviceInfo.Ambientes.AddNew();
                ambiente.UF = siglaUF;
                ambiente.Ambiente = DFeTipoAmbiente.Producao;

                foreach (var servicoCTe in servicoCTes)
                {
                    ambiente.Enderecos.Add(servicoCTe, "");
                }
            }
        }

        Load();
    }

    #endregion Constructors

    #region Propriedades

    /// <summary>
    /// Lista de serviços CTe.
    /// </summary>
    public static DFeServices<TipoServicoCTe> Servicos { get; private set; }

    #endregion Propriedades

    #region Methods

    /// <summary>
    /// Retorna a url do serviço de acordo com os dados passados.
    /// </summary>
    /// <param name="versao">Versão da CTe</param>
    /// <param name="uf">UF do serviço</param>
    /// <param name="tipo">Tipo de serviço</param>
    /// <param name="tipoEmissao"></param>
    /// <param name="ambiente">Ambiente</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetServiceAndress(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo, DFeTipoEmissao tipoEmissao, DFeTipoAmbiente ambiente)
    {
        if (versao == CTeVersao.v400 && (tipo == TipoServicoCTe.CTeInutilizacao || tipo == TipoServicoCTe.CTeRetRecepcao))
            throw new NotSupportedException("A versão 4.00 não possui consulta de recibo nem inutilização.");

        switch (tipo)
        {
            case TipoServicoCTe.DistribuicaoDFe:
                switch (ambiente)
                {
                    case DFeTipoAmbiente.Homologacao: return @"https://hom1.cte.fazenda.gov.br/CTeDistribuicaoDFe/CTeDistribuicaoDFe.asmx";
                    default: return @"https://www1.cte.fazenda.gov.br/CTeDistribuicaoDFe/CTeDistribuicaoDFe.asmx";
                }

            default:
                var service = (versao == CTeVersao.v400 ? servicosV400 : Servicos)[tipoEmissao];
                Guard.Against<OpenException>(service == null, "Tipo de emissão não encontrado no arquivo de serviços.");
                var target = service[ambiente, uf];
                if (target == null || !target.Enderecos.TryGetValue(tipo, out var address) || string.IsNullOrWhiteSpace(address))
                    throw new OpenException($"Endereço do serviço {tipo} não encontrado para {uf}, {ambiente}, versão {versao.GetDescription()}.");
                return address;
        }
    }

    /// <summary>
    /// Salva o arquivo de serviços.
    /// </summary>
    /// <param name="path">Caminho para salvar o arquivo</param>
    public static void Save(string path = "services.cte")
    {
        Guard.Against<ArgumentNullException>(path == null, "Path invalido.");

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using var fileStream = new FileStream(path, FileMode.CreateNew);
        Save(fileStream);
    }

    /// <summary>
    /// Salva o arquivo de serviços.
    /// </summary>
    /// <param name="stream">O stream.</param>
    public static void Save(Stream stream)
    {
        XNamespace ns = "https://www.openac.net.br/";
        var root = new XElement(ns + "DFeServices");
        foreach (var pair in new[] { new { Version = "3.00", Catalog = Servicos }, new { Version = "4.00", Catalog = servicosV400 } })
        {
            foreach (var entry in pair.Catalog.WriteToXml().Elements())
            {
                entry.SetAttributeValue("Versao", pair.Version);
                root.Add(entry);
            }
        }
        new XDocument(root).Save(stream);
    }

    /// <summary>
    /// Carrega o arquivo de serviços.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="clean">if set to <c>true</c> [clean].</param>
    public static void Load(string path = "")
    {
        byte[] buffer = null;
        if (path.IsEmpty())
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("OpenAC.Net.CTe.Resources.services.cte");
            if (stream != null)
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }
        }
        else if (File.Exists(path))
        {
            buffer = File.ReadAllBytes(path);
        }

        Guard.Against<ArgumentException>(buffer == null, "Arquivo de serviços não encontrado");

        using (var stream = new MemoryStream(buffer))
        {
            Load(stream);
        }
    }

    /// <summary>
    /// Carrega o arquivo de serviços.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="clean">if set to <c>true</c> [clean].</param>
    public static void Load(Stream stream)
    {
        Guard.Against<ArgumentException>(stream == null, "Arquivo de serviços não encontrado");

        var document = XDocument.Load(stream);
        var legacy = ReadCatalog(document, "3.00");
        var modern = ReadCatalog(document, "4.00");
        Servicos = legacy;
        servicosV400 = modern;
    }

    /// <summary>
    /// Função para importar endereços no padrão de Ini do ACBr.
    /// </summary>
    /// <param name="path">Caminho para o ini</param>
    public static void ImportIniACBr(string path = "")
    {
        using var stream = File.OpenRead(path);
        ImportIniACBr(stream);
    }

    /// <summary>
    /// Função para importar endereços no padrão de Ini do ACBr.
    /// </summary>
    /// <param name="stream"></param>
    public static void ImportIniACBr(Stream stream)
    {
        var ini = OpenIniFile.Load(stream);

        var dataSource = (from DFeSiglaUF value in Enum.GetValues(typeof(DFeSiglaUF))
            where !value.IsIn(DFeSiglaUF.EX, DFeSiglaUF.AN, DFeSiglaUF.SU)
            orderby value.GetDescription()
            select value).ToArray();

        var servicoCTes =
            (from TipoServicoCTe value in Enum.GetValues(typeof(TipoServicoCTe)) select value).ToArray();

        var getVersion = new Func<CTeVersao, string>(versao =>
        {
            switch (versao)
            {
                case CTeVersao.v200: return "2.00";
                case CTeVersao.v300: return "3.00";
                default: throw new ArgumentOutOfRangeException(nameof(versao), versao, null);
            }
        });

        // Emissão Normal
        foreach (var codUf in dataSource)
        {
            var sectionName = $"CTe_{codUf}_H";

            var sessao = ini[sectionName];
            var sessaoUsar = ini[sectionName];

            while (sessaoUsar.ContainsKey("Usar"))
            {
                sessaoUsar = ini[sessaoUsar["Usar"]];
            }

            foreach (var service in servicoCTes)
            {
                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao.ContainsKey(key) ? sessao[key] : sessaoUsar.ContainsKey(key) ? sessaoUsar[key] : "";

                Servicos[DFeTipoEmissao.Normal][DFeTipoAmbiente.Homologacao, codUf].Enderecos[service] = url;
            }

            sectionName = $"CTe_{codUf.GetDescription()}_P";
            sessao = ini[sectionName];
            sessaoUsar = ini[sectionName];

            while (sessaoUsar.ContainsKey("Usar"))
            {
                sessaoUsar = ini[sessaoUsar["Usar"]];
            }

            foreach (var service in servicoCTes)
            {
                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao.ContainsKey(key) ? sessao[key] :
                    sessaoUsar.ContainsKey(key) ? sessaoUsar[key] : "";

                Servicos[DFeTipoEmissao.Normal][DFeTipoAmbiente.Producao, codUf]
                    .Enderecos[service] = url;
            }
        }

        //Emissão DPEC
        foreach (var codUf in dataSource)
        {
            var sectionName = string.Empty;

            switch (codUf)
            {
                case DFeSiglaUF.PE:
                case DFeSiglaUF.SP:
                case DFeSiglaUF.MS:
                case DFeSiglaUF.MT:
                    sectionName = "CTe_SVC-RS_";
                    break;

                default:
                    sectionName = "CTe_SVC-SP_";
                    break;
            }

            var sessao = ini[sectionName + "H"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.DPEC][DFeTipoAmbiente.Homologacao, codUf][service] = url;
            }

            sessao = ini[sectionName + "P"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.DPEC][DFeTipoAmbiente.Producao, codUf][service] = url;
            }
        }

        //Emissão SVCRS
        foreach (var codUf in dataSource)
        {
            var sectionName = "CTe_SVC-RS_";

            var sessao = ini[sectionName + "H"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.SVCRS][DFeTipoAmbiente.Homologacao, codUf][service] = url;
            }

            sessao = ini[sectionName + "P"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.SVCRS][DFeTipoAmbiente.Producao, codUf][service] = url;
            }
        }

        //Emissão SVCSP
        foreach (var codUf in dataSource)
        {
            var sectionName = "CTe_SVC-SP_";

            var sessao = ini[sectionName + "H"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.SVCSP][DFeTipoAmbiente.Homologacao, codUf][service] = url;
            }

            sessao = ini[sectionName + "P"];
            foreach (var service in servicoCTes)
            {
                if (service == TipoServicoCTe.RecepcaoEventoAN) continue;

                var key = $"{service.GetDescription()}_{getVersion(CTeVersao.v300)}";
                var url = sessao[key];

                Servicos[DFeTipoEmissao.SVCSP][DFeTipoAmbiente.Producao, codUf][service] = url;
            }
        }
    }

    #endregion Methods
}