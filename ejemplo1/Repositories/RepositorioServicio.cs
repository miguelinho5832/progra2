using ejemplo1.config;
using ejemplo1.Interfaces;
using ejemplo1.models;
using MySql.Data.MySqlClient;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.Repositories
{
    public class RepositorioServicio : IRepositorio<Servicio>
    {
        private readonly ConfiguracionBD _configuracion;
        private readonly EntidadFactory<Servicio> _factory;

        public RepositorioServicio(
            ConfiguracionBD configuracion,
            EntidadFactory<Servicio> factory)
        {
            _configuracion = configuracion;
            _factory = factory;
        }

        public async Task<List<Servicio>> ListaEntidadAsync()
        {
            List<Servicio> servicios = new();

            string sql = @"SELECT id, nombre, tipo, descripcion, precio,
                                  duracion_estimada, unidad_duracion, estado
                           FROM servicio
                           WHERE eliminado = 0
                           ORDER BY id DESC";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                servicios.Add(MapearServicio(lector));
            }

            return servicios;
        }

        public async Task<Servicio?> ObtenerIdAsync(int id)
        {
            string sql = @"SELECT id, nombre, tipo, descripcion, precio,
                                  duracion_estimada, unidad_duracion, estado
                           FROM servicio
                           WHERE id = @id AND eliminado = 0";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                return MapearServicio(lector);
            }

            return null;
        }

        public async Task<List<Servicio>> BuscarAsync(string texto)
        {
            List<Servicio> servicios = new();

            string sql = @"SELECT id, nombre, tipo, descripcion, precio,
                                  duracion_estimada, unidad_duracion, estado
                           FROM servicio
                           WHERE eliminado = 0
                             AND (nombre LIKE @texto
                                  OR tipo LIKE @texto
                                  OR descripcion LIKE @texto)
                           ORDER BY id DESC";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.AddWithValue("@texto", "%" + texto + "%");

            using var lector = (MySqlDataReader)await comando.ExecuteReaderAsync();

            while (await lector.ReadAsync())
            {
                servicios.Add(MapearServicio(lector));
            }

            return servicios;
        }

        public async Task<int> CrearAsync(Servicio entidad)
        {
            string sql = @"INSERT INTO servicio
                               (nombre, tipo, descripcion, precio,
                                duracion_estimada, unidad_duracion, estado, eliminado)
                           VALUES
                               (@nombre, @tipo, @descripcion, @precio,
                                @duracion_estimada, @unidad_duracion, @estado, 0)";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            AgregarParametros(comando, entidad);

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> EditarAsync(Servicio entidad)
        {
            string sql = @"UPDATE servicio
                           SET nombre = @nombre,
                               tipo = @tipo,
                               descripcion = @descripcion,
                               precio = @precio,
                               duracion_estimada = @duracion_estimada,
                               unidad_duracion = @unidad_duracion,
                               estado = @estado
                           WHERE id = @id AND eliminado = 0";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.AddWithValue("@id", entidad.Id);
            AgregarParametros(comando, entidad);

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> ActualizarParcialAsync(int id, Dictionary<string, object?> campos)
        {
            if (!Validaciones.EsIdValido(id) || campos == null || campos.Count == 0)
            {
                return 0;
            }

            var columnasPermitidas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["nombre"] = "nombre",
                ["tipo"] = "tipo",
                ["descripcion"] = "descripcion",
                ["precio"] = "precio",
                ["duracionestimada"] = "duracion_estimada",
                ["duracion_estimada"] = "duracion_estimada",
                ["unidadduracion"] = "unidad_duracion",
                ["unidad_duracion"] = "unidad_duracion",
                ["estado"] = "estado"
            };

            var setClauses = new List<string>();
            var parametros = new List<MySqlParameter>
            {
                new MySqlParameter("@id", id)
            };

            int indice = 0;
            foreach (var par in campos)
            {
                string claveNormalizada = par.Key.Replace("_", "").Trim();
                if (!columnasPermitidas.TryGetValue(claveNormalizada, out string? columnaSql))
                {
                    continue;
                }

                string nombreParametro = $"@p{indice++}";
                setClauses.Add($"{columnaSql} = {nombreParametro}");
                parametros.Add(new MySqlParameter(nombreParametro, par.Value ?? DBNull.Value));
            }

            if (setClauses.Count == 0)
            {
                return 0;
            }

            string sql = $@"UPDATE servicio
                           SET {string.Join(", ", setClauses)}
                           WHERE id = @id AND eliminado = 0";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.AddRange(parametros.ToArray());

            return await comando.ExecuteNonQueryAsync();
        }

        public async Task<int> EliminarAsync(int id)
        {
            string sql = @"UPDATE servicio
                           SET eliminado = 1
                           WHERE id = @id AND eliminado = 0";

            using MySqlConnection conexion = new(_configuracion.ConnectionString);
            await conexion.OpenAsync();

            using MySqlCommand comando = new(sql, conexion);
            comando.Parameters.AddWithValue("@id", id);

            return await comando.ExecuteNonQueryAsync();
        }

        private Servicio MapearServicio(MySqlDataReader lector)
        {
            Servicio servicio = _factory.Crear();

            servicio.Id = Convert.ToInt32(lector["id"]);
            servicio.Nombre = lector["nombre"]?.ToString() ?? "";
            servicio.Tipo = lector["tipo"]?.ToString() ?? "";
            servicio.Descripcion = lector["descripcion"]?.ToString() ?? "";
            servicio.Precio = lector["precio"] == DBNull.Value
                ? null
                : Convert.ToDecimal(lector["precio"]);
            servicio.DuracionEstimada = lector["duracion_estimada"] == DBNull.Value
                ? null
                : Convert.ToInt32(lector["duracion_estimada"]);
            servicio.UnidadDuracion = lector["unidad_duracion"]?.ToString() ?? "";
            servicio.Estado = lector["estado"] != DBNull.Value && Convert.ToBoolean(lector["estado"]);

            return servicio;
        }

        private static void AgregarParametros(MySqlCommand comando, Servicio entidad)
        {
            comando.Parameters.AddWithValue("@nombre", entidad.Nombre ?? "");
            comando.Parameters.AddWithValue("@tipo", entidad.Tipo ?? "");
            comando.Parameters.AddWithValue("@descripcion", entidad.Descripcion ?? "");
            comando.Parameters.AddWithValue("@precio", (object?)entidad.Precio ?? DBNull.Value);
            comando.Parameters.AddWithValue("@duracion_estimada", (object?)entidad.DuracionEstimada ?? DBNull.Value);
            comando.Parameters.AddWithValue("@unidad_duracion", entidad.UnidadDuracion ?? "");
            comando.Parameters.AddWithValue("@estado", entidad.Estado);
        }
    }
}