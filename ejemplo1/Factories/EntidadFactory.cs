using ejemplo1.models;


namespace TallerMecanicoSOLID.Factories
{
    public abstract class EntidadFactory<T>
        where T : EntidadBase
    {
        public abstract T Crear();
    }
}

