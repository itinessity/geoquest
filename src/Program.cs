using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

var app = Setup.Create(args);
var catalog = new Catalog(Setup.Http(app), Setup.Fixture(app));
var sessions = new ConcurrentDictionary<string, Game>();
app.MapPost("/api/games", async (HttpContext ctx) =>
{
    foreach (var old in sessions.Where(x => x.Value.Expires < DateTimeOffset.UtcNow))
        sessions.TryRemove(old.Key, out _);
    if (sessions.Count >= 1000)
        return Results.Json(new { error = "Game capacity reached. Please try later." }, statusCode: 503);
    var photos = await catalog.Get();
    if (photos.Length < 5)
        return Results.Json(new { error = "Not enough licensed photographs are available. Please retry later." }, statusCode: 503);
    var game = new Game(photos.OrderBy(_ => Random.Shared.Next()).Take(5).ToArray());
    var key = Guid.NewGuid().ToString("N");
    sessions[key] = game;
    ctx.Response.Cookies.Append("geoquest", key, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = ctx.Request.IsHttps, MaxAge = TimeSpan.FromMinutes(30) });
    return Results.Ok(game.View());
});
Game? Current(HttpContext ctx) => ctx.Request.Cookies.TryGetValue("geoquest", out var key) && sessions.TryGetValue(key, out var g) && g.Expires > DateTimeOffset.UtcNow ? g : null;
app.MapGet("/api/game", (HttpContext ctx) => Current(ctx)is { } g ? Results.Ok(g.View()) : Results.Json(new { error = "Start a new game. Sessions last 30 minutes." }, statusCode: 404));
app.MapPost("/api/guess", (HttpContext ctx, Guess guess) =>
{
    var g = Current(ctx);
    if (g is null)
        return Results.Json(new { error = "Your game expired. Start a new game." }, statusCode: 404);
    lock (g)
    {
        if (g.Index >= 5 || g.Answered || guess.RoundId != g.RoundId)
            return Results.Conflict(new { error = "This round was already answered or is no longer current." });
        if (!double.IsFinite(guess.Latitude) || !double.IsFinite(guess.Longitude) || Math.Abs(guess.Latitude) > 90 || Math.Abs(guess.Longitude) > 180)
            throw new ArgumentException("Coordinates are outside latitude/longitude bounds.");
        var p = g.Photos[g.Index];
        var km = Geo.Distance(guess.Latitude, guess.Longitude, p.Lat, p.Lon);
        var score = (int)Math.Round(5000 * Math.Exp(-km / 2000));
        g.Results.Add(new Answer(p.Title, p.Lat, p.Lon, Math.Round(km, 1), score, guess.Latitude, guess.Longitude));
        g.Answered = true;
        return Results.Ok(g.View());
    }
});
app.MapPost("/api/next", (HttpContext ctx) =>
{
    var g = Current(ctx);
    if (g is null)
        return Results.NotFound(new { error = "Game expired." });
    lock (g)
    {
        if (!g.Answered)
            return Results.Conflict(new { error = "Submit a guess first." });
        g.Index++;
        g.Answered = false;
        g.RoundId = Guid.NewGuid().ToString("N");
        return Results.Ok(g.View());
    }
});
app.Run();
