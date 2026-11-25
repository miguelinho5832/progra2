using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.pages
{
    public class EditarClienteModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly IDataProtector _protector;
        private readonly EntidadFactory<Cliente> _clienteFactory;

        public EditarClienteModel(
            IHttpClientFactory httpClientFactory,
            IDataProtectionProvider provider,
            EntidadFactory<Cliente> clienteFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _protector = provider.CreateProtector("ejemplo1.Clientes");
            _clienteFactory = clienteFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string? Id { get; set; }

        [BindProperty]
        public string? TokenActual { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "El número de CI / NIT es obligatorio.")]
        public string? NumeroCi { get; set; }

        [BindProperty]
        public string? ExtensionCi { get; set; }

        [BindProperty]
        public Cliente Cliente { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Id) || !TryObtenerId(Id, out int clienteId))
            {
                TempData["Error"] = "El enlace para editar el cliente no es válido.";
                return RedirectToPage("/Clientes");
            }

            try
            {
                var respuesta = await _httpClient.GetAsync($"api/clientes/{clienteId}");
                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el cliente solicitado.";
                    return RedirectToPage("/Clientes");
                }

                if (!respuesta.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudieron cargar los datos del cliente desde la API.";
                    return RedirectToPage("/Clientes");
                }

                var clienteObtenido = await respuesta.Content.ReadFromJsonAsync<Cliente>();
                if (clienteObtenido is null)
                {
                    TempData["Error"] = "No se encontró el cliente solicitado.";
                    return RedirectToPage("/Clientes");
                }

                Cliente = clienteObtenido;
                if (Cliente.FechaRegistro == default)
                {
                    Cliente.FechaRegistro = DateTime.Now;
                }

                TokenActual = Id;
                SepararCiNit(Cliente.CiNit);
                return Page();
            }
            catch
            {
                TempData["Error"] = "No se pudieron cargar los datos del cliente desde la API.";
                return RedirectToPage("/Clientes");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var token = !string.IsNullOrWhiteSpace(TokenActual) ? TokenActual : Id;
            if (string.IsNullOrWhiteSpace(token) || !TryObtenerId(token, out int clienteId))
            {
                TempData["Error"] = "Token de cliente no válido o expirado.";
                return RedirectToPage("/Clientes");
            }

            TokenActual = token;

            DateTime fechaRegistroOriginal = Cliente?.FechaRegistro ?? default;

            Cliente ??= _clienteFactory.Crear();
            Cliente.Id = clienteId;
            Cliente.FechaRegistro = fechaRegistroOriginal == default ? DateTime.Now : fechaRegistroOriginal;

            Cliente.Nombre = Validaciones.Capitalizar(Validaciones.SanitizarTexto(Cliente.Nombre ?? "", 50));
            Cliente.Apellido = Validaciones.Capitalizar(Validaciones.SanitizarTexto(Cliente.Apellido ?? "", 50));
            NumeroCi = Validaciones.SanitizarTexto(NumeroCi ?? "", 10);
            ExtensionCi = Validaciones.SanitizarTexto(ExtensionCi ?? "", 5).ToUpperInvariant();

            string[] extensionesValidas = { "CB", "LP", "SC", "OR", "PT", "TJ", "CH", "BE", "PA" };

            // models/Cliente.cs exige solo dígitos y máximo 10 caracteres en CiNit
            Cliente.CiNit = NumeroCi;

            Cliente.Telefono = Validaciones.SanitizarTexto(Cliente.Telefono ?? "", 8);
            Cliente.Email = Validaciones.SanitizarTexto(Cliente.Email ?? "", 50).ToLowerInvariant();
            Cliente.Direccion = Validaciones.SanitizarTexto(Cliente.Direccion ?? "", 255);

            ModelState.Clear();
            TryValidateModel(Cliente, nameof(Cliente));

            if (string.IsNullOrWhiteSpace(NumeroCi))
            {
                ModelState.AddModelError("NumeroCi", "El número de CI / NIT es obligatorio.");
            }
            else if (NumeroCi.Length > 10)
            {
                ModelState.AddModelError("NumeroCi", "El CI / NIT no puede superar 10 caracteres.");
            }
            else if (!Validaciones.ValidarCiNit(NumeroCi))
            {
                ModelState.AddModelError("NumeroCi", "El número de CI / NIT solo puede contener dígitos.");
            }

            if (!string.IsNullOrEmpty(ExtensionCi) && !extensionesValidas.Contains(ExtensionCi))
            {
                ModelState.AddModelError("ExtensionCi", "Seleccione una extensión de CI válida.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Nombre))
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre es obligatorio.");
            }
            else if (Cliente.Nombre.Length < 2)
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre debe tener al menos 2 caracteres.");
            }
            else if (!Validaciones.SoloLetras(Cliente.Nombre))
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre solo puede contener letras y espacios.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Apellido))
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido es obligatorio.");
            }
            else if (Cliente.Apellido.Length < 2)
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido debe tener al menos 2 caracteres.");
            }
            else if (!Validaciones.SoloLetras(Cliente.Apellido))
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido solo puede contener letras y espacios.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Telefono))
            {
                ModelState.AddModelError("Cliente.Telefono", "El teléfono es obligatorio.");
            }
            else if (Cliente.Telefono.Length < 6 || Cliente.Telefono.Length > 8)
            {
                ModelState.AddModelError("Cliente.Telefono", "El teléfono debe tener entre 6 y 8 dígitos.");
            }
            else if (!Validaciones.ValidarTelefono(Cliente.Telefono))
            {
                ModelState.AddModelError("Cliente.Telefono", "El teléfono solo debe contener dígitos.");
            }

            if (string.IsNullOrWhiteSpace(Cliente.Email))
            {
                ModelState.AddModelError("Cliente.Email", "El correo electrónico es obligatorio.");
            }
            else if (Cliente.Email.Length > 50)
            {
                ModelState.AddModelError("Cliente.Email", "El correo no puede superar 50 caracteres.");
            }
            else if (!Validaciones.ValidarEmail(Cliente.Email))
            {
                ModelState.AddModelError("Cliente.Email", "Ingrese un correo electrónico válido.");
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
                var respuesta = await _httpClient.PutAsJsonAsync($"api/clientes/{clienteId}", Cliente);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El cliente fue actualizado correctamente.";
                    return RedirectToPage("/Clientes");
                }

                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El cliente ya no existe en el sistema.";
                    return RedirectToPage("/Clientes");
                }

                if (respuesta.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError("NumeroCi", "Ya existe otro cliente con ese CI / NIT.");
                    return Page();
                }

                if (respuesta.StatusCode == HttpStatusCode.BadRequest)
                {
                    string detalleError = await respuesta.Content.ReadAsStringAsync();
                    if (detalleError.Contains("1062") ||
                        detalleError.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NumeroCi", "Ya existe otro cliente con ese CI / NIT.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Revise los datos ingresados del cliente. No se pudo actualizar.");
                    }
                    return Page();
                }

                ModelState.AddModelError("", "No se pudo actualizar el cliente. Inténtelo nuevamente.");
                return Page();
            }
            catch
            {
                ModelState.AddModelError("", "Ocurrió un problema de comunicación con la API al actualizar el cliente.");
                return Page();
            }
        }

        private bool TryObtenerId(string token, out int id)
        {
            id = 0;
            try
            {
                var texto = _protector.Unprotect(token);
                return int.TryParse(texto, out id) && Validaciones.EsIdValido(id);
            }
            catch
            {
                return false;
            }
        }

        private void SepararCiNit(string? ciNit)
        {
            if (string.IsNullOrWhiteSpace(ciNit))
            {
                NumeroCi = "";
                ExtensionCi = "";
                return;
            }

            var limpio = Validaciones.SanitizarTexto(ciNit, 20);
            if (limpio.Contains('-'))
            {
                var partes = limpio.Split('-', 2, StringSplitOptions.TrimEntries);
                NumeroCi = partes[0];
                ExtensionCi = partes.Length > 1 ? partes[1].ToUpperInvariant() : "";
                return;
            }

            var partesEspacio = limpio.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            NumeroCi = partesEspacio[0];
            ExtensionCi = partesEspacio.Length > 1 ? partesEspacio[1].ToUpperInvariant() : "";
        }
    }
}