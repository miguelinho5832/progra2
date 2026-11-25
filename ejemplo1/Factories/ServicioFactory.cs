using ejemplo1.models;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.Factories
{
    public class ServicioFactory : EntidadFactory<Servicio>
    {
        public override Servicio Crear()
        {
            return new Servicio();
        }
    }
}