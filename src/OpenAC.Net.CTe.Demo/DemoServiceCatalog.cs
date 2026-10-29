using System;
using System.IO;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe.Demo;

/// <summary>Catálogo editável da demo, sujeito à mesma validação de vigência e UF do cliente.</summary>
internal sealed class DemoServiceCatalog
{
    private DFeServices<TipoServicoCTe> services = new();

    public DemoServiceCatalog() => Load();

    public string ObterEndereco(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo,
        DFeTipoEmissao emissao, DFeTipoAmbiente ambiente)
    {
        var profile = CTeServiceManager.ObterPerfil(versao, uf, tipo, emissao, ambiente);
        var target = services[profile, "4.00"]?[ambiente, uf];
        if (target == null || !target.Enderecos.TryGetValue(tipo, out var address))
            throw new NotSupportedException($"Serviço {tipo} não disponível para {uf}, {ambiente}.");
        return address;
    }

    public void AplicarEndereco(CTeVersao versao, DFeSiglaUF uf, TipoServicoCTe tipo,
        DFeTipoEmissao emissao, DFeTipoAmbiente ambiente, string address)
    {
        var profile = CTeServiceManager.ObterPerfil(versao, uf, tipo, emissao, ambiente);
        var target = services[profile, "4.00"]?[ambiente, uf]
                     ?? throw new NotSupportedException("Perfil/UF não disponível no catálogo.");
        if (string.IsNullOrWhiteSpace(address))
            target.Enderecos.Remove(tipo);
        else
            target.Enderecos[tipo] = CTeServiceManager.NormalizarEndereco(tipo, address);
    }

    public void Load(string path = "")
    {
        using var stream = string.IsNullOrWhiteSpace(path)
            ? typeof(OpenCTe).Assembly.GetManifestResourceStream("OpenAC.Net.CTe.Resources.services.xml")
            : File.OpenRead(path);
        Load(stream ?? throw new InvalidOperationException("Catálogo não encontrado."));
    }

    public void Load(Stream stream) => services = CTeServiceManager.CarregarCatalogo(stream);

    public void Save(Stream stream) => services.Save(stream);
}