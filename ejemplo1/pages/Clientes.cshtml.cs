using System.Net;
using System.Net.Http.Json;
using ejemplo1.models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
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
                Buscar = (Buscar ?? "").Trim();

                string ruta = string.IsNullOrWhiteSpace(Buscar)
                    ? "api/clientes"
                    : $"api/clientes?buscar={Uri.EscapeDataString(Buscar)}";

                var resultado = await _httpClient.GetFromJsonAsync<List<Cliente>>(ruta);

                ListaClientes = (resultado ?? new List<Cliente>())
                    .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(c => c.Id)
                    .ToList();
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
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Token de cliente no válido.";
                return RedirectToPage();
            }

            try
            {
                string textoId = _protector.Unprotect(token);
                if (!int.TryParse(textoId, out int id) || id <= 0)
                {
                    TempData["Error"] = "Identificador de cliente no válido.";
                    return RedirectToPage();
                }

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