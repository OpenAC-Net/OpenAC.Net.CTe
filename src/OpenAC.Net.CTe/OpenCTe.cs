// ***********************************************************************
// Assembly         : OpenAC.Net.CTe
// Author           : RFTD
// Created          : 07-23-2016
//
// Last Modified By : RFTD
// Last Modified On : 07-23-2016
// ***********************************************************************
// <copyright file="OpenCTe.cs" company="OpenAC .Net">
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
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.CTe.Eventos;
using OpenAC.Net.CTe.Services;
using OpenAC.Net.DFe.Core;
using OpenAC.Net.DFe.Core.Common;
using OpenAC.Net.DFe.Core.Extensions;
using OpenAC.Net.DFe.Core.Service;

namespace OpenAC.Net.CTe;

public sealed partial class OpenCTe : OpenDisposable, IOpenLog
{
    #region Fields

    private StatusCTe status;
    private DACTeBase dacTe;

    #endregion Fields

    #region Events

    /// <summary>
    /// Evento disparado quando o status do componente muda.
    /// </summary>
    public event EventHandler<EventArgs> StatusChanged;

    #endregion Events

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instancia da classe <see cref="OpenCTe"/>.
    /// </summary>
    public OpenCTe() : this(new CTeConfig()) { }

    public OpenCTe(CTeConfig configuracoes)
    {
        status = StatusCTe.EmEspera;
        Configuracoes = configuracoes ?? throw new ArgumentNullException(nameof(configuracoes));
        Conhecimentos = new ConhecimentosCollection(this);
    }

    #endregion Constructors

    #region Propriedades

    /// <summary>
    /// Retorna as configurações do OpenCTe.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public CTeConfig Configuracoes { get; private set; }

    /// <summary>
    /// Retorna a lista de CTes para processamento.
    /// </summary>
    [Browsable(false)]
    public ConhecimentosCollection Conhecimentos { get; private set; }

