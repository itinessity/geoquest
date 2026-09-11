using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

static class Geo
{
    public static double Distance(double a, double b, double c, double d)
    {
        double R(double x) => x * Math.PI / 180;
        var v = Math.Pow(Math.Sin(R(c - a) / 2), 2) + Math.Cos(R(a)) * Math.Cos(R(c)) * Math.Pow(Math.Sin(R(d - b) / 2), 2);
        return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(v, 0, 1)));
    }
}
