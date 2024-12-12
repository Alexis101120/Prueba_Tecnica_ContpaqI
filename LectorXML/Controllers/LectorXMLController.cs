using LectorXML.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.Drawing;
using System.Text;
using System.Xml.Serialization;

namespace LectorXML.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LectorXMLController : ControllerBase
    {
        private readonly IWebHostEnvironment _host;
        public LectorXMLController(IWebHostEnvironment host)
        {
            _host = host;
        }

        [SwaggerOperation(
            Summary = "Procesa un XML codificado en Base64.",
            Description = "Este endpoint valida y procesa un XML codificado en Base64 y retorna el comprobante procesado, en caso de existir errores tambien los retornará"
            )]
        [HttpPost]
        public IActionResult Post([FromBody] Solicitud_XML Solicitud_XML)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Solicitud_XML.XML))
                {
                    return BadRequest(new Respuesta_Error_XML
                    {
                        Status = "400",
                        Title = "String vacio",
                        Detail = "Haz enviado un string vacio"
                    });
                }

                var XML_From_64 = Encoding.UTF8.GetString(Convert.FromBase64String(Solicitud_XML.XML));

                using (var String_Reader = new StringReader(XML_From_64))
                {
                    var Serializador = new XmlSerializer(typeof(Comprobante));
                    var Comprobante = (Comprobante) Serializador.Deserialize(String_Reader);
                    var Result = Valida_Comprobante(Comprobante);
                    if (Result.Any())
                    {
                        return BadRequest(new Respuesta_Error_XML()
                        {
                            Status = "400",
                            Title = "Errores en la estructura del XML",
                            Detail = "La estructura del XML, contiene errores, pueden faltar atributos o los tipo de datos no corresponden a la estructura especificada en el xsd",
                            Errors = Result
                        });
                    } 
                    return Ok(new { Comprobante });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(400, new Respuesta_Error_XML
                {
                    Status = "400",
                    Title = "Formato incorrecto",
                    Detail = "Haz enviado un string con tipo de dato incorrecto o mal formado",
                    Errors = [ex.InnerException.Message]
                });
            }
        }

        private List<string> Valida_Comprobante(Comprobante comprobante)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(comprobante.Version))
                errores.Add("El atributo 'Version' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.LugarExpedicion))
                errores.Add("El atributo 'LugarExpedicion' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.MetodoPago))
                errores.Add("El atributo 'MetodoPago' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.TipoDeComprobante))
                errores.Add("El atributo 'TipoDeComprobante' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.FormaPago))
                errores.Add("El atributo 'FormaPago' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.Folio))
                errores.Add("El atributo 'Folio' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.Moneda))
                errores.Add("El atributo 'Moneda' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.Serie))
                errores.Add("El atributo 'Serie' es obligatorio.");
            if (string.IsNullOrWhiteSpace(comprobante.UUID))
                errores.Add("El atributo 'UUID' es obligatorio.");
            if (!Es_Decimal_Valido(comprobante.Total.ToString()))
                errores.Add("El atributo 'Total' debe ser un número decimal válido.");
            if (!Es_Decimal_Valido(comprobante.SubTotal.ToString()))
                errores.Add("El atributo 'SubTotal' debe ser un número decimal válido.");

            if (comprobante.Emisor == null)
            {
                errores.Add("El nodo 'Emisor' es obligatorio.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(comprobante.Emisor.Rfc))
                    errores.Add("El atributo 'Rfc' de 'Emisor' es obligatorio.");
                if (string.IsNullOrWhiteSpace(comprobante.Emisor.Nombre))
                    errores.Add("El atributo 'Nombre' de 'Emisor' es obligatorio.");
            }

            if (comprobante.Receptor == null)
            {
                errores.Add("El nodo 'Receptor' es obligatorio.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(comprobante.Receptor.Rfc))
                    errores.Add("El atributo 'Rfc' de 'Receptor' es obligatorio.");
                if (string.IsNullOrWhiteSpace(comprobante.Receptor.Nombre))
                    errores.Add("El atributo 'Nombre' de 'Receptor' es obligatorio.");
            }

            if (comprobante.Conceptos == null || !comprobante.Conceptos.Any())
            {
                errores.Add("Debe existir al menos un nodo 'Concepto'.");
            }
            else
            {
                for (int i = 0; i < comprobante.Conceptos.Length; i++)
                {
                    var concepto = comprobante.Conceptos[i];
                    if (string.IsNullOrWhiteSpace(concepto.NoIdentificacion))
                        errores.Add($"El atributo 'NoIdentificacion' del 'Concepto' {i + 1} es obligatorio.");
                    if (string.IsNullOrWhiteSpace(concepto.ClaveProdServ.ToString()))
                        errores.Add($"El atributo 'ClaveProdServ' del 'Concepto' {i + 1} es obligatorio.");
                    if (string.IsNullOrWhiteSpace(concepto.Descripcion))
                        errores.Add($"El atributo 'Descripcion' del 'Concepto' {i + 1} es obligatorio.");
                    if (string.IsNullOrWhiteSpace(concepto.ClaveUnidad))
                        errores.Add($"El atributo 'ClaveUnidad' del 'Concepto' {i + 1} es obligatorio.");
                    if (!Es_Decimal_Valido(concepto.ValorUnitario.ToString()))
                        errores.Add($"El atributo 'ValorUnitario' del 'Concepto' {i + 1} debe ser un número decimal válido.");
                    if (!Es_Numero_Valido(concepto.Cantidad.ToString()))
                        errores.Add($"El atributo 'Cantidad' del 'Concepto' {i + 1} debe ser un número entero válido.");
                    if (!Es_Decimal_Valido(concepto.Importe.ToString()))
                        errores.Add($"El atributo 'Importe' del 'Concepto' {i + 1} debe ser un número decimal válido.");
                }
            }

            return errores;
        }

        private bool Es_Decimal_Valido(string valor)
        {
            return decimal.TryParse(valor, out _);
        }

        private bool Es_Numero_Valido(string valor)
        {
            return int.TryParse(valor, out _);
        }


    }
}
