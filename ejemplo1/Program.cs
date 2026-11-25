using ejemplo1.config;
using ejemplo1.Factories;
using ejemplo1.Interfaces;
using ejemplo1.models;
using ejemplo1.Repositories;
using TallerMecanicoSOLID.Factories;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios de API, Razor Pages y Data Protection
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddRazorPages();
builder.Services.AddDataProtection();

// 2. Cadena de conexión y Singleton
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión DefaultConnection.");

ConfiguracionBD configuracionBD = ConfiguracionBD.ObtenerInstancia(connectionString);
builder.Services.AddSingleton(configuracionBD);

// 3. Factory Method y Repositorios
builder.Services.AddScoped<EntidadFactory<Cliente>, ClienteFactory>();
builder.Services.AddScoped<IRepositorio<Cliente>, RepositorioCliente>();

// 4. HttpClient asíncrono hacia la API (http://localhost:5251/)
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5251/";
builder.Services.AddHttpClient("ApiTaller", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.EndsWith("/") ? apiBaseUrl : apiBaseUrl + "/");
});

var app = builder.Build();

// 5. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();