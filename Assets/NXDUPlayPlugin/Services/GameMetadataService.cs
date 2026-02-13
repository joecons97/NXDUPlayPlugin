using Cysharp.Threading.Tasks;
using IGDB;
using IGDB.Models;
using LibraryPlugin;
using System;
using System.Linq;
using UnityEngine;

public class GameMetadataService
{
    private string CLIENT_ID = "hbz0opkx4t9ryatxpwqns5q1s1lckn";
    private string SECRET_KEY = "txerihsuld8ok3f9dwbc332aigiv7p";

    public async UniTask<AdditionalMetadata> GetGameMetadata(LibraryEntry entry)
    {
        var igdb = new IGDBClient(CLIENT_ID, SECRET_KEY);

        var name = entry.Name
            .Replace("®", "")
            .Replace("™", "");

        var response = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, $"search \"{name}\"; fields involved_companies.developer, involved_companies.publisher, involved_companies.company.name, screenshots.url, genres.name, release_dates.date, summary;");
        Debug.Log($"Found {response.Length} games with name {name} in IGDB");

        //Let's make a (bad) assumption that the oldest entry is a correct one
        var game = response
            .OrderBy(x => x.Id)
            .FirstOrDefault();

        if (game == null)
            return null;

        var screenshots = game.Screenshots?.Values?
            .Select(x => x.Url?.Replace("thumb", "720p"))
            .Where(x => x != null)
            .ToArray() ?? Array.Empty<string>();
        var developers = game.InvolvedCompanies?.Values?
            .Where(x => x.Developer == true && x.Company?.Value?.Name != null)
            .Select(x => x.Company.Value.Name)
            .ToArray() ?? Array.Empty<string>();
        var publishers = game.InvolvedCompanies?.Values?
            .Where(x => x.Publisher == true && x.Company?.Value?.Name != null)
            .Select(x => x.Company.Value.Name)
            .ToArray() ?? Array.Empty<string>();
        var genres = game.Genres?.Values?
            .Select(x => x.Name)
            .Where(x => x != null)
            .ToArray() ?? Array.Empty<string>();
        var releaseDateTimeOffset = game.ReleaseDates?.Values?.LastOrDefault()?.Date;

        DateTime? releaseDate = releaseDateTimeOffset?.DateTime;

        var result = new AdditionalMetadata(game.Summary, screenshots, developers, publishers, genres, releaseDate);

        return result;
    }
}
