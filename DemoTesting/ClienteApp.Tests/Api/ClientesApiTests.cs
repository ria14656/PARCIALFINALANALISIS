using System.Net;
using System.Net.Http.Json;
using ClienteApp.Core.Data;
using ClienteApp.Core.Models;
using ClienteApp.Core.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClienteApp.Tests.Api;

public class ClientesApiTests
{
    [Fact]
    public async Task ObtenerPorId_ClienteExistente_DebeRetornar200()
    {
        using var factory = new ClienteApiFactory();
        using HttpClient client = factory.CreateClient();

        using var registro = await client.PostAsJsonAsync(
            "/api/Clientes",
            new
            {
                Nombre = "Ana López",
                Correo = "ana@example.com"
            });

        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        var esperado = await registro.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.NotNull(esperado);

        using var response = await client.GetAsync(
            $"/api/Clientes/{esperado.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var resultado = await response.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public async Task ObtenerPorId_ClienteInexistente_DebeRetornar404()
    {
        using var factory = new ClienteApiFactory();
        using HttpClient client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/Clientes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content
            .ReadFromJsonAsync<ErrorRespuesta>();

        Assert.NotNull(error);
        Assert.Equal("Cliente no encontrado.", error.Mensaje);
    }

    [Fact]
    public async Task Registrar_ClienteValido_DebeRetornar201()
    {
        using var factory = new ClienteApiFactory();
        using HttpClient client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "Ana López",
            Correo = "ana@example.com"
        };

        using var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var cliente = await response.Content
            .ReadFromJsonAsync<ClienteRespuesta>();

        Assert.NotNull(cliente);
        Assert.Equal(solicitud.Nombre, cliente.Nombre);
        Assert.Equal(solicitud.Correo, cliente.Correo);
    }

    [Fact]
    public async Task Registrar_NombreVacio_DebeRetornar400()
    {
        using var factory = new ClienteApiFactory();
        using HttpClient client = factory.CreateClient();

        var solicitud = new
        {
            Nombre = "",
            Correo = "correo@example.com"
        };

        using var response = await client.PostAsJsonAsync(
            "/api/Clientes",
            solicitud);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record ErrorRespuesta(string Mensaje);

    private sealed record ClienteRespuesta(
        int Id,
        string Nombre,
        string Correo);
}

public sealed class ClienteApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"clientes_api_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var cadena = $"Data Source={_dbPath};Pooling=False";

        builder.UseSetting("ConnectionStrings:ClientesDb", cadena);

        builder.ConfigureServices(services =>
        {
            Database.Inicializar(cadena);

            // Usar exclusivamente el repositorio de esta prueba.
            services.RemoveAll<IClienteRepository>();

            services.AddScoped<IClienteRepository>(
                _ => new ClienteRepository(cadena));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}