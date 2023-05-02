using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SlackNet;
using SlackNet.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using SpotMySlack;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AppData>();
builder.Services.AddHostedService<StatusUpdateService>();

builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("Spotify"));
builder.Services.Configure<SlackSettings>(builder.Configuration.GetSection("Slack"));
builder.Services.AddSlackNet(c =>
{
    c.UseApiToken(builder.Configuration.GetSection("Slack:Token").Value);

});

var app = builder.Build();

app.MapGet("/hello", async context =>
{
    await context.Response.WriteAsync("Hello World!");
});
app.MapGet("/", async (HttpContext context) =>
{
    var appData = context.RequestServices.GetService<AppData>();
    var spotifyOptions = context.RequestServices.GetService<IOptions<SpotifySettings>>();

    if (!string.IsNullOrWhiteSpace(context.Request.Query["code"]))
    {
        var code = context.Request.Query["code"];
        var response = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(spotifyOptions.Value.ClientId, spotifyOptions.Value.ClientSecret, code,
                new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}"))
        );

        appData.IsAuthorized = true;
        appData.AccessToken = response.AccessToken;
        appData.RefreshToken = response.RefreshToken;

        return Results.Ok();
    }

    if (appData.IsAuthorized)
    {
        return Results.Ok();
    }
    var loginRequest = new LoginRequest(
        new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}"),
        spotifyOptions.Value.ClientId,
        LoginRequest.ResponseType.Code
    )
    {
        Scope = new[]
        {
            Scopes.UserReadCurrentlyPlaying
        }
    };
    return Results.Redirect(loginRequest.ToUri().ToString());
});

app.Run();

public class SpotifySettings
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }
}

public class SlackSettings
{
    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Token { get; set; }

    public string UserId { get; set; }
}