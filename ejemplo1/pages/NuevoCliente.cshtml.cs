using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.pages
{
    public class NuevoClienteModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly EntidadFactory<Cliente> _clienteFactory;

        public NuevoClienteModel(
            IHttpClientFactory httpClientFactory,
            EntidadFactory<Cliente> clienteFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _clienteFactory = clienteFactory;
        }

        [BindProperty]
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

            Cliente.Nombre = Validaciones.Capitalizar(
                Validaciones.SanitizarTexto(Cliente.Nombre ?? "", 50));
            Cliente.Apellido = Validaciones.Capitalizar(
                Validaciones.SanitizarTexto(Cliente.Apellido ?? "", 50));

            NumeroCi = Validaciones.SanitizarTexto(NumeroCi ?? "", 10);
            ExtensionCi = Validaciones.SanitizarTexto(ExtensionCi ?? "", 5).ToUpperInvariant();

            // Cliente.cs exige solo dígitos y máximo 10 caracteres en CiNit
            Cliente.CiNit = NumeroCi;

            Cliente.Telefono = Validaciones.SanitizarTexto(Cliente.Telefono ?? "", 8);
            Cliente.Email = Validaciones.SanitizarTexto(Cliente.Email ?? "", 50).ToLowerInvariant();
            Cliente.Direccion = Validaciones.SanitizarTexto(Cliente.Direccion ?? "", 255);
            Cliente.FechaRegistro = DateTime.Now;

            ModelState.Clear();
            TryValidateModel(Cliente, nameof(Cliente));

            string[] extensionesValidas = { "CB", "LP", "SC", "OR", "PT", "TJ", "CH", "BE", "PA" };

            // 1. Nombre
            if (string.IsNullOrWhiteSpace(Cliente.Nombre))
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre es obligatorio.");
            }
            else if (Cliente.Nombre.Length < 2 || Cliente.Nombre.Length > 50)
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre debe tener entre 2 y 50 caracteres.");
            }
            else if (!Validaciones.SoloLetras(Cliente.Nombre))
            {
                ModelState.AddModelError("Cliente.Nombre", "El nombre solo puede contener letras y espacios.");
            }

            // 2. Apellido
            if (string.IsNullOrWhiteSpace(Cliente.Apellido))
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido es obligatorio.");
            }
            else if (Cliente.Apellido.Length < 2 || Cliente.Apellido.Length > 50)
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido debe tener entre 2 y 50 caracteres.");
            }
            else if (!Validaciones.SoloLetras(Cliente.Apellido))
            {
                ModelState.AddModelError("Cliente.Apellido", "El apellido solo puede contener letras y espacios.");
            }

            // 3. Número de CI / NIT
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

            // 4. Extensión CI (opcional)
            if (!string.IsNullOrEmpty(ExtensionCi) && !extensionesValidas.Contains(ExtensionCi))
            {
                ModelState.AddModelError("ExtensionCi", "Seleccione una extensión de CI válida.");
            }

            // 5. Teléfono
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

            // 6. Correo electrónico
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

            // 7. Dirección
            if (string.IsNullOrWhiteSpace(Cliente.Direccion))
            {
                ModelState.AddModelError("Cliente.Direccion", "La dirección es obligatoria.");
            }
            else if (Cliente.Direccion.Length < 3 || Cliente.Direccion.Length > 255)
            {
                ModelState.AddModelError("Cliente.Direccion", "La dirección debe tener entre 3 y 255 caracteres.");
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
                    ModelState.AddModelError("NumeroCi", "Ya existe un cliente registrado con ese CI / NIT.");
                    return Page();
                }

                if (respuesta.StatusCode == HttpStatusCode.BadRequest)
                {
                    string detalleError = await respuesta.Content.ReadAsStringAsync();
                    if (detalleError.Contains("1062") ||
                        detalleError.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("NumeroCi", "Ya existe un cliente registrado con ese CI / NIT.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Revise los datos ingresados del cliente. No se pudo completar el registro.");
                    }
                    return Page();
                }

                ModelState.AddModelError("", "No se pudo registrar el cliente. Inténtelo nuevamente.");
                return Page();
            }
            catch
            {
                ModelState.AddModelError("", "Ocurrió un problema de comunicación al registrar el cliente. Inténtelo nuevamente.");
                return Page();
            }
        }
    }
}