using OpenAC.Net.DFe.Core.Extensions;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe;

/// <summary>Cliente assíncrono com configuração isolada e recursos locais a cada operação.</summary>
public sealed class OpenCTe : IOpenCTeClient
{
    private readonly CTeConfig configuracao;
    private readonly ICTeCertificadoProvider certificados;

    /// <summary>Captura a configuração. Transportes e stores injetados devem suportar chamadas concorrentes.</summary>
    public OpenCTe(CTeConfig configuracao, ICTeCertificadoProvider certificados = null)
    {
        this.configuracao = (configuracao ?? throw new ArgumentNullException(nameof(configuracao))).Copiar();
        this.certificados = certificados ?? new CTeCertificadoProvider();
    }

    public Task<CTeAutorizacaoResposta> AutorizarAsync(CTe documento, CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        if (documento == null) throw new ArgumentNullException(nameof(documento));
        CTeUtils.ValidarVersaoComunicacao(documento.InfCTe.Versao);
        return AutorizarDocumentoAsync(CTeDocumentCopy.Copiar(documento), null, cancellationToken);
    }

    public Task<CTeAutorizacaoResposta> AutorizarAsync(CTeOS documento, CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        if (documento == null) throw new ArgumentNullException(nameof(documento));
        CTeUtils.ValidarVersaoComunicacao(documento.InfCTe.Versao);
        CTeUtils.ValidarVersaoComunicacao(documento.Versao);
        return AutorizarDocumentoAsync(null, CTeDocumentCopy.Copiar(documento), cancellationToken);
    }

    private async Task<CTeAutorizacaoResposta> AutorizarDocumentoAsync(CTe cte, CTeOS os, CancellationToken token)
    {
        using var certificate = await ObterCertificadoAsync(token).ConfigureAwait(false);
        return await CTeAutorizador.AutorizarAsync(configuracao, cte, os, certificate, token).ConfigureAwait(false);
    }

    public async Task<ConsultaCTeResposta> ConsultarAsync(string chave, CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        var key = ValidarChave(chave);
        using var certificate = await ObterCertificadoAsync(cancellationToken).ConfigureAwait(false);
        using var service = new CTeConsultaServiceClient(configuracao, certificate);
        return await service.ConsultaAsync(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ConsultaStatusResposta> ConsultarSituacaoServicoAsync(
        CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        using var certificate = await ObterCertificadoAsync(cancellationToken).ConfigureAwait(false);
        using var service = new CTeStatusServicoServiceClient(configuracao, certificate);
        return await service.StatusServicoAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecepcaoEventoResposta> EnviarEventoAsync(CTeEventoSolicitacao solicitacao,
        CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        if (solicitacao == null) throw new ArgumentNullException(nameof(solicitacao));
        var key = ValidarChave(solicitacao.Chave);
        var cnpj = solicitacao.CnpjAutor?.OnlyNumbers();
        if (string.IsNullOrWhiteSpace(cnpj) || !cnpj.IsCNPJ())
            throw new ArgumentException("CNPJ do autor inválido.", nameof(solicitacao));
        if (solicitacao.Sequencia is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(solicitacao), "Sequência do evento deve estar entre 1 e 99.");
        var sequence = solicitacao.Sequencia;
        var evento = CTeDocumentCopy.Copiar(solicitacao.Evento ??
                                            throw new ArgumentException("Evento não informado.", nameof(solicitacao)));
        var tipo = evento is CTeEvEPEC ? TipoServicoCTe.RecepcaoEventoEpec : TipoServicoCTe.RecepcaoEvento;
        using var certificate = await ObterCertificadoAsync(cancellationToken).ConfigureAwait(false);
        using var service = new CTeRecepcaoEventoServiceClient(configuracao, tipo, certificate);
        return await service.RecepcaoEventoAsync(sequence, key, cnpj, evento, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DFeConsultaCadastroResposta> ConsultaCadastroAsync(DFeCodUF uf, string cpfCNPJ = "",
        string ie = "", CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        using var certificate = await ObterCertificadoAsync(cancellationToken).ConfigureAwait(false);
        using var service = new ConsultaCadastroServiceClient(configuracao, uf, certificate);
        return await service.ConsultaCadastroAsync(uf.ToSiglaUF(), uf, cpfCNPJ, ie, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DistribuicaoDFeResponse> ConsultarDistribuicaoAsync(CTeDistribuicaoSolicitacao solicitacao,
        CancellationToken cancellationToken = default)
    {
        ValidarOperacao(cancellationToken);
        if (solicitacao == null) throw new ArgumentNullException(nameof(solicitacao));
        var uf = solicitacao.UF;
        var taxpayer = solicitacao.CpfCnpj?.OnlyNumbers();
        if (string.IsNullOrWhiteSpace(taxpayer) || !taxpayer.IsCPF() && !taxpayer.IsCNPJ())
            throw new ArgumentException("CPF/CNPJ inválido.", nameof(solicitacao));
        var last = solicitacao.UltimoNsu;
        var nsu = solicitacao.Nsu;
        if (new[] { last, nsu }.Count(value => !string.IsNullOrWhiteSpace(value)) != 1)
            throw new ArgumentException("Informe exatamente um filtro de distribuição: último NSU ou NSU específico.",
                nameof(solicitacao));
        ValidarNsu(last);
        ValidarNsu(nsu);
        using var certificate = await ObterCertificadoAsync(cancellationToken).ConfigureAwait(false);
        using var service = new DistribuicaoDFeServiceClient(configuracao, uf, certificate);
        return await service.ConsultaDFeAsync(taxpayer, nsu ?? "", last ?? "", cancellationToken).ConfigureAwait(false);
    }

    private async Task<X509Certificate2> ObterCertificadoAsync(
        CancellationToken token)
    {
        var certificate =
            await certificados.ObterAsync(configuracao.Certificados.Copiar(), token).ConfigureAwait(false);
        try
        {
            token.ThrowIfCancellationRequested();
            return certificate is not { HasPrivateKey: true }
                ? throw new InvalidOperationException("Certificado com chave privada não informado.")
                : certificate;
        }
        catch
        {
            certificate?.Dispose();
            throw;
        }
    }

    private void ValidarOperacao(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(configuracao.Geral.VersaoDFe);
    }

    private static string ValidarChave(string chave)
    {
        var key = chave?.Trim();
        if (key is not { Length: 44 } || key.Any(c => c is < '0' or > '9'))
            throw new ArgumentException("A chave deve conter 44 dígitos.", nameof(chave));
        return key;
    }

    private static void ValidarNsu(string nsu)
    {
        if (!string.IsNullOrEmpty(nsu) && (nsu.Length > 15 || nsu.Any(c => c is < '0' or > '9')))
            throw new ArgumentException("O NSU deve conter até 15 dígitos.", nameof(nsu));
    }
}