using ejemplo1.config;
using ejemplo1.Interfaces;
using ejemplo1.models;
using MySql.Data.MySqlClient;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.Repositories
{
    public class RepositorioVehiculo : IRepositorio<Vehiculo>
    {
        private readonly ConfiguracionBD _configuracion;
        private readonly EntidadFactory<Vehiculo> _vehiculoFactory;

        public RepositorioVehiculo(ConfiguracionBD configuracion, EntidadFactory<Vehiculo> vehiculoFactory)
        {
            _configuracion = configuracion;
            _vehiculoFactory = vehiculoFactory;
        }

        public async Task<List<Vehiculo>> ListaEntidadAsync()
        {
            var vehiculos = new List<Vehiculo>();

            const string sql = @"SELECT v.id, v.cliente_id, v.placa, v.marca, v.modelo,
                                        v.anio, v.color, v.tipo, v.kilometraje, v.estado, v.creado_en,
                                        CONCAT(IFNULL(c.nombre, ''), ' ', IFNULL(c.apellido, '')) AS cliente_nombre
                                 FROM vehiculo v
                                 LEFT JOIN cliente c ON v.cliente_id = c.id
                                 WHERE v.eliminado = 0
                                 ORDER BY v.id DESC";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                vehiculos.Add(MapearVehiculo(lector));
            }

            return vehiculos;
        }

        public async Task<Vehiculo?> ObtenerIdAsync(int id)
        {
            const string sql = @"SELECT v.id, v.cliente_id, v.placa, v.marca, v.modelo,
                                        v.anio, v.color, v.tipo, v.kilometraje, v.estado, v.creado_en,
                                        CONCAT(IFNULL(c.nombre, ''), ' ', IFNULL(c.apellido, '')) AS cliente_nombre
                                 FROM vehiculo v
                                 LEFT JOIN cliente c ON v.cliente_id = c.id
                                 WHERE v.id = @id AND v.eliminado = 0";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                return MapearVehiculo(lector);
            }

            return null;
        }

        public async Task<List<Vehiculo>> BuscarAsync(string texto)
        {
            var vehiculos = new List<Vehiculo>();

            const string sql = @"SELECT v.id, v.cliente_id, v.placa, v.marca, v.modelo,
                                        v.anio, v.color, v.tipo, v.kilometraje, v.estado, v.creado_en,
                                        CONCAT(IFNULL(c.nombre, ''), ' ', IFNULL(c.apellido, '')) AS cliente_nombre
                                 FROM vehiculo v
                                 LEFT JOIN cliente c ON v.cliente_id = c.id
                                 WHERE v.eliminado = 0
                                   AND (v.placa LIKE @texto
                                     OR v.marca LIKE @texto
                                     OR v.modelo LIKE @texto
                                     OR v.color LIKE @texto
                                     OR v.tipo LIKE @texto
                                     OR c.nombre LIKE @texto
                                     OR c.apellido LIKE @texto)
                                 ORDER BY v.id DESC";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@texto", "%" + texto.Trim() + "%");

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                vehiculos.Add(MapearVehiculo(lector));
            }

            return vehiculos;
        }

        public async Task<int> CrearAsync(Vehiculo entidad)
        {
            const string sql = @"INSERT INTO vehiculo
                                    (cliente_id, placa, marca, modelo, anio, color, tipo, kilometraje, estado, eliminado)
                                 VALUES
                                    (@cliente_id, @placa, @marca, @modelo, @anio, @color, @tipo, @kilometraje, 1, 0);
                                 SELECT LAST_INSERT_ID();";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@cliente_id", entidad.ClienteId);
            comando.Parameters.AddWithValue("@placa", entidad.Placa.Trim().ToUpperInvariant());
            comando.Parameters.AddWithValue("@marca", entidad.Marca.Trim());
            comando.Parameters.AddWithValue("@modelo", entidad.Modelo.Trim());
            comando.Parameters.AddWithValue("@anio", entidad.Anio ?? 0);
            comando.Parameters.AddWithValue("@color", entidad.Color.Trim());
            comando.Parameters.AddWithValue("@tipo", entidad.Tipo.Trim());
            comando.Parameters.AddWithValue("@kilometraje", entidad.Kilometraje ?? 0);

            var resultado = await comando.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }

        public async Task<int> EditarAsync(Vehiculo entidad)
        {
            const string sql = @"UPDATE vehiculo
                                 SET cliente_id = @cliente_id,
                                     placa = @placa,
                                     marca = @marca,
                                     modelo = @modelo,
                                     anio = @anio,
                                     color = @color,
                                     tipo = @tipo,
                                     kilometraje = @kilometraje
                                 WHERE id = @id AND eliminado = 0";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", entidad.Id);
            comando.Parameters.AddWithValue("@cliente_id", entidad.ClienteId);
            comando.Parameters.AddWithValue("@placa", entidad.Placa.Trim().ToUpperInvariant());
            comando.Parameters.AddWithValue("@marca", entidad.Marca.Trim());
            comando.Parameters.AddWithValue("@modelo", entidad.Modelo.Trim());
            comando.Parameters.AddWithValue("@anio", entidad.Anio ?? 0);
            comando.Parameters.AddWithValue("@color", entidad.Color.Trim());
            comando.Parameters.AddWithValue("@tipo", entidad.Tipo.Trim());
            comando.Parameters.AddWithValue("@kilometraje", entidad.Kilometraje ?? 0);

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
                    case "placa":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Placa = valor.Trim().ToUpperInvariant();
                        break;
                    case "marca":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Marca = valor.Trim();
                        break;
                    case "modelo":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Modelo = valor.Trim();
                        break;
                    case "anio":
                        if (int.TryParse(valor, out int anio))
                            actual.Anio = anio;
                        break;
                    case "color":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Color = valor.Trim();
                        break;
                    case "tipo":
                        if (!string.IsNullOrWhiteSpace(valor))
                            actual.Tipo = valor.Trim();
                        break;
                    case "kilometraje":
                        if (int.TryParse(valor, out int km))
                            actual.Kilometraje = km;
                        break;
                    case "cliente_id":
                    case "clienteid":
                        if (int.TryParse(valor, out int clienteId))
                            actual.ClienteId = clienteId;
                        break;
                }
            }

            return await EditarAsync(actual);
        }

        public async Task<int> EliminarAsync(int id)
        {
            const string sql = @"UPDATE vehiculo
                                 SET eliminado = 1
                                 WHERE id = @id AND eliminado = 0";

            using var conexion = new MySqlConnection(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using var comando = new MySqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            return await comando.ExecuteNonQueryAsync();
        }

        private Vehiculo MapearVehiculo(MySqlDataReader lector)
        {
            var vehiculo = _vehiculoFactory.Crear();
            vehiculo.Id = Convert.ToInt32(lector["id"]);
            vehiculo.ClienteId = Convert.ToInt32(lector["cliente_id"]);
            vehiculo.Placa = lector["placa"].ToString()!;
            vehiculo.Marca = lector["marca"].ToString()!;
            vehiculo.Modelo = lector["modelo"].ToString()!;
            vehiculo.Anio = lector["anio"] == DBNull.Value ? null : Convert.ToInt32(lector["anio"]);
            vehiculo.Color = lector["color"] == DBNull.Value ? "" : lector["color"].ToString()!;
            vehiculo.Tipo = lector["tipo"] == DBNull.Value ? "" : lector["tipo"].ToString()!;
            vehiculo.Kilometraje = lector["kilometraje"] == DBNull.Value ? 0 : Convert.ToInt32(lector["kilometraje"]);
            vehiculo.Estado = lector["estado"] != DBNull.Value && Convert.ToInt32(lector["estado"]) == 1;
            vehiculo.ClienteNombre = lector["cliente_nombre"] == DBNull.Value ? "" : lector["cliente_nombre"].ToString()!.Trim();
            vehiculo.CreadoEn = lector["creado_en"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(lector["creado_en"]);
            return vehiculo;
        }
    }
}