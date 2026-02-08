using craftersmine.SteamGridDBNet;
using Cysharp.Threading.Tasks;
using System.Linq;

public class GameArtworkService
{
    private const string KEY = "eac60eb72325b5e1fc6a16466f0a041c";
    private readonly SteamGridDb client = new(KEY);

    public async UniTask<string> GetCoverUrlAsync(string name)
    {
        var games = await client.SearchForGamesAsync(name);
        if(games.Length == 0)
            return null;

        var game = games.First();
        var grids = await client.GetGridsForGameAsync(game, dimensions: SteamGridDbDimensions.W600H900, types: SteamGridDbTypes.Static, limit: 1);
        if (grids.Length == 0)
            return null;

        return grids.First().FullImageUrl;
    }
}
