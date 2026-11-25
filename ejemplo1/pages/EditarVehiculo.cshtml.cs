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
    public class EditarVehiculoModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly EntidadFactory<Vehiculo> _factory;
        private readonly IDataProtector _protector;

        public EditarVehiculoModel(
            IHttpClientFactory httpClientFactory,
            EntidadFactory<Vehiculo> factory,
            IDataProtectionProvider provider)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _factory = factory;
            _protector = provider.CreateProtector("ejemplo1.Vehiculos");
        }

        [BindProperty]
        public Vehiculo Vehiculo { get; set; } = null!;

        [BindProperty]
        public string Token { get; set; } = "";

        public List<Cliente> ClientesDisponibles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Vehiculo = _factory.Crear();

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "El enlace de edición no es válido.";
                return RedirectToPage("/Vehiculos");
            }

            int vehiculoId;

            try
            {
                string textoId = _protector.Unprotect(id);
                if (!int.TryParse(textoId, out vehiculoId) || !Validaciones.EsIdValido(vehiculoId))
                {
                    TempData["Error"] = "El enlace de edición no es válido.";
                    return RedirectToPage("/Vehiculos");
                }
            }
            catch
            {
                TempData["Error"] = "El enlace de edición no es válido.";
                return RedirectToPage("/Vehiculos");
            }

            try
            {
                var respuesta = await _httpClient.GetAsync($"api/vehiculos/{vehiculoId}");

                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El vehículo ya no está disponible.";
                    return RedirectToPage("/Vehiculos");
                }

                respuesta.EnsureSuccessStatusCode();

                var vehiculo = await respuesta.Content.ReadFromJsonAsync<Vehiculo>();
                if (vehiculo == null)
                {
                    TempData["Error"] = "El vehículo ya no está disponible.";
                    return RedirectToPage("/Vehiculos");
                }

                Vehiculo = vehiculo;
                Token = id;

                await CargarClientesAsync();
                return Page();
            }
            catch
            {
                TempData["Error"] = "No se pudo cargar la información del vehículo.";
                return RedirectToPage("/Vehiculos");
            }
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>("api/clientes");
                ClientesDisponibles = (clientes ?? new List<Cliente>())
                    .Where(cliente => cliente.Estado)
                    .ToList();
            }
            catch
            {
                ClientesDisponibles = new List<Cliente>();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Vehiculo ??= _factory.Crear();

            if (string.IsNullOrWhiteSpace(Token))
            {
                TempData["Error"] = "El enlace de edición no es válido.";
                return RedirectToPage("/Vehiculos");
            }

            int vehiculoId;

            try
            {
                string textoId = _protector.Unprotect(Token);
                if (!int.TryParse(textoId, out vehiculoId) || !Validaciones.EsIdValido(vehiculoId))
                {
                    TempData["Error"] = "El enlace de edición no es válido.";
                    return RedirectToPage("/Vehiculos");
                }

                Vehiculo.Id = vehiculoId;
            }
            catch
            {
                TempData["Error"] = "El enlace de edición no es válido.";
                return RedirectToPage("/Vehiculos");
            }

            Vehiculo.Placa = Validaciones.FormatearPlaca(Vehiculo.Placa);
            Vehiculo.Marca = Validaciones.SanitizarTexto(Vehiculo.Marca, 60);
            Vehiculo.Modelo = Validaciones.SanitizarTexto(Vehiculo.Modelo, 60);
            Vehiculo.Color = Validaciones.SanitizarTexto(Vehiculo.Color, 40);
            Vehiculo.Tipo = Validaciones.SanitizarTexto(Vehiculo.Tipo, 40);

            ModelState.Clear();
            TryValidateModel(Vehiculo, nameof(Vehiculo));

            if (!Validaciones.ValidarPlaca(Vehiculo.Placa))
            {
                ModelState.AddModelError(
                    "Vehiculo.Placa",
                    "La placa debe contener 3 o 4 números seguidos de 3 letras. Ejemplo: 5214BEX.");
            }

            if (!Validaciones.ValidarMarca(Vehiculo.Marca))
            {
                ModelState.AddModelError(
                    "Vehiculo.Marca",
                    "La marca solo puede contener letras, espacios y guiones.");
            }

            if (!Validaciones.ValidarModelo(Vehiculo.Modelo))
            {
                ModelState.AddModelError(
                    "Vehiculo.Modelo",
                    "El modelo solo puede contener letras, números, espacios y guiones.");
            }

            if (string.IsNullOrWhiteSpace(Vehiculo.Color) ||
                !Validaciones.LetrasNumerosEspacios(Vehiculo.Color))
            {
                ModelState.AddModelError(
                    "Vehiculo.Color",
                    "Ingrese un color válido.");
            }

            string[] tiposPermitidos =
            {
                "Automóvil",
                "Camioneta",
                "Motocicleta",
                "Camión",
                "Sedán",
                "SUV",
                "Otro"
            };

            if (!tiposPermitidos.Contains(Vehiculo.Tipo))
            {
                ModelState.AddModelError(
                    "Vehiculo.Tipo",
                    "Seleccione un tipo de vehículo válido.");
            }

            await CargarClientesAsync();

            if (!Validaciones.EsIdValido(Vehiculo.ClienteId))
            {
                ModelState.AddModelError(
                    "Vehiculo.ClienteId",
                    "Debe seleccionar un cliente válido.");
            }
            else
            {
                bool clienteValido = ClientesDisponibles.Any(c => c.Id == Vehiculo.ClienteId);
                if (!clienteValido)
                {
                    ModelState.AddModelError(
                        "Vehiculo.ClienteId",
                        "El cliente seleccionado no está disponible.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var respuesta = await _httpClient.PutAsJsonAsync($"api/vehiculos/{vehiculoId}", Vehiculo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El vehículo fue actualizado correctamente.";
                    return RedirectToPage("/Vehiculos");
                }

                if (respuesta.StatusCode == HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "El vehículo ya no está disponible.";
                    return RedirectToPage("/Vehiculos");
                }

                if (respuesta.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "Vehiculo.Placa",
                        "Ya existe otro vehículo registrado con esa placa.");
                    return Page();
                }

                string detalleError = await respuesta.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(detalleError) &&
                    detalleError.Contains("placa", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Vehiculo.Placa",
                        "Ya existe otro vehículo registrado con esa placa o la placa no es válida.");
                    return Page();
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo actualizar el vehículo. Inténtelo nuevamente.");
                return Page();
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "Ocurrió un problema inesperado al actualizar el vehículo. Inténtelo nuevamente.");
                return Page();
            }
        }
    }
}