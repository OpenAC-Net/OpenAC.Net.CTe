using System;
using System.Collections.Generic;
using System.IO;
using OpenAC.Net.Core.Extensions;
using OpenAC.Net.DFe.Core.Common;

namespace OpenAC.Net.CTe.Configuracao;

public sealed partial class CTeConfigArquivos
{
    /// <summary>Desative ao utilizar um armazenamento externo. O store padrão cria seus próprios diretórios.</summary>
    public bool CriarDiretorios { get; set; } = true;

    protected override string GetPath(string aPath, string aLiteral, string cnpj = "", DateTime? data = null, string modeloDescr = "")
    {
        var path = string.IsNullOrEmpty(aPath) ? PathSalvar : aPath;
        var order = new List<TagOrdenacaoPath>(OrdenacaoPath);
        if (order.Count == 0)
        {
            if (SepararPorCNPJ) order.Add(TagOrdenacaoPath.CNPJ);
            if (SepararPorModelo) order.Add(TagOrdenacaoPath.Modelo);
            if (SepararPorAno || SepararPorMes || SepararPorDia) order.Add(TagOrdenacaoPath.Data);
            if (AdicionarLiteral) order.Add(TagOrdenacaoPath.Literal);
        }
        foreach (var item in order)
        {
            switch (item)
            {
                case TagOrdenacaoPath.CNPJ:
                    if (!string.IsNullOrEmpty(cnpj)) path = Path.Combine(path, cnpj.OnlyNumbers());
                    break;
                case TagOrdenacaoPath.Modelo:
                    if (!string.IsNullOrEmpty(modeloDescr)) path = Path.Combine(path, modeloDescr);
                    break;
                case TagOrdenacaoPath.Data:
                    var date = data ?? DateTime.Now;
                    if (SepararPorAno) path = Path.Combine(path, date.ToString("yyyy"));
                    if (SepararPorMes) path = Path.Combine(path, date.ToString("MM"));
                    if (SepararPorDia) path = Path.Combine(path, date.ToString("dd"));
                    break;
                case TagOrdenacaoPath.Literal:
                    if (!string.IsNullOrEmpty(aLiteral) && path.IndexOf(aLiteral, StringComparison.OrdinalIgnoreCase) < 0)
                        path = Path.Combine(path, aLiteral);
                    break;
                case TagOrdenacaoPath.Nenhum:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item));
            }
        }
        if (CriarDiretorios) Directory.CreateDirectory(path);
        return path;
    }
}
