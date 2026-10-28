using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OpenAC.Net.CTe.Configuracao;
using OpenAC.Net.DFe.Core;

namespace OpenAC.Net.CTe.Services;

internal static class CTeSchemaValidator
{
    internal static void Validate(string xml, CTeConfig config, bool os)
    {
        var errors = new List<string>();
        XmlSchemaValidation.ValidarXml(xml, config.Arquivos.GetSchema(os ? SchemaCTe.CTeOS : SchemaCTe.CTe), out var main, out _);
        errors.AddRange(main);
        XNamespace ns = "http://www.portalfiscal.inf.br/cte";
        var modal = XElement.Parse(xml).Element(ns + "infCte")?.Element(ns + "infCTeNorm")?.Element(ns + "infModal")?.Elements().FirstOrDefault();
        if (modal != null)
        {
            var schema = modal.Name.LocalName switch
            {
                "rodo" => SchemaCTe.CTeModalRodoviario, "rodoOS" => SchemaCTe.CTeModalRodoviarioOS,
                "aereo" => SchemaCTe.CTeModalAereo, "aquav" => SchemaCTe.CTeModalAquaviario,
                "ferrov" => SchemaCTe.CTeModalFerroviario, "duto" => SchemaCTe.CTeModalDutoviario,
                "multimodal" => SchemaCTe.CTeMultiModal,
                _ => throw new OpenDFeValidationException("Modal de transporte não reconhecido.")
            };
            XmlSchemaValidation.ValidarXml(modal.ToString(SaveOptions.DisableFormatting), config.Arquivos.GetSchema(schema), out var detail, out _);
            errors.AddRange(detail);
        }
        if (errors.Count > 0) throw new OpenDFeValidationException("Erros de validação do XML." + (config.Geral.ExibirErroSchema ? Environment.NewLine + string.Join(Environment.NewLine, errors) : ""));
    }
}
