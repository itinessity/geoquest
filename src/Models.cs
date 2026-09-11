using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

record Guess(string RoundId, double Latitude, double Longitude);
record Photo(string Title, string Image, string Page, string Artist, string License, string LicenseUrl, double Lat, double Lon);
record Answer(string Title, double Latitude, double Longitude, double DistanceKm, int Score, double GuessLatitude, double GuessLongitude);
