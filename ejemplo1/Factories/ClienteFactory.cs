using ejemplo1.models;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.Factories
{
    public class ClienteFactory : EntidadFactory<Cliente>
    {
        public override Cliente Crear()
        {
            return new Cliente();

        }
    }
}
