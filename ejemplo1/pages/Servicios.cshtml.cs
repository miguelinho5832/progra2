using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
{
    public class ServiciosModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProtector _protector;

        public ServiciosModel(
            IHttpClientFactory httpClientFactory,
            IDataProtectionProvider provider)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _protector = provider.CreateProtector("ejemplo1.Servicios");
        }

        public List<Servicio> ListaServicios { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Buscar { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Buscar))
                {
                    var servicios = await _httpClient.GetFromJsonAsync<List<Servicio>>("api/servicios");
                    ListaServicios = servicios ?? new List<Servicio>();
                }
                else
                {
                    Buscar = Validaciones.SanitizarTexto(Buscar, 100);
                    string termino = Uri.EscapeDataString(Buscar);

                    var respuestaBuscar = await _httpClient.GetAsync($"api/servicios/buscar?q={termino}");
                    if (respuestaBuscar.IsSuccessStatusCode)
                    {
                        var encontrados = await respuestaBuscar.Content.ReadFromJsonAsync<List<Servicio>>();
                        ListaServicios = encontrados ?? new List<Servicio>();
                    }
                    else
                    {
                        var todos = await _httpClient.GetFromJsonAsync<List<Servicio>>("api/servicios")
                                    ?? new List<Servicio>();

                        string filtro = Buscar.Trim().ToLowerInvariant();
                        ListaServicios = todos.Where(s =>
                            (!string.IsNullOrWhiteSpace(s.Nombre) && s.Nombre.ToLowerInvariant().Contains(filtro)) ||
                            (!string.IsNullOrWhiteSpace(s.Tipo) && s.Tipo.ToLowerInvariant().Contains(filtro)) ||
                            (!string.IsNullOrWhiteSpace(s.Descripcion) && s.Descripcion.ToLowerInvariant().Contains(filtro))
                        ).ToList();
                    }
                }
            }
            catch
            {
                ListaServicios = new List<Servicio>();
                TempData["Error"] = "No se pudo cargar el catálogo de servicios desde la API.";
            }
        }

        public string GenerarToken(int id)
        {
            return _protector.Protect(id.ToString());
        }

        public async Task<IActionResult> OnPostCambiarEstadoAsync(string token, bool nuevoEstado)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "No se pudo identificar el servicio seleccionado.";
                return RedirectToPage();
            }

            try
            {
                string textoId = _protector.Unprotect(token);

                if (!int.TryParse(textoId, out int id) || !Validaciones.EsIdValido(id))
                {
                    TempData["Error"] = "No se pudo identificar el servicio seleccionado.";
                    return RedirectToPage();
                }

                var payload = new Dictionary<string, object?>
                {
                    ["estado"] = nuevoEstado
                };

                var respuesta = await _httpClient.PatchAsJsonAsync($"api/servicios/{id}", payload);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = nuevoEstado
                        ? "El servicio fue activado correctamente."
                        : "El servicio fue desactivado correctamente.";
                }
                else if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El servicio ya no está disponible.";
                }
                else
                {
                    TempData["Error"] = "No se pudo actualizar el estado del servicio.";
                }
            }
            catch
            {
                TempData["Error"] = "No se pudo actualizar el estado del servicio. Inténtelo nuevamente.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEliminarAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "No se pudo identificar el servicio seleccionado.";
                return RedirectToPage();
            }

            try
            {
                string textoId = _protector.Unprotect(token);

                if (!int.TryParse(textoId, out int id) || !Validaciones.EsIdValido(id))
                {
                    TempData["Error"] = "No se pudo identificar el servicio seleccionado.";
                    return RedirectToPage();
                }

                var respuesta = await _httpClient.DeleteAsync($"api/servicios/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El servicio fue eliminado correctamente.";
                }
                else if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El servicio ya no está disponible.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el servicio. Inténtelo nuevamente.";
                }
            }
            catch
            {
                TempData["Error"] = "No se pudo eliminar el servicio. Inténtelo nuevamente.";
            }

            return RedirectToPage();
        }
    }
}