using System.Net.Http.Json;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
        }

        public int TotalVehiculos { get; set; }
        public int TotalEnReparacion { get; set; }
        public int TotalListosEntrega { get; set; }
        public int TotalEntregasHoy { get; set; }

        public List<Vehiculo> Vehiculos { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<Vehiculo>>("api/vehiculos");
                Vehiculos = lista ?? new List<Vehiculo>();

                TotalVehiculos = Vehiculos.Count;
                TotalEnReparacion = 0;
                TotalListosEntrega = 0;
                TotalEntregasHoy = 0;
            }
            catch
            {
                Vehiculos = new List<Vehiculo>();
                TotalVehiculos = 0;
                TotalEnReparacion = 0;
                TotalListosEntrega = 0;
                TotalEntregasHoy = 0;
            }
        }
    }
}