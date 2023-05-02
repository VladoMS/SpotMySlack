using Microsoft.Extensions.Options;
using SlackNet;
using SpotifyAPI.Web;

namespace SpotMySlack;

public class StatusUpdateService : BackgroundService
{
    private readonly ILogger<StatusUpdateService> _logger;

    private readonly AppData _appData;

    private readonly ISlackApiClient _slackApiClient;

    private readonly SlackSettings _slackSettings;

    private readonly SpotifySettings _spotifySettings;

    public StatusUpdateService(ILogger<StatusUpdateService> logger,
        AppData appData,
        ISlackApiClient slackApiClient,
        IOptions<SlackSettings> slackSettings,
        IOptions<SpotifySettings> spotifySettings)
    {
        _logger = logger;
        _appData = appData;
        _slackApiClient = slackApiClient;
        _slackSettings = slackSettings.Value;
        _spotifySettings = spotifySettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var spotifyCLient = new SpotifyClient(_appData.AccessToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await spotifyCLient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                if (item.IsPlaying && item.CurrentlyPlayingType == "track")
                {
                    var track = item.Item as FullTrack;

                    var url = "";
                    track.ExternalUrls.TryGetValue("spotify", out url);

                    _logger.LogInformation($"Playing {string.Join(",", track.Artists.Select(x => x.Name).ToArray())} - {track.Name} {url}");

                    var status = Truncate($"{string.Join(",", track.Artists.Select(x => x.Name).ToArray())} - {track.Name}", 96);
                    if (status.Length > 100)
                    {
                        
                    }
                    await _slackApiClient.UserProfile.Set(new UserProfile()
                    {
                        
                        StatusEmoji = ":rainbowpls:", StatusText = status
                    }, _slackSettings.UserId);
                }
                else
                {
                    _logger.LogInformation("Nothing is playing");

                    await _slackApiClient.UserProfile.Set(new UserProfile()
                    {
                        StatusEmoji = ":kumapls:", StatusText = $""
                    }, _slackSettings.UserId);
                }
            }
            catch (APIUnauthorizedException ex)
            {
                var newResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(_spotifySettings.ClientId, _spotifySettings.ClientSecret, (_appData.RefreshToken)
                ));
                _appData.IsAuthorized = true;
                _appData.AccessToken = newResponse.AccessToken;
                _appData.RefreshToken = newResponse.RefreshToken;
                _logger.LogInformation($"{ex.GetType()} - {ex.Message}");
                
                spotifyCLient = new SpotifyClient(_appData.AccessToken);
            }
            catch (APIException ex)
            {
                spotifyCLient = new SpotifyClient(_appData.AccessToken);
                _logger.LogInformation("Trying to update spotify client");
                _logger.LogInformation($"{ex.GetType()} - {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _ = _slackApiClient.UserProfile.Set(new UserProfile()
        {
            StatusEmoji = "", StatusText = ""
        }, _slackSettings.UserId).Result;
    }
    
    private string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "..."; 
    }
}