    /// <summary>
    /// Retorna a situação do componente.
    /// </summary>
    public StatusCTe Status
    {
        get => status;
        internal set
        {
            if (status == value) return;

            status = value;
            StatusChanged.Raise(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Componente para impressão do DACTe
    /// </summary>
    public DACTeBase DACTe
    {
        get => dacTe;
        set
        {
            dacTe = value;
            if (dacTe != null && dacTe.Parent != this) dacTe.Parent = this;
        }
    }

    #endregion Propriedades

    #region Methods

    /// <summary>
    /// Metodo para enviar a(s) CTe(d) carregada(s) na coleção.
    /// </summary>
    /// <param name="lote">Número do lote.</param>
    /// <param name="imprimir">Verdadeiro para imprimir o(s) CTe(s) retornado(s) com sucesso.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns></returns>
    public Task<EnviarCTeResposta> EnviarAsync(int lote, bool imprimir = true, CancellationToken cancellationToken = default)
        => EnviarAsync(lote.ToString(), imprimir, cancellationToken);

    /// <summary>
    /// Metodo para enviar a(s) CTe(d) carregada(s) na coleção.
    /// </summary>
    /// <param name="lote">Número do lote.</param>
    /// <param name="imprimir">Verdadeiro para imprimir o(s) CTe(s) retornado(s) com sucesso.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns></returns>
    public async Task<EnviarCTeResposta> EnviarAsync(string lote, bool imprimir = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        return await EnviarSincronoAsync(imprimir, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Consulta o recibo da CTe para saber se o mesmo foi processado.
    /// </summary>
    /// <param name="recibo"></param>
    /// <param name="cancellationToken">Token para cancelar a consulta e a espera entre tentativas.</param>
    /// <returns></returns>
    public Task<RetRecepcaoResposta> ConsultaReciboAsync(string recibo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        throw new NotSupportedException("A consulta de recibo foi descontinuada. CT-e 4.00 utiliza autorização síncrona.");
    }

    /// <summary>
    /// Metodo para checar a situação da CTe pela chave.
    /// </summary>
    /// <param name="chave">Chave de acesso ou vazio para consultar o primeiro CT-e carregado.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>A situação do serviço de CTe</returns>
    public async Task<ConsultaCTeResposta> ConsultarAsync(string chave = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        Guard.Against<ArgumentException>(chave.IsEmpty() && !Conhecimentos.CTe.Any(), "ERRO: Nenhum CT-e ou Chave Informada!");

        using var cert = Configuracoes.Certificados.ObterCertificado();

        try
        {
            Status = StatusCTe.Consulta;

            using var cliente = new CTeConsultaServiceClient(Configuracoes, cert);
            return await cliente.ConsultaAsync(!chave.IsEmpty() ? chave : Conhecimentos.CTe.First().InfCTe.Id.OnlyNumbers(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this.Log().Error("[Consultar]", exception);

            throw;
        }
        finally
        {
            Status = StatusCTe.EmEspera;
        }
    }

    /// <summary>
    /// Metodo para checar a situação do serviço de CTe.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>A situação do serviço de CTe</returns>
    public async Task<ConsultaStatusResposta> ConsultarSituacaoServicoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        using var cert = Configuracoes.Certificados.ObterCertificado();

        try
        {
            Status = StatusCTe.StatusServico;

            using var cliente = new CTeStatusServicoServiceClient(Configuracoes, cert);
            return await cliente.StatusServicoAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this.Log().Error("[ConsultarSituacaoServico]", exception);
            throw;
        }
        finally
        {
            Status = StatusCTe.EmEspera;
        }
    }

    /// <summary>
    /// Metodo para inutilizar uma faixa de numeração do CTe.
    /// </summary>
    /// <param name="cnpj"></param>
    /// <param name="aJustificativa"></param>
    /// <param name="ano"></param>
    /// <param name="modelo"></param>
    /// <param name="serie"></param>
    /// <param name="numeroInicial"></param>
    /// <param name="numeroFinal"></param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns></returns>
    public Task<InutilizaoResposta> InutilizacaoAsync(string cnpj, string aJustificativa, int ano, ModeloCTe modelo, int serie,
        int numeroInicial, int numeroFinal, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        throw new NotSupportedException("A inutilização de numeração foi descontinuada e não existe no CT-e 4.00.");
    }

    /// <summary>
    /// Metodo para enviar eventos da CTe.
    /// </summary>
    /// <param name="nSeqEvento"></param>
    /// <param name="chave"></param>
    /// <param name="cnpj"></param>
    /// <param name="evento"></param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns></returns>
    public async Task<RecepcaoEventoResposta> EnviarEventoAsync(int nSeqEvento, string chave, string cnpj, IEventoCTe evento, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        using var cert = Configuracoes.Certificados.ObterCertificado();

        try
        {
            TipoServicoCTe service;
            switch (evento)
            {
                case CTeEvCancCTe _:
                    service = TipoServicoCTe.RecepcaoEvento;
                    Status = StatusCTe.Cancelamento;
                    break;

                case CTeEvCCeCTe _:
                    service = TipoServicoCTe.RecepcaoEvento;
                    Status = StatusCTe.CCe;
                    break;

                case CTeEvEPEC _:
                    service = TipoServicoCTe.RecepcaoEventoAN;
                    Status = StatusCTe.Evento;
                    break;

                default:
                    service = TipoServicoCTe.RecepcaoEvento;
                    Status = StatusCTe.Evento;
                    break;
            }

            using var cliente = new CTeRecepcaoEventoServiceClient(Configuracoes, service, cert);
            return await cliente.RecepcaoEventoAsync(nSeqEvento, chave.OnlyNumbers(), cnpj.OnlyNumbers(), evento, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this.Log().Error("[EnviarEvento]", exception);
            throw;
        }
        finally
        {
            Status = StatusCTe.EmEspera;
        }
    }

    /// <summary>
    /// Consulta Cadastro do contribuinte na SEFAZ.
    /// </summary>
    /// <param name="uf">UF do cadastro consultado.</param>
    /// <param name="cpfCNPJ">CPF ou CNPJ do contribuinte.</param>
    /// <param name="ie">Inscrição estadual do contribuinte.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns></returns>
    public async Task<DFeConsultaCadastroResposta> ConsultaCadastroAsync(DFeCodUF uf, string cpfCNPJ = "", string ie = "", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CTeUtils.ValidarVersaoComunicacao(Configuracoes.Geral.VersaoDFe);
        using var cert = Configuracoes.Certificados.ObterCertificado();

        try
        {
            Status = StatusCTe.Cadastro;

            using var cliente = new ConsultaCadastroServiceClient(Configuracoes, uf, cert);
            return await cliente.ConsultaCadastroAsync(uf.ToSiglaUF(), uf, cpfCNPJ, ie, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this.Log().Error("[ConsultaCadastro]", exception);
            throw;
        }
        finally
        {
            Status = StatusCTe.EmEspera;
        }
    }

    #endregion Methods

    #region Override

    /// <inheritdoc />
    protected override void DisposeManaged()
    {
    }

    #endregion Override
}
