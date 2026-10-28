using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenAC.Net.CTe.Services;

/// <summary>
/// Mantém clientes HTTP reutilizáveis para aplicações desktop, isolados por certificado e protocolo TLS.
/// </summary>
internal sealed class DesktopCTeHttpClientPool
{
    private static readonly TimeSpan TempoVidaCliente = TimeSpan.FromMinutes(10);
    private readonly object syncRoot = new();
    private readonly Dictionary<string, EntradaCliente> clientes = new();

    /// <summary>Instância compartilhada do pool desktop.</summary>
    public static DesktopCTeHttpClientPool Instancia { get; } = new();

    private DesktopCTeHttpClientPool()
    {
    }

    /// <summary>Obtém um cliente associado ao certificado e aos protocolos informados.</summary>
    public EmprestimoCliente ObterCliente(X509Certificate2 certificado, SslProtocols protocolos)
    {
        var chave = CriarChave(certificado, protocolos);
        var agora = DateTimeOffset.UtcNow;

        lock (syncRoot)
        {
            RemoverClientesExpirados(agora);

            if (!clientes.TryGetValue(chave, out var entrada))
            {
                entrada = CriarEntrada(certificado, protocolos, agora);
                clientes.Add(chave, entrada);
            }

            entrada.RequisicoesAtivas++;
            return new EmprestimoCliente(this, entrada);
        }
    }

    private static EntradaCliente CriarEntrada(X509Certificate2 certificado, SslProtocols protocolos,
        DateTimeOffset agora)
    {
        var ownedCertificate = new X509Certificate2(certificado);
        var handler = new HttpClientHandler { SslProtocols = protocolos, AllowAutoRedirect = false, UseCookies = false };
        handler.ClientCertificates.Add(ownedCertificate);

        var expiracaoCertificado = new DateTimeOffset(certificado.NotAfter.ToUniversalTime());
        var expiracao = agora.Add(TempoVidaCliente);
        if (expiracaoCertificado < expiracao)
            expiracao = expiracaoCertificado;

        return new EntradaCliente(new HttpClient(handler, true) { Timeout = System.Threading.Timeout.InfiniteTimeSpan }, ownedCertificate, expiracao);
    }

    private void RemoverClientesExpirados(DateTimeOffset agora)
    {
        var chavesExpiradas = new List<string>();
        foreach (var item in clientes)
        {
            if (item.Value.Expiracao > agora) continue;
            item.Value.Removido = true;
            chavesExpiradas.Add(item.Key);
            if (item.Value.RequisicoesAtivas == 0)
                item.Value.Dispose();
        }

        foreach (var chave in chavesExpiradas)
            clientes.Remove(chave);
    }

    private void Devolver(EntradaCliente entrada)
    {
        lock (syncRoot)
        {
            entrada.RequisicoesAtivas--;
            if (entrada is { Removido: true, RequisicoesAtivas: 0 })
                entrada.Dispose();
        }
    }

    private static string CriarChave(X509Certificate2 certificado, SslProtocols protocolos)
    {
        var identidade = certificado.Thumbprint;
        if (!string.IsNullOrWhiteSpace(identidade)) return $"{identidade}:{(int)protocolos}";

        using var sha256 = SHA256.Create();
        identidade = Convert.ToBase64String(sha256.ComputeHash(certificado.RawData));

        return $"{identidade}:{(int)protocolos}";
    }

    internal sealed class EntradaCliente(HttpClient http, X509Certificate2 certificate, DateTimeOffset expiracao) : IDisposable
    {
        public HttpClient Http { get; } = http;
        public DateTimeOffset Expiracao { get; } = expiracao;
        public int RequisicoesAtivas { get; set; }
        public bool Removido { get; set; }
        public void Dispose() { Http.Dispose(); certificate.Dispose(); }
    }

    /// <summary>Representa o uso temporário de um cliente mantido pelo pool.</summary>
    internal sealed class EmprestimoCliente : IDisposable
    {
        private readonly DesktopCTeHttpClientPool pool;
        private readonly EntradaCliente entrada;
        private bool devolvido;

        internal EmprestimoCliente(DesktopCTeHttpClientPool pool, EntradaCliente entrada)
        {
            this.pool = pool;
            this.entrada = entrada;
        }

        public HttpClient Http => entrada.Http;

        public void Dispose()
        {
            if (devolvido) return;
            devolvido = true;
            pool.Devolver(entrada);
        }
    }
}
