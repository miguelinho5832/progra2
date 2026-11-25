using ejemplo1.Interfaces;
using ejemplo1.models;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ejemplo1.Controllers
{
    public class ClientePatchDto
    {
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly IRepositorio<Cliente> _repositorioCliente;

        public ClientesController(IRepositorio<Cliente> repositorioCliente)
        {
            _repositorioCliente = repositorioCliente;
        }

        // GET: api/clientes?buscar=...
        [HttpGet]
        public async Task<ActionResult<List<Cliente>>> Get([FromQuery] string? buscar = null)
        {
            var clientes = string.IsNullOrWhiteSpace(buscar)
                ? await _repositorioCliente.ListaEntidadAsync()
                : await _repositorioCliente.BuscarAsync(buscar);

            return Ok(clientes);
        }

        // GET: api/clientes/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Cliente>> GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del cliente debe ser un número mayor a 0." });
            }

            var cliente = await _repositorioCliente.ObtenerIdAsync(id);
            if (cliente is null)
            {
                return NotFound(new { mensaje = $"No existe un cliente registrado con el ID {id}." });
            }

            return Ok(cliente);
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> Post([FromBody] Cliente cliente)
        {
            if (cliente is null)
            {
                return BadRequest(new { mensaje = "Debe enviar los datos del cliente." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(cliente.Nombre) || cliente.Nombre.Trim().Length < 2)
            {
                return BadRequest(new { mensaje = "El nombre del cliente debe tener al menos 2 caracteres." });
            }

            if (string.IsNullOrWhiteSpace(cliente.Telefono) || cliente.Telefono.Trim().Length < 6)
            {
                return BadRequest(new { mensaje = "El teléfono del cliente debe tener al menos 6 caracteres." });
            }

            if (!string.IsNullOrWhiteSpace(cliente.Email) && !new EmailAddressAttribute().IsValid(cliente.Email.Trim()))
            {
                return BadRequest(new { mensaje = "El correo electrónico no tiene un formato válido." });
            }

            var nuevoId = await _repositorioCliente.CrearAsync(cliente);
            var clienteCreado = await _repositorioCliente.ObtenerIdAsync(nuevoId);

            return CreatedAtAction(nameof(GetById), new { id = nuevoId }, clienteCreado ?? cliente);
        }

        // PUT: api/clientes/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Cliente cliente)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del cliente debe ser un número mayor a 0." });
            }

            if (cliente is null)
            {
                return BadRequest(new { mensaje = "Debe enviar los datos completos del cliente para actualizar." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(cliente.Nombre) || cliente.Nombre.Trim().Length < 2)
            {
                return BadRequest(new { mensaje = "El nombre del cliente debe tener al menos 2 caracteres." });
            }

            if (string.IsNullOrWhiteSpace(cliente.Telefono) || cliente.Telefono.Trim().Length < 6)
            {
                return BadRequest(new { mensaje = "El teléfono del cliente debe tener al menos 6 caracteres." });
            }

            if (!string.IsNullOrWhiteSpace(cliente.Email) && !new EmailAddressAttribute().IsValid(cliente.Email.Trim()))
            {
                return BadRequest(new { mensaje = "El correo electrónico no tiene un formato válido." });
            }

            var existente = await _repositorioCliente.ObtenerIdAsync(id);
            if (existente is null)
            {
                return NotFound(new { mensaje = $"No se encontró un cliente con ID {id} para actualizar." });
            }

            cliente.Id = id;
            var filasAfectadas = await _repositorioCliente.EditarAsync(cliente);
            if (filasAfectadas <= 0)
            {
                return StatusCode(500, new { mensaje = "No se pudo actualizar el registro del cliente." });
            }

            return NoContent();
        }

        // PATCH: api/clientes/1
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] ClientePatchDto datos)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del cliente debe ser un número mayor a 0." });
            }

            if (datos is null ||
                (datos.Nombre is null && datos.Telefono is null && datos.Email is null && datos.Direccion is null))
            {
                return BadRequest(new { mensaje = "Envíe al menos un campo (nombre, telefono, email o direccion) para actualizar." });
            }

            var campos = new Dictionary<string, object?>();

            if (datos.Nombre is not null)
            {
                if (datos.Nombre.Trim().Length < 2)
                {
                    return BadRequest(new { mensaje = "El nombre debe tener al menos 2 caracteres." });
                }
                campos["nombre"] = datos.Nombre;
            }

            if (datos.Telefono is not null)
            {
                if (datos.Telefono.Trim().Length < 6)
                {
                    return BadRequest(new { mensaje = "El teléfono debe tener al menos 6 caracteres." });
                }
                campos["telefono"] = datos.Telefono;
            }

            if (datos.Email is not null)
            {
                if (!string.IsNullOrWhiteSpace(datos.Email) && !new EmailAddressAttribute().IsValid(datos.Email.Trim()))
                {
                    return BadRequest(new { mensaje = "El correo electrónico no tiene un formato válido." });
                }
                campos["email"] = datos.Email;
            }

            if (datos.Direccion is not null)
            {
                campos["direccion"] = datos.Direccion;
            }

            var filasAfectadas = await _repositorioCliente.ActualizarParcialAsync(id, campos);
            if (filasAfectadas <= 0)
            {
                return NotFound(new { mensaje = $"No se encontró un cliente con ID {id} para actualizar parcialmente." });
            }

            return NoContent();
        }

        // DELETE: api/clientes/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El ID del cliente debe ser un número mayor a 0." });
            }

            var filasAfectadas = await _repositorioCliente.EliminarAsync(id);
            if (filasAfectadas <= 0)
            {
                return NotFound(new { mensaje = $"No se encontró un cliente con ID {id} para eliminar." });
            }

            return NoContent();
        }
    }
}