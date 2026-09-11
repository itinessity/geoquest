static void Check(bool value, string message)
{
    if (!value)
        throw new Exception(message);
}

Check(Geo.Distance(0, 0, 0, 0) == 0, "identical locations");
Check(Math.Abs(Geo.Distance(0, 179, 0, -179) - 222.39016) < .01, "antimeridian");
Check(Geo.Distance(90, 0, 90, 180) < .00001, "north pole");
Check(Math.Abs(Geo.Distance(0, 0, 0, 180) - 20015.1144) < .01, "antipodes");
Check(Math.Abs(Geo.Distance(48.8566, 2.3522, 51.5074, -.1278) - 343.556) < 1, "Paris to London");
Console.WriteLine("5 spatial checks passed.");
