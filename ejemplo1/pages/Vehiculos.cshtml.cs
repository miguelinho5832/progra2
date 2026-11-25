using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
{
    public class VehiculosModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProtector _protector;

        public VehiculosModel(
            IHttpClientFactory httpClientFactory,
            IDataProtectionProvider provider)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _protector = provider.CreateProtector("ejemplo1.Vehiculos");
        }

        public List<Vehiculo> ListaVehiculos { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Buscar { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                string endpoint = "api/vehiculos";

                if (!string.IsNullOrWhiteSpace(Buscar))
                {
                    Buscar = Validaciones.SanitizarTexto(Buscar, 100);
                    endpoint = $"api/vehiculos?buscar={Uri.EscapeDataString(Buscar)}";
                }

                var lista = await _httpClient.GetFromJsonAsync<List<Vehiculo>>(endpoint);
                ListaVehiculos = lista ?? new List<Vehiculo>();
            }
            catch
            {
                ListaVehiculos = new List<Vehiculo>();
                TempData["Error"] = "No se pudo cargar la lista de vehículos.";
            }
        }

        public string GenerarToken(int id)
        {
            return _protector.Protect(id.ToString());
        }

        public async Task<IActionResult> OnPostEliminarAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    TempData["Error"] = "No se pudo identificar el vehículo seleccionado.";
                    return RedirectToPage();
                }

                string textoId = _protector.Unprotect(token);

                if (!int.TryParse(textoId, out int id) || !Validaciones.EsIdValido(id))
                {
                    TempData["Error"] = "No se pudo identificar el vehículo seleccionado.";
                    return RedirectToPage();
                }

                var respuesta = await _httpClient.DeleteAsync($"api/vehiculos/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El vehículo fue eliminado correctamente.";
                }
                else if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El vehículo ya no está disponible.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el vehículo. Inténtelo nuevamente.";
                }
            }
            catch
            {
                TempData["Error"] = "No se pudo eliminar el vehículo. Inténtelo nuevamente.";
            }

            return RedirectToPage();
        }
    }
}