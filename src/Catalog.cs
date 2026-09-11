using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

sealed class Catalog(HttpClient http, bool fixture)
{
    Photo[] cached = [];
    DateTimeOffset until;
    readonly SemaphoreSlim gate = new(1);
    readonly (double, double)[] places = [(48.8584, 2.2945), (51.5007, -0.1246), (40.6892, -74.0445), (27.1751, 78.0421), (30.3285, 35.4444), (-33.8568, 151.2153), (-22.9519, -43.2105), (35.6586, 139.7454), (41.8902, 12.4922), (20.6843, -88.5678), (-13.1631, -72.545), (29.9792, 31.1342), (41.4036, 2.1744), (37.9715, 23.7267), (41.0086, 28.9802), (37.8199, -122.4783), (43.6426, -79.3871), (-33.9628, 18.4098), (1.2834, 103.8607), (25.1972, 55.2744), (52.5163, 13.3777), (55.7525, 37.6231), (39.9163, 116.3972), (37.5796, 126.9770), (22.2759, 114.1455), (47.5070, 19.0450), (50.0865, 14.4114), (38.6916, -9.2160), (48.1845, 16.3122), (55.6929, 12.5993), (59.3275, 18.0544), (60.1704, 24.9522), (53.3438, -6.2546), (55.9486, -3.1999), (64.1417, -21.9266), (38.8899, -77.0091)];
    public async Task<Photo[]> Get()
    {
        await gate.WaitAsync();
        try
        {
            if (until > DateTimeOffset.UtcNow)
                return cached;
            if (fixture)
            {
                cached = places.Select((p, i) => new Photo("Test location " + i, "/fixture.svg", "https://commons.wikimedia.org/", "Test fixture", "CC0", "https://creativecommons.org/publicdomain/zero/1.0/", p.Item1, p.Item2)).ToArray();
            }
            else
            {
                var found = new List<Photo>();
                foreach (var(lat, lon)in places)
                {
                    try
                    {
                        var url = FormattableString.Invariant($"https://commons.wikimedia.org/w/api.php?action=query&generator=geosearch&ggscoord={lat}%7C{lon}&ggsradius=300&ggsnamespace=6&ggslimit=5&prop=imageinfo%7Ccoordinates&iiprop=url%7Cextmetadata%7Cmime&iiurlwidth=900&format=json&formatversion=2");
                        using var json = JsonDocument.Parse(await http.GetStringAsync(url));
                        if (!json.RootElement.TryGetProperty("query", out var query))
                            continue;
                        foreach (var p in query.GetProperty("pages").EnumerateArray())
                        {
                            if (!p.TryGetProperty("coordinates", out var coords) || !p.TryGetProperty("imageinfo", out var infos))
                                continue;
                            var info = infos[0];
                            var mime = info.GetProperty("mime").GetString();
                            if (mime != "image/jpeg" && mime != "image/png")
                                continue;
                            var meta = info.GetProperty("extmetadata");
                            string Field(string key) => meta.TryGetProperty(key, out var v) ? WebUtility.HtmlDecode(Regex.Replace(v.GetProperty("value").GetString() ?? "", "<[^>]*>", "")) : "";
                            var license = Field("LicenseShortName");
                            if (!(license.StartsWith("CC BY") || license == "CC0" || license == "Public domain"))
                                continue;
                            var image = info.TryGetProperty("thumburl", out var thumb) ? thumb.GetString() : info.GetProperty("url").GetString();
                            var coord = coords[0];
                            found.Add(new Photo(p.GetProperty("title").GetString()!, image!, info.GetProperty("descriptionurl").GetString()!, Field("Artist"), license, Field("LicenseUrl"), coord.GetProperty("lat").GetDouble(), coord.GetProperty("lon").GetDouble()));
                            break;
                        }
                    }
                    catch (HttpRequestException)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (JsonException)
                    {
                    }
                }

                cached = found.ToArray();
            }

            until = DateTimeOffset.UtcNow.AddHours(cached.Length >= 5 ? 12 : 0);
            return cached;
        }
        finally
        {
            gate.Release();
        }
    }
}
