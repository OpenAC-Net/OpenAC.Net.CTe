using OpenAC.Net.DFe.Core.Extensions;
using System;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.CTe.Storage;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Configuracao;

/// <summary>Configuração usada para construir um cliente. Alterações posteriores não afetam o cliente criado.</summary>
public sealed class CTeConfig : DFeConfigBase<CTeConfigGeral, CTeConfigWebServices, CTeConfigCertificados, CTeConfigArquivos>
{
    public CTeConfig()
    {
        HttpTransport = new DesktopCTeHttpTransport();
        DocumentStore = new LocalDocumentStore();
        Geral = new CTeConfigGeral();
        WebServices = new CTeConfigWebServices();
        Certificados = new CTeConfigCertificados();
        Arquivos = new CTeConfigArquivos(Geral) { Salvar = false, CriarDiretorios = false };
    }

    /// <summary>Copia as opções, os bytes do certificado e as coleções de endereços e ordenação.</summary>
    public CTeConfig Copiar()
    {
        var copia = new CTeConfig
        {
            HttpTransport = HttpTransport ?? throw new InvalidOperationException("Transporte HTTP não configurado."),
            DocumentStore = DocumentStore ?? throw new InvalidOperationException("Armazenamento não configurado.")
        };
        copia.Geral.VersaoDFe = Geral.VersaoDFe;
        copia.Geral.FormaEmissao = Geral.FormaEmissao;
        copia.Geral.Mod = Geral.Mod;
        copia.Geral.Salvar = Geral.Salvar;
        copia.Geral.ExibirErroSchema = Geral.ExibirErroSchema;
        copia.Geral.FormatoAlerta = Geral.FormatoAlerta;
        copia.Geral.RetirarAcentos = Geral.RetirarAcentos;
        copia.Geral.RetirarEspacos = Geral.RetirarEspacos;
        copia.Geral.IdentarXml = Geral.IdentarXml;
        copia.Geral.ValidarDigest = Geral.ValidarDigest;
        copia.WebServices.UF = WebServices.UF;
        copia.WebServices.TimeoutOperacao = WebServices.TimeoutOperacao;
        copia.WebServices.TimeoutTentativa = WebServices.TimeoutTentativa;
        copia.WebServices.CircuitBreakerHabilitado = WebServices.CircuitBreakerHabilitado;
        copia.WebServices.TempoVidaHandler = WebServices.TempoVidaHandler;
        copia.WebServices.TempoVidaConexaoPool = WebServices.TempoVidaConexaoPool;
        copia.WebServices.Salvar = WebServices.Salvar;
        copia.WebServices.Ambiente = WebServices.Ambiente;
        copia.WebServices.Tentativas = WebServices.Tentativas;
        copia.WebServices.IntervaloTentativas = WebServices.IntervaloTentativas;
        copia.WebServices.AjustaAguardaConsultaRet = WebServices.AjustaAguardaConsultaRet;
        copia.WebServices.AguardarConsultaRet = WebServices.AguardarConsultaRet;
        copia.WebServices.Protocolos = WebServices.Protocolos;
        copia.WebServices.Proxy = WebServices.Proxy;
        copia.WebServices.TimeOut = WebServices.TimeOut;
        copia.Arquivos.PathSalvar = Arquivos.PathSalvar;
        copia.Arquivos.PathSchemas = Arquivos.PathSchemas;
        copia.Arquivos.Salvar = Arquivos.Salvar;
        copia.Arquivos.AdicionarLiteral = Arquivos.AdicionarLiteral;
        copia.Arquivos.SepararPorCNPJ = Arquivos.SepararPorCNPJ;
        copia.Arquivos.SepararPorModelo = Arquivos.SepararPorModelo;
        copia.Arquivos.SepararPorAno = Arquivos.SepararPorAno;
        copia.Arquivos.SepararPorMes = Arquivos.SepararPorMes;
        copia.Arquivos.SepararPorDia = Arquivos.SepararPorDia;
        copia.Arquivos.PathCTe = Arquivos.PathCTe;
        copia.Arquivos.PathInu = Arquivos.PathInu;
        copia.Arquivos.PathEvento = Arquivos.PathEvento;
        copia.Arquivos.EmissaoPathCTe = Arquivos.EmissaoPathCTe;
        copia.Arquivos.SalvarApenasCTeProcessados = Arquivos.SalvarApenasCTeProcessados;
        copia.Arquivos.CriarDiretorios = Arquivos.CriarDiretorios;
        copia.Certificados.Certificado = Certificados.Certificado;
        copia.Certificados.Senha = Certificados.Senha;
        copia.Certificados.MargemValidadeCertificado = Certificados.MargemValidadeCertificado;
        copia.Certificados.CertificadoBytes = Certificados.CertificadoBytes == null ? null : (byte[])Certificados.CertificadoBytes.Clone();
        copia.Arquivos.OrdenacaoPath.AddRange(Arquivos.OrdenacaoPath);
        copia.Arquivos.CopiarCatalogo(Arquivos);
        foreach (var endpoint in WebServices.Enderecos) copia.WebServices.Enderecos.Add(endpoint.Key, endpoint.Value);
        return copia;
    }

    internal string ObterEndereco(TipoServicoCTe tipo, DFeCodUF? uf = null)
    {
        CTeServiceManager.ValidarConsulta(Geral.VersaoDFe, (uf ?? WebServices.UF).ToSiglaUF(), tipo, Geral.FormaEmissao, WebServices.Ambiente);
        if (WebServices.Enderecos.TryGetValue(tipo, out var endpoint))
        {
            return CTeServiceManager.NormalizarEndereco(tipo, endpoint);
        }
        return CTeServiceManager.ObterEndereco(Arquivos.Catalogo ?? CTeServiceManager.Padrao, Geral.VersaoDFe,
            (uf ?? WebServices.UF).ToSiglaUF(), tipo, Geral.FormaEmissao, WebServices.Ambiente);
    }
}

internal static class CTeCertificadoConfigCopy
{
    internal static CTeConfigCertificados Copiar(this CTeConfigCertificados origem) => new()
    {
        Certificado = origem.Certificado,
        Senha = origem.Senha,
        MargemValidadeCertificado = origem.MargemValidadeCertificado,
        CertificadoBytes = origem.CertificadoBytes == null ? null : (byte[])origem.CertificadoBytes.Clone()
    };
}
