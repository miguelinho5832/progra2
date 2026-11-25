using System.ComponentModel.DataAnnotations;
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

        public async Task<IActionResult> OnPostActualizarRapidoAsync(
            string token,
            string? telefono,
            string? email,
            string? direccion)
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

                string telLimpio = (telefono ?? "").Trim();
                string emailLimpio = (email ?? "").Trim();
                string dirLimpia = (direccion ?? "").Trim();

                if (telLimpio.Length < 6 || telLimpio.Length > 8)
                {
                    TempData["Error"] = "El teléfono debe tener entre 6 y 8 caracteres.";
                    return RedirectToPage();
                }

                if (!string.IsNullOrWhiteSpace(emailLimpio) &&
                    !new EmailAddressAttribute().IsValid(emailLimpio))
                {
                    TempData["Error"] = "El correo electrónico no tiene un formato válido.";
                    return RedirectToPage();
                }

                var payload = new
                {
                    telefono = telLimpio,
                    email = emailLimpio,
                    direccion = dirLimpia
                };

                var respuesta = await _httpClient.PatchAsJsonAsync($"api/clientes/{id}", payload);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "Datos de contacto actualizados correctamente.";
                }
                else if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el cliente o ya fue eliminado.";
                }
                else
                {
                    TempData["Error"] = "No se pudieron actualizar los datos del cliente.";
                }
            }
            catch
            {
                TempData["Error"] = "No se pudo procesar la actualización rápida del cliente.";
            }

            return RedirectToPage();
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