using System.Text.Json;
using ejemplo1.config;
using ejemplo1.Interfaces;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;

namespace ejemplo1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiciosController : ControllerBase
    {
        private readonly IRepositorio<Servicio> _repositorio;

        public ServiciosController(IRepositorio<Servicio> repositorio)
        {
            _repositorio = repositorio;
        }

        [HttpGet]
        public async Task<ActionResult<List<Servicio>>> GetServicios()
        {
            try
            {
                var servicios = await _repositorio.ListaEntidadAsync();
                return Ok(servicios);
            }
            catch
            {
                return StatusCode(500, "Error interno al obtener los servicios.");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Servicio>> GetServicio(int id)
        {
            if (!Validaciones.EsIdValido(id))
            {
                return BadRequest("El ID del servicio no es válido.");
            }

            try
            {
                var servicio = await _repositorio.ObtenerIdAsync(id);
                if (servicio == null)
                {
                    return NotFound();
                }

                return Ok(servicio);
            }
            catch
            {
                return StatusCode(500, "Error interno al consultar el servicio.");
            }
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<List<Servicio>>> BuscarServicios([FromQuery] string? q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    var todos = await _repositorio.ListaEntidadAsync();
                    return Ok(todos);
                }

                string termino = Validaciones.SanitizarTexto(q, 100);
                var resultados = await _repositorio.BuscarAsync(termino);
                return Ok(resultados);
            }
            catch
            {
                return StatusCode(500, "Error interno al buscar servicios.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Servicio>> PostServicio([FromBody] Servicio servicio)
        {
            if (servicio == null)
            {
                return BadRequest("Los datos del servicio son obligatorios.");
            }

            NormalizarServicio(servicio);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int filas = await _repositorio.CrearAsync(servicio);
                if (filas <= 0)
                {
                    return StatusCode(500, "No se pudo registrar el servicio.");
                }

                return CreatedAtAction(nameof(GetServicio), new { id = servicio.Id }, servicio);
            }
            catch
            {
                return StatusCode(500, "Error interno al registrar el servicio.");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutServicio(int id, [FromBody] Servicio servicio)
        {
            if (!Validaciones.EsIdValido(id))
            {
                return BadRequest("El ID del servicio no es válido.");
            }

            if (servicio == null)
            {
                return BadRequest("Los datos del servicio son obligatorios.");
            }

            if (servicio.Id != 0 && servicio.Id != id)
            {
                return BadRequest("El ID del cuerpo no coincide con el ID de la ruta.");
            }

            servicio.Id = id;
            NormalizarServicio(servicio);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existente = await _repositorio.ObtenerIdAsync(id);
                if (existente == null)
                {
                    return NotFound();
                }

                int filas = await _repositorio.EditarAsync(servicio);
                if (filas <= 0)
                {
                    return StatusCode(500, "No se pudo actualizar el servicio.");
                }

                return NoContent();
            }
            catch
            {
                return StatusCode(500, "Error interno al actualizar el servicio.");
            }
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> PatchServicio(int id, [FromBody] Dictionary<string, JsonElement>? campos)
        {
            if (!Validaciones.EsIdValido(id))
            {
                return BadRequest("El ID del servicio no es válido.");
            }

            if (campos == null || campos.Count == 0)
            {
                return BadRequest("Debe enviar al menos un campo para actualizar.");
            }

            var diccionarioValores = new Dictionary<string, object?>();

            foreach (var par in campos)
            {
                string clave = Validaciones.SanitizarTexto(par.Key, 40);
                if (string.IsNullOrWhiteSpace(clave))
                {
                    continue;
                }

                diccionarioValores[clave] = ConvertirJsonElement(par.Value);
            }

            if (diccionarioValores.Count == 0)
            {
                return BadRequest("No se enviaron campos válidos para actualizar.");
            }

            try
            {
                var existente = await _repositorio.ObtenerIdAsync(id);
                if (existente == null)
                {
                    return NotFound();
                }

                int filas = await _repositorio.ActualizarParcialAsync(id, diccionarioValores);
                if (filas <= 0)
                {
                    return StatusCode(500, "No se pudo actualizar parcialmente el servicio.");
                }

                return NoContent();
            }
            catch
            {
                return StatusCode(500, "Error interno al actualizar parcialmente el servicio.");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteServicio(int id)
        {
            if (!Validaciones.EsIdValido(id))
            {
                return BadRequest("El ID del servicio no es válido.");
            }

            try
            {
                var existente = await _repositorio.ObtenerIdAsync(id);
                if (existente == null)
                {
                    return NotFound();
                }

                int filas = await _repositorio.EliminarAsync(id);
                if (filas <= 0)
                {
                    return StatusCode(500, "No se pudo eliminar el servicio.");
                }

                return NoContent();
            }
            catch
            {
                return StatusCode(500, "Error interno al eliminar el servicio.");
            }
        }

        private static void NormalizarServicio(Servicio servicio)
        {
            servicio.Nombre = Validaciones.SanitizarTexto(servicio.Nombre, 120);
            servicio.Tipo = Validaciones.SanitizarTexto(servicio.Tipo, 50);
            servicio.Descripcion = Validaciones.SanitizarTexto(servicio.Descripcion, 255);
            servicio.UnidadDuracion = Validaciones.SanitizarTexto(servicio.UnidadDuracion, 30);
        }

        private static object? ConvertirJsonElement(JsonElement elemento)
        {
            return elemento.ValueKind switch
            {
                JsonValueKind.String => Validaciones.SanitizarTexto(elemento.GetString(), 255),
                JsonValueKind.Number => elemento.TryGetInt32(out int entero)
                    ? entero
                    : elemento.TryGetDecimal(out decimal decimalVal)
                        ? decimalVal
                        : elemento.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => elemento.ToString()
            };
        }
    }
}