using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.Pages
{
    public class VehiculoPanelItem
    {
        public string Placa { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
        public string Marca { get; set; } = "";
        public string Modelo { get; set; } = "";
        public int Anio { get; set; }
        public string Tipo { get; set; } = "";
        public int Kilometraje { get; set; }
        public bool Estado { get; set; }
        public string EstadoTrabajo { get; set; } = "";
        public bool EntregaHoy { get; set; }
    }

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

        public List<VehiculoPanelItem> Vehiculos { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var lista = await _httpClient.GetFromJsonAsync<List<VehiculoPanelItem>>("api/vehiculos");
                Vehiculos = lista ?? new List<VehiculoPanelItem>();

                TotalVehiculos = Vehiculos.Count;
                TotalEnReparacion = Vehiculos.Count(v =>
                    v.EstadoTrabajo.Equals("En reparación", StringComparison.OrdinalIgnoreCase));
                TotalListosEntrega = Vehiculos.Count(v =>
                    v.EstadoTrabajo.Equals("Listo para entregar", StringComparison.OrdinalIgnoreCase));
                TotalEntregasHoy = Vehiculos.Count(v => v.EntregaHoy);
            }
            catch
            {
                Vehiculos = new List<VehiculoPanelItem>();
                TotalVehiculos = 0;
                TotalEnReparacion = 0;
                TotalListosEntrega = 0;
                TotalEntregasHoy = 0;
            }
        }
    }
}