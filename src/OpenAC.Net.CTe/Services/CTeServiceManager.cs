using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Services;

/// <summary>Consulta o catálogo CT-e 4.00 e valida ambiente, UF, serviço e modalidade de emissão.</summary>
public static class CTeServiceManager
{
    private static readonly HashSet<DFeSiglaUF> Estados =
    [

        .. "AC AL AP AM BA CE DF ES GO MA MT MS MG PA PB PR PE PI RJ RN RS RO RR SC SP SE TO"
            .Split(' ').Select(value => (DFeSiglaUF)Enum.Parse(typeof(DFeSiglaUF), value))
    ];

    private static readonly HashSet<DFeSiglaUF> AtendidosSvcRs =
        [DFeSiglaUF.AP, DFeSiglaUF.PE, DFeSiglaUF.RR, DFeSiglaUF.MT, DFeSiglaUF.MS, DFeSiglaUF.SP];

    private static readonly HashSet<DFeSiglaUF> AutorizadoresProprios =
        [DFeSiglaUF.MT, DFeSiglaUF.MS, DFeSiglaUF.MG, DFeSiglaUF.PR, DFeSiglaUF.SP];

    internal static DFeServices<TipoServicoCTe> Padrao { get; } = CarregarPadrao();

    private static DateTime DataAtual => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).Date;

    private static DFeServices<TipoServicoCTe> CarregarPadrao()
    {
        using var stream =
            typeof(CTeServiceManager).Assembly.GetManifestResourceStream("OpenAC.Net.CTe.Resources.services.xml");
        return LerCatalogo(stream);
    }

    /// <summary>Carrega um catálogo exclusivo do CT-e 4.00, rejeitando versões e serviços descontinuados.</summary>
    public static DFeServices<TipoServicoCTe> CarregarCatalogo(Stream stream) => LerCatalogo(stream);

    internal static DFeServices<TipoServicoCTe> LerCatalogo(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var document = XDocument.Load(stream);
        XNamespace ns = "https://www.openac.net.br/";
        if (document.Root?.Name != ns + "DFeServices")
            throw new ArgumentException("Raiz do catálogo DFeServices inválida.");

        var profiles = new HashSet<string>();
        if (document.Root == null)
            throw new ArgumentException("Catálogo de serviços vazio.");

        foreach (var group in document.Root.Elements())
        {
            if (group.Name != ns + "Services" || (string)group.Attribute("Tipo") != "CTe" ||
                (string)group.Attribute("Versao") != "4.00")
                throw new NotSupportedException("O catálogo de comunicação aceita somente serviços CT-e 4.00.");
            
            var profile = (string)group.Attribute("TipoEmissao");
            if (profile is not ("1" or "7" or "8") || !profiles.Add(profile))
                throw new ArgumentException(
                    "Perfil de autorização inválido ou duplicado no catálogo. Use 1, 7 ou 8.");
            
            var environments = new HashSet<string>();
            foreach (var environment in group.Elements())
            {
                var type = (string)environment.Attribute("Tipo");
                var state = (string)environment.Attribute("UF");
                if (environment.Name != ns + "Ambiente" || type is not ("1" or "2") ||
                    !Enum.TryParse(state, out DFeSiglaUF uf) || !Estados.Contains(uf) ||
                    !environments.Add(type + ":" + state))
                    throw new ArgumentException("Ambiente/UF inválido ou duplicado no catálogo.");
                if (profile == "7" && !AtendidosSvcRs.Contains(uf) || profile == "8" && AtendidosSvcRs.Contains(uf))
                    throw new ArgumentException($"A UF {uf} não é atendida pela SVC indicada no catálogo.");
                var addresses = environment.Elements(ns + "Enderecos").ToArray();
                if (addresses.Length != 1 || environment.Elements().Count() != 1)
                    throw new ArgumentException("Cada ambiente deve conter um único grupo Enderecos.");
                var services = new HashSet<TipoServicoCTe>();
                foreach (var address in addresses[0].Elements())
                {
                    var name = (string)address.Attribute("Tipo");
                    if (address.Name != ns + "Endereco" || !Enum.TryParse(name, out TipoServicoCTe service) ||
                        !Enum.IsDefined(typeof(TipoServicoCTe), service) || name != service.ToString() ||
                        !services.Add(service))
                        throw new NotSupportedException(
                            $"Serviço inválido, descontinuado ou duplicado no catálogo: {name}.");
                    if (profile != "1" && service is TipoServicoCTe.DistribuicaoDFe
                            or TipoServicoCTe.CTeConsultaCadastro or TipoServicoCTe.RecepcaoEventoEpec)
                        throw new ArgumentException(
                            "Distribuição, cadastro e EPEC são resolvidos pelo perfil normal.");
                    address.Value = NormalizarEndereco(service, address.Value);
                }
            }
        }

        return profiles.Count == 0
            ? throw new ArgumentException("Catálogo de serviços vazio.")
            : DFeServices<TipoServicoCTe>.Load(document.ToString(SaveOptions.DisableFormatting));
    }

    /// <summary>Valida a URL de uma substituição de endpoint ou de uma entrada do catálogo.</summary>
    public static void ValidarEndereco(TipoServicoCTe tipo, string endereco)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps && !(tipo == TipoServicoCTe.QRCode && uri.Scheme == Uri.UriSchemeHttp))
            throw new ArgumentException("Endpoint fiscal deve ser HTTPS; somente QR Code pode usar HTTP.",
                nameof(endereco));
    }

    /// <summary>Normaliza URLs de descrição WSDL para o endereço usado no POST SOAP.</summary>
    public static string NormalizarEndereco(TipoServicoCTe tipo, string endereco)
    {
        ValidarEndereco(tipo, endereco);
        var value = endereco.Trim();
        if (value.EndsWith("?wsdl", StringComparison.OrdinalIgnoreCase)) return value.Substring(0, value.Length - 5);
        return value.EndsWith("?wsdl=", StringComparison.OrdinalIgnoreCase)
            ? value.Substring(0, value.Length - 6)
            : value;
    }

    /// <summary>Consulta endereços conforme a vigência atual. A data opcional permite auditar a transição da NT 2026.004.</summary>
    public static string ObterEndereco(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo,
        DFeTipoEmissao tipoEmissao, DFeTipoAmbiente ambiente, DateTime? dataConsulta = null) =>
        ObterEndereco(Padrao, versao, uf, tipo, tipoEmissao, ambiente, dataConsulta);

    /// <summary>Determina o perfil de endereços: normal, SVC-RS ou SVC-SP.</summary>
    public static DFeTipoEmissao ObterPerfil(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo,
        DFeTipoEmissao emissao, DFeTipoAmbiente ambiente, DateTime? dataConsulta = null)
    {
        ValidarConsulta(versao, uf, tipo, emissao, ambiente, dataConsulta);
        if (tipo is TipoServicoCTe.DistribuicaoDFe or TipoServicoCTe.CTeConsultaCadastro
            or TipoServicoCTe.RecepcaoEventoEpec)
            return DFeTipoEmissao.Normal;
        return emissao is DFeTipoEmissao.SVCRS or DFeTipoEmissao.SVCSP ? emissao : DFeTipoEmissao.Normal;
    }

    /// <summary>Valida as combinações mesmo quando a configuração fornece uma URL personalizada.</summary>
    public static void ValidarConsulta(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo,
        DFeTipoEmissao emissao, DFeTipoAmbiente ambiente, DateTime? dataConsulta = null)
    {
        CTeUtils.ValidarVersaoComunicacao(versao);
        if (!Estados.Contains(uf)) throw new ArgumentOutOfRangeException(nameof(uf));
        if (ambiente is not (DFeTipoAmbiente.Producao or DFeTipoAmbiente.Homologacao))
            throw new ArgumentOutOfRangeException(nameof(ambiente));
        if (!Enum.IsDefined(typeof(TipoServicoCTe), tipo)) throw new ArgumentOutOfRangeException(nameof(tipo));
        if (!Enum.IsDefined(typeof(DFeTipoEmissao), emissao)) throw new ArgumentOutOfRangeException(nameof(emissao));
        if (tipo is TipoServicoCTe.DistribuicaoDFe or TipoServicoCTe.CTeConsultaCadastro) return;
        if (tipo == TipoServicoCTe.CTeRecepcaoOS && emissao == DFeTipoEmissao.DPEC)
            throw new NotSupportedException("CT-e OS não admite emissão EPEC.");
        var date = (dataConsulta ?? DataAtual).Date;
        var svcRs = AtendidosSvcRs.Contains(uf);
        var normalSvrs = !AutorizadoresProprios.Contains(uf) &&
                         uf is not (DFeSiglaUF.AP or DFeSiglaUF.PE or DFeSiglaUF.RR);
        var svrs = tipo == TipoServicoCTe.RecepcaoEventoEpec
            ? svcRs
            : emissao == DFeTipoEmissao.SVCRS || emissao != DFeTipoEmissao.SVCSP && normalSvrs;
        var migration = ambiente == DFeTipoAmbiente.Producao ? new DateTime(2026, 12, 14)
            : svrs ? new DateTime(2026, 10, 19) : new DateTime(2026, 10, 26);
        if (tipo == TipoServicoCTe.RecepcaoEventoEpec)
        {
            if (date >= migration)
                throw new NotSupportedException(
                    "Novos eventos EPEC foram desativados neste ambiente pela NT 2026.004.");
            return;
        }

        switch (emissao)
        {
            case DFeTipoEmissao.Normal:
            case DFeTipoEmissao.DPEC
                : // tpEmis=4 permanece para conciliação de EPEC já registrada, sem criar novos eventos.
                return;
            case DFeTipoEmissao.Contingencia: // No CT-e, o código 2 corresponde à contingência offline da NT 2026.004.
                if (date < migration)
                    throw new NotSupportedException(
                        $"Contingência offline disponível neste ambiente a partir de {migration:dd/MM/yyyy}.");
                return;
            case DFeTipoEmissao.FSDA:
                if (date >= migration)
                    throw new NotSupportedException("FS-DA foi desativada neste ambiente pela NT 2026.004.");
                return;
            case DFeTipoEmissao.SVCRS when svcRs:
            case DFeTipoEmissao.SVCSP when !svcRs:
                return;
            case DFeTipoEmissao.SVCRS:
            case DFeTipoEmissao.SVCSP:
                throw new NotSupportedException($"A UF {uf} deve utilizar {(svcRs ? "SVC-RS" : "SVC-SP")}.");
            default:
                throw new NotSupportedException($"Modalidade de emissão {emissao} não atendida pelo cliente CT-e.");
        }
    }

    internal static string ObterEndereco(DFeServices<TipoServicoCTe> catalogo, CTeVersao versao, DFeSiglaUF uf,
        TipoServicoCTe tipo, DFeTipoEmissao emissao, DFeTipoAmbiente ambiente, DateTime? dataConsulta = null)
    {
        var profile = ObterPerfil(versao, uf, tipo, emissao, ambiente, dataConsulta);
        var target = catalogo[profile, "4.00"]?[ambiente, uf];
        if (target == null || !target.Enderecos.TryGetValue(tipo, out var address))
            throw new NotSupportedException(
                $"Endereço oficial do serviço {tipo} não publicado para {uf}, {ambiente}, perfil {profile}, CT-e 4.00.");
        return address;
    }
}