using System.Net;
using System.Net.Http.Json;
using ejemplo1.config;
using ejemplo1.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TallerMecanicoSOLID.Factories;

namespace ejemplo1.pages
{
    public class NuevoServicioModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly EntidadFactory<Servicio> _factory;

        public NuevoServicioModel(
            IHttpClientFactory httpClientFactory,
            EntidadFactory<Servicio> factory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiTaller");
            _factory = factory;
        }

        [BindProperty]
        public Servicio Servicio { get; set; } = null!;

        public void OnGet()
        {
            Servicio = _factory.Crear();
            Servicio.Estado = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Servicio ??= _factory.Crear();

            Servicio.Nombre = Validaciones.SanitizarTexto(Servicio.Nombre, 120);
            Servicio.Descripcion = Validaciones.SanitizarTexto(Servicio.Descripcion, 255);
            Servicio.Nombre = Validaciones.Capitalizar(Servicio.Nombre);
            Servicio.UnidadDuracion = Validaciones.SanitizarTexto(Servicio.UnidadDuracion, 30);

            switch (Servicio.Tipo?.Trim())
            {
                case "Mantenimiento preventivo":
                case "preventivo":
                    Servicio.Tipo = "preventivo";
                    break;

                case "Mantenimiento correctivo":
                case "correctivo":
                    Servicio.Tipo = "correctivo";
                    break;

                case "Revisión simple":
                case "Revision simple":
                case "revision_simple":
                    Servicio.Tipo = "revision_simple";
                    break;

                default:
                    Servicio.Tipo = "";
                    break;
            }

            ModelState.Clear();
            TryValidateModel(Servicio, nameof(Servicio));

            string[] tiposPermitidos =
            {
                "preventivo",
                "correctivo",
                "revision_simple"
            };

            if (string.IsNullOrWhiteSpace(Servicio.Tipo) ||
                !tiposPermitidos.Contains(Servicio.Tipo))
            {
                ModelState.AddModelError(
                    "Servicio.Tipo",
                    "Debe seleccionar un tipo de servicio válido.");
            }

            if (string.IsNullOrWhiteSpace(Servicio.Nombre))
            {
                ModelState.AddModelError(
                    "Servicio.Nombre",
                    "El nombre es obligatorio.");
            }
            else if (Servicio.Nombre.Length > 120)
            {
                ModelState.AddModelError(
                    "Servicio.Nombre",
                    "El nombre no puede superar 120 caracteres.");
            }
            else if (!Validaciones.LetrasNumerosEspacios(Servicio.Nombre))
            {
                ModelState.AddModelError(
                    "Servicio.Nombre",
                    "El nombre solo puede contener letras, números y espacios.");
            }

            if (string.IsNullOrWhiteSpace(Servicio.Descripcion))
            {
                ModelState.AddModelError(
                    "Servicio.Descripcion",
                    "La descripción es obligatoria.");
            }
            else if (Servicio.Descripcion.Length > 255)
            {
                ModelState.AddModelError(
                    "Servicio.Descripcion",
                    "La descripción no puede superar 255 caracteres.");
            }

            if (!Servicio.Precio.HasValue ||
                Servicio.Precio.Value <= 0 ||
                Servicio.Precio.Value > 99999999.99m)
            {
                ModelState.AddModelError(
                    "Servicio.Precio",
                    "El precio debe ser mayor a 0.");
            }

            if (!Servicio.DuracionEstimada.HasValue ||
                Servicio.DuracionEstimada.Value <= 0 ||
                Servicio.DuracionEstimada.Value > 100000)
            {
                ModelState.AddModelError(
                    "Servicio.DuracionEstimada",
                    "La duración estimada debe ser mayor a 0.");
            }

            string[] unidadesPermitidas =
            {
                "Minutos",
                "Horas",
                "Dias"
            };

            if (string.IsNullOrWhiteSpace(Servicio.UnidadDuracion) ||
                !unidadesPermitidas.Contains(Servicio.UnidadDuracion))
            {
                ModelState.AddModelError(
                    "Servicio.UnidadDuracion",
                    "Debe seleccionar una unidad de duración.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var respuesta = await _httpClient.PostAsJsonAsync("api/servicios", Servicio);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "El servicio fue registrado correctamente.";
                    return RedirectToPage("/Servicios");
                }

                if (respuesta.StatusCode == HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "Servicio.Nombre",
                        "Ya existe un servicio con ese nombre y tipo.");
                    return Page();
                }

                if (respuesta.StatusCode == HttpStatusCode.BadRequest)
                {
                    string detalleError = await respuesta.Content.ReadAsStringAsync();
                    if (detalleError.Contains("1062") ||
                        detalleError.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            "Servicio.Nombre",
                            "Ya existe un servicio con ese nombre y tipo.");
                    }
                    else
                    {
                        ModelState.AddModelError(
                            "",
                            "Revise los datos ingresados del servicio. No se pudo completar el registro.");
                    }

                    return Page();
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo registrar el servicio. Inténtelo nuevamente.");

                return Page();
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "Ocurrió un problema de comunicación al registrar el servicio. Inténtelo nuevamente.");

                return Page();
            }
        }
    }
}