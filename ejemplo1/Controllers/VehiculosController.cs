using ejemplo1.Interfaces;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;

namespace ejemplo1.Controllers
{
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