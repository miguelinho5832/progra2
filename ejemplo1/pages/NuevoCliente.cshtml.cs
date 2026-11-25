using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using ejemplo1.Factories;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ejemplo1.pages
{
    public class NuevoClienteModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ClienteFactory _clienteFactory;

        public NuevoClienteModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _clienteFactory = new ClienteFactory();
        }

        [BindProperty]
        [Required(ErrorMessage = "El número de CI / NIT es obligatorio.")]
        public string? NumeroCi { get; set; }

        [BindProperty]
        public string? ExtensionCi { get; set; }

        [BindProperty]
        public Cliente Cliente { get; set; } = new();

        public void OnGet()
        {
            Cliente = _clienteFactory.Crear();
            Cliente.FechaRegistro = DateTime.Now;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Cliente ??= _clienteFactory.Crear();

            Cliente.Nombre = CapitalizarTexto(LimpiarTexto(Cliente.Nombre, 50));
            Cliente.Apellido = CapitalizarTexto(LimpiarTexto(Cliente.Apellido, 50));
            NumeroCi = (NumeroCi ?? "").Trim();
            ExtensionCi = (ExtensionCi ?? "").Trim().ToUpperInvariant();

            string[] extensionesValidas = { "CB", "LP", "SC", "OR", "PT", "TJ", "CH", "BE", "PA" };

            // models/Cliente.cs exige solo dígitos y máximo 10 caracteres en CiNit
            Cliente.CiNit = NumeroCi;

            Cliente.Telefono = LimpiarTexto(Cliente.Telefono, 8);
            Cliente.Email = LimpiarTexto(Cliente.Email, 50).ToLowerInvariant();
            Cliente.Direccion = LimpiarTexto(Cliente.Direccion, 255);
            Cliente.FechaRegistro = DateTime.Now;

            ModelState.Clear();
            TryValidateModel(Cliente, nameof(Cliente));

            if (string.IsNullOrWhiteSpace(NumeroCi))
                ModelState.AddModelError("NumeroCi", "El número de CI / NIT es obligatorio.");
            else if (!NumeroCi.All(char.IsDigit))
                ModelState.AddModelError("NumeroCi", "El número de CI / NIT solo puede contener dígitos.");
            else if (NumeroCi.Length > 10)
                ModelState.AddModelError("NumeroCi", "El CI / NIT no puede superar 10 caracteres.");

            if (!string.IsNullOrEmpty(ExtensionCi) && !extensionesValidas.Contains(ExtensionCi))
                ModelState.AddModelError("ExtensionCi", "Seleccione una extensión de CI válida.");

            if (string.IsNullOrWhiteSpace(Cliente.Nombre))
                ModelState.AddModelError("Cliente.Nombre", "El nombre es obligatorio.");
            else if (Cliente.Nombre.Length < 2)
                ModelState.AddModelError("Cliente.Nombre", "El nombre debe tener al menos 2 caracteres.");
            else if (!SoloLetrasYEspacios(Cliente.Nombre))
                ModelState.AddModelError("Cliente.Nombre", "El nombre solo puede contener letras y espacios.");

            if (string.IsNullOrWhiteSpace(Cliente.Apellido))
                ModelState.AddModelError("Cliente.Apellido", "El apellido es obligatorio.");
            else if (Cliente.Apellido.Length < 2)
                ModelState.AddModelError("Cliente.Apellido", "El apellido debe tener al menos 2 caracteres.");
            else if (!SoloLetrasYEspacios(Cliente.Apellido))
                ModelState.AddModelError("Cliente.Apellido", "El apellido solo puede contener letras y espacios.");

            if (string.IsNullOrWhiteSpace(Cliente.Telefono))
                ModelState.AddModelError("Cliente.Telefono", "El teléfono es obligatorio.");
            else if (Cliente.Telefono.Length < 6)
                ModelState.AddModelError("Cliente.Telefono", "El teléfono debe tener al menos 6 caracteres.");
            else if (!Cliente.Telefono.All(char.IsDigit))
                ModelState.AddModelError("Cliente.Telefono", "El teléfono solo debe contener dígitos.");

            if (string.IsNullOrWhiteSpace(Cliente.Email))
                ModelState.AddModelError("Cliente.Email", "El correo electrónico es obligatorio.");
            else if (!new EmailAddressAttribute().IsValid(Cliente.Email))
                ModelState.AddModelError("Cliente.Email", "Ingrese un correo electrónico válido.");

            if (string.IsNullOrWhiteSpace(Cliente.Direccion))
                ModelState.AddModelError("Cliente.Direccion", "La dirección es obligatoria.");

            if (!ModelState.IsValid)
                return Page();

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
                    ModelState.AddModelError("NumeroCi", "Ya existe un cliente registrado con ese CI / NIT.");
                    return Page();
                }

                var detalleError = await respuesta.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"No se pudo registrar el cliente (HTTP {(int)respuesta.StatusCode}). {detalleError}");
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error de comunicación con la API: {ex.Message}");
                return Page();
            }
        }

        private static string LimpiarTexto(string? valor, int maxLongitud)
        {
            if (string.IsNullOrWhiteSpace(valor)) return "";
            var limpio = valor.Trim();
            return limpio.Length > maxLongitud ? limpio[..maxLongitud] : limpio;
        }

        private static string CapitalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < partes.Length; i++)
            {
                var p = partes[i].ToLowerInvariant();
                partes[i] = char.ToUpperInvariant(p[0]) + p[1..];
            }
            return string.Join(" ", partes);
        }

        private static bool SoloLetrasYEspacios(string texto) =>
            texto.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
    }
}