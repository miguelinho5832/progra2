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

            const string sql = @"SELECT id, nombre, apellido, ci_nit, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 WHERE eliminado = 0
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
            const string sql = @"SELECT id, nombre, apellido, ci_nit, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 WHERE id = @id AND eliminado = 0";

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

            const string sql = @"SELECT id, nombre, apellido, ci_nit, telefono, email, direccion, fecha_registro
                                 FROM cliente
                                 WHERE eliminado = 0
                                   AND (nombre LIKE @texto
                                     OR apellido LIKE @texto
                                     OR ci_nit LIKE @texto
                                     OR telefono LIKE @texto
                                     OR email LIKE @texto
                                     OR direccion LIKE @texto)
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
            const string sql = @"INSERT INTO cliente (nombre, apellido, ci_nit, telefono, email, direccion, estado, eliminado)
                                 VALUES (@nombre, @apellido, @ci_nit, @telefono, @email, @direccion, 1, 0);
                                 SELECT LAST_INSERT_ID();";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@nombre", entidad.Nombre.Trim());
            comando.Parameters.AddWithValue("@apellido", (entidad.Apellido ?? "").Trim());
            comando.Parameters.AddWithValue("@ci_nit", (entidad.CiNit ?? "").Trim());
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
                                     apellido = @apellido,
                                     ci_nit = @ci_nit,
                                     telefono = @telefono,
                                     email = @email,
                                     direccion = @direccion
                                 WHERE id = @id AND eliminado = 0";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", entidad.Id);
            comando.Parameters.AddWithValue("@nombre", entidad.Nombre.Trim());
            comando.Parameters.AddWithValue("@apellido", (entidad.Apellido ?? "").Trim());
            comando.Parameters.AddWithValue("@ci_nit", (entidad.CiNit ?? "").Trim());
            comando.Parameters.AddWithValue("@telefono", entidad.Telefono.Trim());
            comando.Parameters.AddWithValue("@email", (object?)entidad.Email?.Trim() ?? DBNull.Value);
            comando.Parameters.AddWithValue("@direccion", (object?)entidad.Direccion?.Trim() ?? DBNull.Value);

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> ActualizarParcialAsync(int id, Dictionary<string, object?> campos)
        {
            if (id <= 0 || campos is null || campos.Count == 0)
            {
                return 0;
            }

            var columnasPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "nombre",
                "apellido",
                "ci_nit",
                "telefono",
                "email",
                "direccion"
            };

            var sets = new List<string>();
            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand();
            comando.Connection = conexion;

            int idx = 0;
            foreach (var par in campos)
            {
                string clave = par.Key.Trim().ToLowerInvariant();
                if (clave == "cinit")
                {
                    clave = "ci_nit";
                }

                if (!columnasPermitidas.Contains(clave))
                {
                    continue;
                }

                string paramName = $"@p{idx++}";
                sets.Add($"{clave} = {paramName}");

                if (par.Value is null)
                {
                    comando.Parameters.AddWithValue(paramName, DBNull.Value);
                }
                else if (par.Value is string s)
                {
                    string limpio = s.Trim();

                    if (clave == "nombre" || clave == "apellido")
                    {
                        limpio = Validaciones.Capitalizar(Validaciones.SanitizarTexto(limpio, 50));
                        comando.Parameters.AddWithValue(paramName, limpio);
                    }
                    else if (clave == "ci_nit")
                    {
                        comando.Parameters.AddWithValue(paramName, Validaciones.SanitizarTexto(limpio, 10));
                    }
                    else if (clave == "telefono")
                    {
                        comando.Parameters.AddWithValue(paramName, Validaciones.SanitizarTexto(limpio, 8));
                    }
                    else if (clave == "email")
                    {
                        string emailLimpio = Validaciones.SanitizarTexto(limpio, 50);
                        comando.Parameters.AddWithValue(
                            paramName,
                            string.IsNullOrWhiteSpace(emailLimpio) ? DBNull.Value : emailLimpio);
                    }
                    else if (clave == "direccion")
                    {
                        string dirLimpia = Validaciones.SanitizarTexto(limpio, 255);
                        comando.Parameters.AddWithValue(
                            paramName,
                            string.IsNullOrWhiteSpace(dirLimpia) ? DBNull.Value : dirLimpia);
                    }
                    else
                    {
                        comando.Parameters.AddWithValue(paramName, limpio);
                    }
                }
                else
                {
                    comando.Parameters.AddWithValue(paramName, par.Value);
                }
            }

            if (sets.Count == 0)
            {
                return 0;
            }

            comando.CommandText = $@"UPDATE cliente
                                     SET {string.Join(", ", sets)}
                                     WHERE id = @id AND eliminado = 0";
            comando.Parameters.AddWithValue("@id", id);

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> EliminarAsync(int id)
        {
            const string sql = @"UPDATE cliente
                                 SET eliminado = 1
                                 WHERE id = @id AND eliminado = 0";

            try
            {
                using var conexion = new MySqlConnection(_configuracion.ConnectionString);
                await conexion.OpenAsync();

                using var comando = new MySqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("@id", id);

                return await comando.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                throw new InvalidOperationException("No se puede eliminar el cliente porque tiene registros asociados.", ex);
            }
        }

        private Cliente MapearCliente(MySqlDataReader lector)
        {
            var cliente = _clienteFactory.Crear();
            cliente.Id = Convert.ToInt32(lector["id"]);
            cliente.Nombre = lector["nombre"].ToString()!;
            cliente.Apellido = lector["apellido"] == DBNull.Value ? "" : lector["apellido"].ToString()!;
            cliente.CiNit = lector["ci_nit"] == DBNull.Value ? "" : lector["ci_nit"].ToString()!;
            cliente.Telefono = lector["telefono"].ToString()!;
            cliente.Email = lector["email"] == DBNull.Value ? "" : lector["email"].ToString()!;
            cliente.Direccion = lector["direccion"] == DBNull.Value ? "" : lector["direccion"].ToString()!;
            cliente.FechaRegistro = Convert.ToDateTime(lector["fecha_registro"]);
            return cliente;
        }
    }
}