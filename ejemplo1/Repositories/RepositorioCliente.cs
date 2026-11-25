using ejemplo1.config;
using ejemplo1.Interfaces;
using ejemplo1.models;
using MySql.Data.MySqlClient;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.Repositories
{
    public class RepositorioCliente : IRepositorio<Cliente>
    {
        private readonly ConfiguracionBD _configuracion;
        private readonly EntidadFactory<Cliente> _clienteFactory;

        public RepositorioCliente(ConfiguracionBD configuracion, EntidadFactory<Cliente> clienteFactory)
        {
            _configuracion = configuracion;
            _clienteFactory = clienteFactory;
        }

        public async Task<List<Cliente>> ListaEntidadAsync()
        {
            var clientes = new List<Cliente>();

            const string sql = @"SELECT id, nombre, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 ORDER BY nombre ASC, id ASC";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                clientes.Add(MapearCliente(lector));
            }

            return clientes;
        }

        public async Task<Cliente?> ObtenerIdAsync(int id)
        {
            const string sql = @"SELECT id, nombre, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 WHERE id = @id";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                return MapearCliente(lector);
            }

            return null;
        }

        public async Task<List<Cliente>> BuscarAsync(string texto)
        {
            var clientes = new List<Cliente>();

            const string sql = @"SELECT id, nombre, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 WHERE nombre LIKE @texto
                                    OR telefono LIKE @texto
                                    OR email LIKE @texto
                                    OR direccion LIKE @texto
                                 ORDER BY nombre ASC, id ASC";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@texto", "%" + texto.Trim() + "%");

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                clientes.Add(MapearCliente(lector));
            }

            return clientes;
        }

        public async Task<int> CrearAsync(Cliente entidad)
        {
            const string sql = @"INSERT INTO cliente (nombre, telefono, email, direccion)
                                 VALUES (@nombre, @telefono, @email, @direccion);
                                 SELECT LAST_INSERT_ID();";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@nombre", entidad.Nombre.Trim());
            comando.Parameters.AddWithValue("@telefono", entidad.Telefono.Trim());
            comando.Parameters.AddWithValue("@email", (object?)entidad.Email?.Trim() ?? DBNull.Value);
            comando.Parameters.AddWithValue("@direccion", (object?)entidad.Direccion?.Trim() ?? DBNull.Value);

            var resultado = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        public async Task<int> EditarAsync(Cliente entidad)
        {
            const string sql = @"UPDATE cliente
                                 SET nombre = @nombre,
                                     telefono = @telefono,
                                     email = @email,
                                     direccion = @direccion
                                 WHERE id = @id";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", entidad.Id);
            comando.Parameters.AddWithValue("@nombre", entidad.Nombre.Trim());
            comando.Parameters.AddWithValue("@telefono", entidad.Telefono.Trim());
            comando.Parameters.AddWithValue("@email", (object?)entidad.Email?.Trim() ?? DBNull.Value);
            comando.Parameters.AddWithValue("@direccion", (object?)entidad.Direccion?.Trim() ?? DBNull.Value);

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> ActualizarParcialAsync(int id, Dictionary<string, object?> campos)
        {
            var actual = await ObtenerIdAsync(id);
            if (actual is null || campos is null || campos.Count == 0)
            {
                return 0;
            }

            foreach (var par in campos)
            {
                var clave = par.Key.ToLowerInvariant();
                var valor = par.Value?.ToString();

                switch (clave)
                {
                    case "nombre":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Nombre = valor.Trim();
                        break;
                    case "telefono":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Telefono = valor.Trim();
                        break;
                    case "email":
                        actual.Email = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
                        break;
                    case "direccion":
                        actual.Direccion = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
                        break;
                }
            }

            return await EditarAsync(actual);
        }

        public async Task<int> EliminarAsync(int id)
        {
            const string sql = "DELETE FROM cliente WHERE id = @id";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            return await comando.ExecuteNonQueryAsync();
        }

        private Cliente MapearCliente(MySqlDataReader lector)
        {
            var cliente = _clienteFactory.Crear();
            cliente.Id = Convert.ToInt32(lector["id"]);
            cliente.Nombre = lector["nombre"].ToString()!;
            cliente.Telefono = lector["telefono"].ToString()!;
            cliente.Email = lector["email"] == DBNull.Value ? null : lector["email"].ToString();
            cliente.Direccion = lector["direccion"] == DBNull.Value ? null : lector["direccion"].ToString();
            cliente.FechaRegistro = Convert.ToDateTime(lector["fecha_registro"]);
            return cliente;
        }
    }
}
