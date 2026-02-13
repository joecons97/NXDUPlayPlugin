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

        entry.Name = entry.Name
            .Replace("®", "")
            .Replace("™", "");

        var response = await igdb.QueryAsync<Game>(IGDBClient.Endpoints.Games, $"search \"{entry.Name}\"; fields involved_companies.developer, involved_companies.publisher, involved_companies.company.name, screenshots.url, genres.name, release_dates.date, summary;");
        Debug.Log($"Found {response.Length} games with name {entry.Name} in IGDB");

        //Let's make a (bad) assumption that the oldest entry is a correct one
        var game = response
            .OrderBy(x => x.Id)
            .FirstOrDefault();

        if (game == null)
            return null;

        var screenshots = game.Screenshots.Values.Select(x => x.Url.Replace("thumb", "720p")).ToArray();
        var developers = game.InvolvedCompanies.Values.Where(x => x.Developer == true).Select(x => x.Company.Value.Name).ToArray();
        var publishers = game.InvolvedCompanies.Values.Where(x => x.Publisher == true).Select(x => x.Company.Value.Name).ToArray();
        var genres = game.Genres.Values.Select(x => x.Name).ToArray();
        var releaseDateTimeOffset = game.ReleaseDates.Values.Last().Date;

        DateTime? releaseDate = releaseDateTimeOffset.HasValue 
            ? releaseDateTimeOffset.Value.DateTime
            : null;

        var result = new AdditionalMetadata(game.Summary, screenshots, developers, publishers, genres, releaseDate);

        return result;
    }
}
