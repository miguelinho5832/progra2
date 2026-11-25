using ejemplo1.models;

namespace ejemplo1.Interfaces
{
    public interface IRepositorio<T> where T : EntidadBase
    {
        Task<List<T>> ListaEntidadAsync();

        Task<T?> ObtenerIdAsync(int id);

        Task<List<T>> BuscarAsync(string texto);

        Task<int> CrearAsync(T entidad);

        Task<int> EditarAsync(T entidad);
        Task<int> ActualizarParcialAsync(int id, Dictionary<string, object?> campos);

        Task<int> EliminarAsync(int id);
    }
}
