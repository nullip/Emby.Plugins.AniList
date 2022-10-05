using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Configuration;
using Emby.Anime;

//API v2
namespace Emby.Plugins.AniList
{
    public class AniListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _paths;
        private readonly ILogger _log;
        private readonly Api _api;
        public int Order => 8;
        public string Name => "AniList";

        public AniListSeriesProvider(IApplicationPaths appPaths, IHttpClient httpClient, ILogManager logManager, IJsonSerializer jsonSerializer)
        {
            _log = logManager.GetLogger("AniList");
            _httpClient = httpClient;
            _api = new Api(_log, httpClient, jsonSerializer);
            _paths = appPaths;
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var aid = info.GetProviderId(ProviderNames.AniList);
            if (string.IsNullOrEmpty(aid))
            {
                _log.Info("Start AniList... Searching(" + info.Name + ")");
                aid = await _api.FindSeries(info.Name, cancellationToken);
            }

            if (!string.IsNullOrEmpty(aid))
            {
                RootObject WebContent = await _api.WebRequestAPI(_api.AniList_anime_link.Replace("{0}", aid), cancellationToken);
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.Name = _api.SelectName(WebContent, info.MetadataLanguage);
                result.Item.OriginalTitle = WebContent.data.Media.title.native;

                result.People = await _api.GetPersonInfo(WebContent.data.Media.id, cancellationToken);
                result.Item.SetProviderId(ProviderNames.AniList, aid);
                result.Item.Overview = WebContent.data.Media.description;
                try
                {
                    StartDate startDate = WebContent.data.Media.startDate;
                    DateTime date = new DateTime(startDate.year, startDate.month, startDate.day);
                    date = date.ToUniversalTime();
                    result.Item.PremiereDate = date;
                    result.Item.ProductionYear = date.Year;
                }
                catch (Exception) { }
                try
                {
                    EndDate endDate = WebContent.data.Media.endDate;
                    DateTime date = new DateTime(endDate.year, endDate.month, endDate.day);
                    date = date.ToUniversalTime();
                    result.Item.EndDate = date;
                }
                catch (Exception) { }
                try
                {
                    string status = WebContent.data.Media.status;
                    if (status == Status.RELEASING || status == Status.NOT_YET_RELEASED)
                    {
                        result.Item.Status = SeriesStatus.Continuing;
                    }
                    else
                    {
                        result.Item.Status = SeriesStatus.Ended;
                    }
                }
                catch (Exception) { }
                try
                    //AniList has a max rating of 5
                    result.Item.CommunityRating = (WebContent.data.Media.averageScore / 10);
                }
                catch (Exception) { }
                foreach (var genre in _api.Get_Genre(WebContent))
                    result.Item.AddGenre(genre);
                GenreHelper.CleanupGenres(result.Item);
            }
            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, RemoteSearchResult>();

            var aid = searchInfo.GetProviderId(ProviderNames.AniList);
            if (!string.IsNullOrEmpty(aid))
            {
                if (!results.ContainsKey(aid))
                    results.Add(aid, await _api.GetAnime(aid, cancellationToken));
            }

            if (!string.IsNullOrEmpty(searchInfo.Name))
            {
                List<string> ids = await _api.Search_GetSeries_list(searchInfo.Name, cancellationToken);
                foreach (string a in ids)
                {
                    results.Add(a, await _api.GetAnime(a, cancellationToken));
                }
            }

            return results.Values;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }

    public class AniListSeriesImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly Api _api;
        public AniListSeriesImageProvider(ILogManager logManager, IHttpClient httpClient, IApplicationPaths appPaths, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _api = new Api(logManager.GetLogger(Name), httpClient, jsonSerializer);
        }

        public string Name => "AniList";

        public bool Supports(BaseItem item) => item is Series || item is Season;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var seriesId = item.GetProviderId(ProviderNames.AniList);
            return GetImages(seriesId, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(string aid, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrEmpty(aid))
            {
                var primary = _api.Get_ImageUrl(await _api.WebRequestAPI(_api.AniList_anime_link.Replace("{0}", aid), cancellationToken));
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = primary
                });
            }
            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}