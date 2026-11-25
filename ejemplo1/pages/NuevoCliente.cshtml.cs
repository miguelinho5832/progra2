using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
{
    public class NuevoClienteModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public NuevoClienteModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
        }

        [BindProperty]
        [Required(ErrorMessage = "El número de CI es obligatorio.")]
        public string? NumeroCi { get; set; }

        [BindProperty]
        public string? ExtensionCi { get; set; }

        [BindProperty]
        public Cliente Cliente { get; set; } = new();

        public void OnGet()
        {
            Cliente = new Cliente
            {
                Estado = true
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Cliente.Nombre = CapitalizarTexto(SanitizarTexto(Cliente.Nombre, 80));
            Cliente.Apellido = CapitalizarTexto(SanitizarTexto(Cliente.Apellido, 80));

            NumeroCi = (NumeroCi ?? "").Trim();
            ExtensionCi = (ExtensionCi ?? "").Trim().ToUpperInvariant();

            string[] extensionesCi = { "CB", "LP", "SC", "OR", "PT", "TJ", "CH", "BE", "PA" };

            Cliente.CiNit = ExtensionCi.Length > 0
                ? $"{NumeroCi}-{ExtensionCi}"
                : NumeroCi;

            Cliente.Telefono = (Cliente.Telefono ?? "").Trim();
            Cliente.Email = SanitizarTexto(Cliente.Email, 120).Trim().ToLowerInvariant();
            Cliente.Direccion = SanitizarTexto(Cliente.Direccion, 200);

            ModelState.Clear();
            TryValidateModel(Cliente, nameof(Cliente));

            if (string.IsNullOrWhiteSpace(NumeroCi))
            {
                ModelState.AddModelError("NumeroCi", "El número de CI es obligatorio.");
            }
            else if (!NumeroCi.All(char.IsDigit))
            {
                ModelState.AddModelError("NumeroCi", "El número de CI debe contener solo dígitos.");
            }

            if (ExtensionCi.Length > 0 && !extensionesCi.Contains(ExtensionCi))
            {
                ModelState.AddModelError("ExtensionCi", "Seleccione una extensión válida.");
            }

            if (!string.IsNullOrWhiteSpace(Cliente.Nombre) && !SoloLetrasYEspacios(Cliente.Nombre))
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre solo puede contener letras y espacios.");
            }

            if (!string.IsNullOrWhiteSpace(Cliente.Apellido) && !SoloLetrasYEspacios(Cliente.Apellido))
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido solo puede contener letras y espacios.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Telefono))
            {
                ModelState.AddModelError("Cliente.Telefono", "El teléfono es obligatorio.");
            }

            if (!string.IsNullOrWhiteSpace(Cliente.Email) &&
                !new EmailAddressAttribute().IsValid(Cliente.Email))
            {
                ModelState.AddModelError("Cliente.Email", "Ingrese un correo electrónico con un formato válido.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Direccion))
            {
                ModelState.AddModelError("Cliente.Direccion", "La dirección es obligatoria.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var respuesta = await _httpClient.PostAsJsonAsync("api/clientes", Cliente);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El cliente fue registrado correctamente.";
                    return RedirectToPage("/Clientes");
                }

                if (respuesta.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError("NumeroCi", "Ya existe un cliente registrado con ese CI/NIT.");
                    return Page();
                }

                ModelState.AddModelError("", "No se pudo registrar el cliente. Inténtelo nuevamente.");
                return Page();
            }
            catch
            {
                ModelState.AddModelError("", "Ocurrió un problema al comunicarse con la API.");
                return Page();
            }
        }

        private static string SanitizarTexto(string? texto, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var limpio = texto.Trim();
            return limpio.Length > maxLength ? limpio[..maxLength] : limpio;
        }

        private static string CapitalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < palabras.Length; i++)
            {
                var p = palabras[i].ToLowerInvariant();
                palabras[i] = char.ToUpperInvariant(p[0]) + p[1..];
            }
            return string.Join(" ", palabras);
        }

        private static bool SoloLetrasYEspacios(string texto)
        {
            return texto.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
        }
    }
}