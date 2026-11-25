using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ejemplo1.models;

namespace ejemplo1.Pages
{
    public class ClientesModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProtector _protector;

        public ClientesModel(
            IHttpClientFactory httpClientFactory,
            IDataProtectionProvider provider)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _protector = provider.CreateProtector("ejemplo1.Clientes");
        }

        public List<Cliente> ListaClientes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Buscar { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                string ruta = string.IsNullOrWhiteSpace(Buscar)
                    ? "api/clientes"
                    : $"api/clientes?buscar={Uri.EscapeDataString(Buscar.Trim())}";

                if (!string.IsNullOrWhiteSpace(Buscar))
                {
                    Buscar = Buscar.Trim();
                }

                var resultado = await _httpClient.GetFromJsonAsync<List<Cliente>>(ruta);
                ListaClientes = resultado ?? new List<Cliente>();
            }
            catch
            {
                ListaClientes = new List<Cliente>();
                TempData["Error"] = "No se pudo consultar la lista de clientes desde la API.";
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
                string textoId = _protector.Unprotect(token);
                int id = Convert.ToInt32(textoId);

                var respuesta = await _httpClient.DeleteAsync($"api/clientes/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El cliente fue eliminado correctamente.";
                }
                else if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el cliente o ya fue eliminado.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el cliente desde la API.";
                }
            }
            catch
            {
                TempData["Error"] = "No se pudo procesar la eliminación del cliente.";
            }

            return RedirectToPage();
        }
    }
}