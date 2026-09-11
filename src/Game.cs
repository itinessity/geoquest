using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

sealed class Game(Photo[] photos)
{
    public Photo[] Photos = photos;
    public int Index;
    public bool Answered;
    public string RoundId = Guid.NewGuid().ToString("N");
    public DateTimeOffset Expires = DateTimeOffset.UtcNow.AddMinutes(30);
    public List<Answer> Results = [];
    public object View()
    {
        lock (this)
        {
            var p = Index < 5 ? Photos[Index] : null;
            return new
            {
                round = Index + 1,
                roundId = RoundId,
                complete = Index >= 5,
                answered = Answered,
                total = Results.Sum(x => x.Score),
                expires = Expires,
                photo = p is null ? null : new
                {
                    p.Image,
                    p.Page,
                    p.Artist,
                    p.License,
                    p.LicenseUrl
                },
                answers = Results.ToArray()
            };
        }
    }
}
