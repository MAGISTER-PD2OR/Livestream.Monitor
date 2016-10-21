using System;
using System.Collections.Generic;
using System.Linq;
using ExternalAPIs.Beam.Pro;
using ExternalAPIs.Hitbox;
using ExternalAPIs.TwitchTv;
using ExternalAPIs.Youtube;
using Livestream.Monitor.Core;

namespace Livestream.Monitor.Model.ApiClients
{
    public class ApiClientFactory : IApiClientFactory
    {
        private readonly TwitchApiClient twitchApiClient;
        private readonly YoutubeApiClient youtubeApiClient;
        private readonly HitboxApiClient hitboxApiClient;
        private readonly BeamProApiClient beamProClient;

        private readonly List<IApiClient> apiClients = new List<IApiClient>();

        public ApiClientFactory(ISettingsHandler settingsHandler)
        {
            if (settingsHandler == null) throw new ArgumentNullException(nameof(settingsHandler));

            twitchApiClient = new TwitchApiClient(new TwitchTvReadonlyClient(), settingsHandler);
            youtubeApiClient = new YoutubeApiClient(new YoutubeReadonlyClient());
            hitboxApiClient = new HitboxApiClient(new HitboxReadonlyClient());
            beamProClient = new BeamProApiClient(new BeamProReadonlyClient());

            apiClients.Add(twitchApiClient);
            apiClients.Add(youtubeApiClient);
            apiClients.Add(hitboxApiClient);
            apiClients.Add(beamProClient);
        }

        public T Get<T>() where T : IApiClient
        {
            var apiClient = apiClients.FirstOrDefault(x => x.GetType() == typeof (T));
            if (apiClient == null)
                throw new ArgumentException($"{typeof(T).Name} is not a stream provider.");

            return (T) apiClient;
        }

        public IEnumerable<IApiClient> GetAll()
        {
            return apiClients;
        }

        public IApiClient GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            switch (name)
            {
                case YoutubeApiClient.API_NAME:
                    return youtubeApiClient;
                case TwitchApiClient.API_NAME:
                    return twitchApiClient;
                case HitboxApiClient.API_NAME:
                    return hitboxApiClient;
                case BeamProApiClient.API_NAME:
                    return beamProClient;
                default:
                    return null;
            }
        }
    }
}