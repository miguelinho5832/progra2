using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.pages
{
    public class NuevoVehiculoModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly EntidadFactory<Vehiculo> _factory;

        public NuevoVehiculoModel(
            IHttpClientFactory httpClientFactory,
            EntidadFactory<Vehiculo> factory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _factory = factory;
        }

        [BindProperty]
        public Vehiculo Vehiculo { get; set; } = null!;

        public List<Cliente> ClientesDisponibles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Vehiculo = _factory.Crear();
            Vehiculo.Estado = true;

            await CargarClientesAsync();
        }

        private async Task CargarClientesAsync()
        {
            try
            {
                var clientes = await _httpClient.GetFromJsonAsync<List<Cliente>>("api/clientes");
                ClientesDisponibles = (clientes ?? new List<Cliente>())
                    .OrderBy(c => c.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(c => c.Apellido, StringComparer.CurrentCultureIgnoreCase)
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

            Vehiculo.Placa = Validaciones.FormatearPlaca(Vehiculo.Placa);
            Vehiculo.Marca = Validaciones.SanitizarTexto(Vehiculo.Marca, 60);
            Vehiculo.Modelo = Validaciones.SanitizarTexto(Vehiculo.Modelo, 60);
            Vehiculo.Color = Validaciones.SanitizarTexto(Vehiculo.Color, 40);
            Vehiculo.Tipo = Validaciones.SanitizarTexto(Vehiculo.Tipo, 40);

            ModelState.Clear();
            TryValidateModel(Vehiculo, nameof(Vehiculo));

            // 1. Placa
            if (!string.IsNullOrWhiteSpace(Vehiculo.Placa) &&
                !Validaciones.ValidarPlaca(Vehiculo.Placa))
            {
                ModelState.AddModelError(
                    "Vehiculo.Placa",
                    "La placa debe contener 3 o 4 números seguidos de 3 letras. Ejemplo: 5214BEX.");
            }

            // 2. Marca
            string[] marcasPermitidas =
            {
                "Toyota",
                "Nissan",
                "Suzuki",
                "Ford",
                "Chevrolet",
                "Hyundai",
                "Kia",
                "Mazda",
                "Pico"
            };

            if (!string.IsNullOrWhiteSpace(Vehiculo.Marca))
            {
                if (!Validaciones.ValidarMarca(Vehiculo.Marca) ||
                    !marcasPermitidas.Contains(Vehiculo.Marca))
                {
                    ModelState.AddModelError(
                        "Vehiculo.Marca",
                        "Seleccione una marca de vehículo válida.");
                }
            }

            // 3. Modelo
            if (!string.IsNullOrWhiteSpace(Vehiculo.Modelo) &&
                !Validaciones.ValidarModelo(Vehiculo.Modelo))
            {
                ModelState.AddModelError(
                    "Vehiculo.Modelo",
                    "El modelo solo puede contener letras, números, espacios y guiones.");
            }

            // 4. Año
            if (Vehiculo.Anio.HasValue &&
                (Vehiculo.Anio.Value < 1901 || !Validaciones.ValidarAnio(Vehiculo.Anio.Value)))
            {
                ModelState.AddModelError(
                    "Vehiculo.Anio",
                    "Ingrese un año válido.");
            }

            // 5. Color
            string[] coloresPermitidos =
            {
                "Blanco",
                "Negro",
                "Gris",
                "Plata",
                "Rojo",
                "Azul",
                "Verde",
                "Amarillo",
                "Marrón",
                "Otro"
            };

            if (!string.IsNullOrWhiteSpace(Vehiculo.Color))
            {
                if (!Validaciones.LetrasNumerosEspacios(Vehiculo.Color) ||
                    !coloresPermitidos.Contains(Vehiculo.Color))
                {
                    ModelState.AddModelError(
                        "Vehiculo.Color",
                        "Seleccione un color válido.");
                }
            }

            // 6. Tipo
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

            if (!string.IsNullOrWhiteSpace(Vehiculo.Tipo) &&
                !tiposPermitidos.Contains(Vehiculo.Tipo))
            {
                ModelState.AddModelError(
                    "Vehiculo.Tipo",
                    "Seleccione un tipo de vehículo válido.");
            }

            // 7. Kilometraje
            if (Vehiculo.Kilometraje.HasValue &&
                !Validaciones.ValidarKilometraje(Vehiculo.Kilometraje.Value))
            {
                ModelState.AddModelError(
                    "Vehiculo.Kilometraje",
                    "El kilometraje debe estar entre 0 y 2.000.000.");
            }

            // 8. Cliente
            await CargarClientesAsync();

            if (!Validaciones.EsIdValido(Vehiculo.ClienteId))
            {
                ModelState.AddModelError(
                    "Vehiculo.ClienteId",
                    "Debe seleccionar un cliente.");
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
                var respuesta = await _httpClient.PostAsJsonAsync("api/vehiculos", Vehiculo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El vehículo fue registrado correctamente.";
                    return RedirectToPage("/Vehiculos");
                }

                if (respuesta.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "Vehiculo.Placa",
                        "Ya existe un vehículo registrado con esa placa.");
                    return Page();
                }

                string detalleError = await respuesta.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(detalleError) &&
                    (detalleError.Contains("placa", StringComparison.OrdinalIgnoreCase) ||
                     detalleError.Contains("1062") ||
                     detalleError.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(
                        "Vehiculo.Placa",
                        "Ya existe un vehículo registrado con esa placa o la placa no es válida.");
                    return Page();
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo registrar el vehículo. Inténtelo nuevamente.");
                return Page();
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "Ocurrió un problema inesperado al registrar el vehículo. Inténtelo nuevamente.");
                return Page();
            }
        }
    }
}