using ejemplo1.Interfaces;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;

namespace ejemplo1.Controllers
{
    public class VehiculoPatchDto
    {
        public int? Kilometraje { get; set; }
        public string? Color { get; set; }
        public string? Tipo { get; set; }
        public int? ClienteId { get; set; }
        public bool? Estado { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class VehiculosController : ControllerBase
    {
        private readonly IRepositorio<Vehiculo> _repositorioVehiculo;

        public VehiculosController(IRepositorio<Vehiculo> repositorioVehiculo)
        {
            _repositorioVehiculo = repositorioVehiculo;
        }

        // GET: api/vehiculos?buscar=...
        [HttpGet]
        public async Task<ActionResult<List<Vehiculo>>> Get([FromQuery] string? buscar = null)
        {
            var vehiculos = string.IsNullOrWhiteSpace(buscar)
                ? await _repositorioVehiculo.ListaEntidadAsync()
                : await _repositorioVehiculo.BuscarAsync(buscar);

            return Ok(vehiculos);
        }

        // GET: api/vehiculos/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Vehiculo>> GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del vehículo debe ser un número mayor a 0." });
            }

            var vehiculo = await _repositorioVehiculo.ObtenerIdAsync(id);
            if (vehiculo is null)
            {
                return NotFound(new { mensaje = $"No existe un vehículo registrado con el ID {id}." });
            }

            return Ok(vehiculo);
        }

        // POST: api/vehiculos
        [HttpPost]
        public async Task<ActionResult<Vehiculo>> Post([FromBody] Vehiculo vehiculo)
        {
            if (vehiculo is null)
            {
                return BadRequest(new { mensaje = "Debe enviar los datos del vehículo." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var nuevoId = await _repositorioVehiculo.CrearAsync(vehiculo);
            var creado = await _repositorioVehiculo.ObtenerIdAsync(nuevoId);

            return CreatedAtAction(nameof(GetById), new { id = nuevoId }, creado ?? vehiculo);
        }

        // PUT: api/vehiculos/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vehiculo vehiculo)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del vehículo debe ser un número mayor a 0." });
            }

            if (vehiculo is null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existente = await _repositorioVehiculo.ObtenerIdAsync(id);
            if (existente is null)
            {
                return NotFound(new { mensaje = $"No se encontró un vehículo con ID {id}." });
            }

            vehiculo.Id = id;
            var filas = await _repositorioVehiculo.EditarAsync(vehiculo);
            if (filas <= 0)
            {
                return StatusCode(500, new { mensaje = "No se pudo actualizar el vehículo." });
            }

            return NoContent();
        }

        // PATCH: api/vehiculos/1
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] VehiculoPatchDto datos)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del vehículo debe ser un número mayor a 0." });
            }

            if (datos is null ||
                (datos.Kilometraje is null &&
                 datos.Color is null &&
                 datos.Tipo is null &&
                 datos.ClienteId is null &&
                 datos.Estado is null))
            {
                return BadRequest(new { mensaje = "Envíe al menos un campo (kilometraje, color, tipo, clienteId o estado) para actualizar." });
            }

            var campos = new Dictionary<string, object?>();

            if (datos.Kilometraje is not null)
            {
                if (datos.Kilometraje.Value < 0 || datos.Kilometraje.Value > 2000000)
                {
                    return BadRequest(new { mensaje = "El kilometraje debe estar entre 0 y 2.000.000." });
                }
                campos["kilometraje"] = datos.Kilometraje.Value;
            }

            if (datos.Color is not null)
            {
                string colorLimpio = datos.Color.Trim();
                if (string.IsNullOrWhiteSpace(colorLimpio) || colorLimpio.Length > 40)
                {
                    return BadRequest(new { mensaje = "El color es obligatorio y no puede superar 40 caracteres." });
                }
                campos["color"] = colorLimpio;
            }

            if (datos.Tipo is not null)
            {
                string tipoLimpio = datos.Tipo.Trim();
                if (string.IsNullOrWhiteSpace(tipoLimpio) || tipoLimpio.Length > 40)
                {
                    return BadRequest(new { mensaje = "El tipo de vehículo es obligatorio y no puede superar 40 caracteres." });
                }
                campos["tipo"] = tipoLimpio;
            }

            if (datos.ClienteId is not null)
            {
                if (datos.ClienteId.Value <= 0)
                {
                    return BadRequest(new { mensaje = "Debe seleccionar un cliente válido." });
                }
                campos["cliente_id"] = datos.ClienteId.Value;
            }

            if (datos.Estado is not null)
            {
                campos["estado"] = datos.Estado.Value;
            }

            var filas = await _repositorioVehiculo.ActualizarParcialAsync(id, campos);
            if (filas <= 0)
            {
                return NotFound(new { mensaje = $"No se encontró un vehículo con ID {id} para actualizar parcialmente." });
            }

            return NoContent();
        }

        // DELETE: api/vehiculos/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del vehículo debe ser un número mayor a 0." });
            }

            var filas = await _repositorioVehiculo.EliminarAsync(id);
            if (filas <= 0)
            {
                return NotFound(new { mensaje = $"No se encontró un vehículo con ID {id}." });
            }

            return NoContent();
        }
    }
}