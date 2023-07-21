﻿using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Emby.Anime;
using MediaBrowser.Common.Net;
using System.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Logging;
using System.Xml;
using System.Drawing;

namespace Emby.Plugins.AniList
{
    /// <summary>
    /// Based on the new API from AniList
    /// 🛈 This code works with the API Interface (v2) from AniList
    /// 🛈 https://anilist.gitbook.io/anilist-apiv2-docs
    /// 🛈 THIS IS AN UNOFFICAL API INTERFACE FOR EMBY
    /// </summary>
    public class Api
    {
        private static IJsonSerializer _jsonSerializer;
        private const string SearchLink = @"https://graphql.anilist.co/api/v2?query=
query ($query: String, $type: MediaType) {
  Page {
    media(search: $query, type: $type) {
      id
      title {
        romaji
        english
        native
      }
      coverImage {
        medium
        large
      }
      format
      type
      averageScore
      popularity
      episodes
      duration
      season
      hashtag
      isAdult
      startDate {
        year
        month
        day
      }
      endDate {
        year
        month
        day
      }
    }
  }
}&variables={ ""query"":""{0}"",""type"":""ANIME""}";
        public string AniList_anime_link = @"https://graphql.anilist.co/api/v2?query=
query($id: Int!, $type: MediaType) {
    Media(id: $id, type: $type) {
        id
        title {
            romaji
            english
            native
            userPreferred
        }
        startDate {
            year
            month
            day
        }
        endDate {
            year
            month
            day
        }
        coverImage {
            large
            medium
        }
        bannerImage
        format
        type
        status
        episodes
        duration
        chapters
        volumes
        season
        description
        averageScore
        meanScore
        genres
        synonyms
        nextAiringEpisode {
            airingAt
            timeUntilAiring
            episode
        }
        studios {
            edges {
                node {
                    name
                }
            }
        }
        trailer {
            id
            site
        }
        tags {
            name
        }
    }
}&variables={ ""id"":""{0}"",""type"":""ANIME""}";
        private const string AniList_anime_char_link = @"https://graphql.anilist.co/api/v2?query=
query($id: Int!, $type: MediaType, $staffLanguage: StaffLanguage, $page: Int = 1) {
  Media(id: $id, type: $type) {
    id
    characters(page: $page, sort: [ROLE]) {
      pageInfo {
        total
        perPage
        hasNextPage
        currentPage
        lastPage
      }
      edges {
        node {
          id
          name {
            first
            last
          }
          image {
            medium
            large
          }
        }
        role
        voiceActors(language: $staffLanguage, sort: [ROLE]) {
          id
          name {
            first
            last
            native
          }
          image {
            medium
            large
          }
          language
        }
      }
    }
  }
}&variables={ ""id"":""{0}"",""type"":""ANIME"",""staffLanguage"":""JAPANESE""}";

        private IHttpClient _httpClient;
        private ILogger _logger;

        public Api(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }
        /// <summary>
        /// API call to get the anime with the id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<RemoteSearchResult> GetAnime(string id, CancellationToken cancellationToken)
        {
            RootObject WebContent = await WebRequestAPI(AniList_anime_link.Replace("{0}", id), cancellationToken);

            var result = new RemoteSearchResult
            {
                Name = ""
            };

            result.SearchProviderName = WebContent.data.Media.title.romaji;
            result.ImageUrl = WebContent.data.Media.coverImage.large;
            result.SetProviderId(ProviderNames.AniList, id);
            result.Overview = WebContent.data.Media.description;

            return result;
        }

        /// <summary>
        /// API call to select the lang
        /// </summary>
        /// <param name="WebContent"></param>
        /// <param name="preference"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public string SelectName(RootObject WebContent, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(preferredLanguage) || preferredLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                var title = WebContent.data.Media.title.english; // something fucky, if no value "english" then errors out
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            if (string.Equals(preferredLanguage, "ja", StringComparison.OrdinalIgnoreCase))
            {
                var title = WebContent.data.Media.title.native;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            return WebContent.data.Media.title.romaji;
        }

        public async Task<List<PersonInfo>> GetPersonInfo(int id, CancellationToken cancellationToken)
        {
            List<PersonInfo> lpi = new List<PersonInfo>();
            RootObject WebContent = await WebRequestAPI(AniList_anime_char_link.Replace("{0}", id.ToString()), cancellationToken);
            foreach (Edge edge in WebContent.data.Media.characters.edges)
            {
                if (edge.voiceActors.Count > 0) {
                    VoiceActor va = edge.voiceActors[0];
                    PersonInfo actor = new PersonInfo();
                    PersonInfo character = new PersonInfo();
                    actor.Name = va.name.first + " " + va.name.last;
                    actor.ImageUrl = va.image.large;
                    actor.Role = edge.node.name.first + " " + edge.node.name.last;
                    actor.Type = PersonType.Actor;
                    character.Name = actor.Role;
                    character.ImageUrl = edge.node.image.large;
                    character.Role = actor.Name;
                    character.Type = PersonType.GuestStar;
                    lpi.Add(actor);
                    lpi.Add(character);
                }
            }
            return lpi;
        }

        /// <summary>
        /// API call to get the studios of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public List<string> Get_Studio(RootObject WebContent)
        {
            List<string> studios = new List<string>();
            WebContent.data.Media.studios.edges.ForEach(edge => studios.Add(edge.node.name));
            return studios;
        }

        /// <summary>
        /// API call to get the tags of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public List<string> Get_Tag(RootObject WebContent)
        {
            List<string> tags = new List<string>();
            WebContent.data.Media.tags.ForEach(tag => tags.Add(tag.name));
            return tags;
        }

        /// <summary>
        /// API call to get the genre of the anime
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public List<string> Get_Genre(RootObject WebContent)
        {
            return WebContent.data.Media.genres;
        }

        /// <summary>
        /// API call to get the img url
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_ImageUrl(RootObject WebContent)
        {
            return WebContent.data.Media.coverImage.large;
        }

        /// <summary>
        /// API call too get the rating
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_Rating(RootObject WebContent)
        {
            return (WebContent.data.Media.averageScore / 10).ToString();
        }

        /// <summary>
        /// API call to get the description
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_Overview(RootObject WebContent)
        {
            return WebContent.data.Media.description;
        }

        /// <summary>
        /// API call to search a title and return the right one back
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> Search_GetSeries(string title, CancellationToken cancellationToken)
        {
            string result = null;
            RootObject WebContent = await WebRequestAPI(SearchLink.Replace("{0}", title), cancellationToken);
            var count = 1;
            foreach (Medium media in WebContent.data.Page.media)
            {
                // Less restrictive check against first result (Which should in theory, be the most accurate result)
                if (count == 1)
                {
                    if (Equals_check.Compare_strings_less_strict(media.title.romaji, title))
                    {
                        return media.id.ToString();
                    }
                    if (Equals_check.Compare_strings_less_strict(media.title.english, title))
                    {
                        return media.id.ToString();
                    }
                }

                //get id
                if (Equals_check.Compare_strings(media.title.romaji, title))
                {
                    return media.id.ToString();
                }
                if (Equals_check.Compare_strings(media.title.english, title))
                {
                    return media.id.ToString();
                }


                count++;

                // Check if only result

                //Disabled due to false result.
                /*if (await Task.Run(() => Equals_check.Compare_strings(media.title.native, title)))
                {
                    return media.id.ToString();
                }*/
            }
            return result;
        }

        /// <summary>
        /// API call to search a title and return a list back
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<string>> Search_GetSeries_list(string title, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();
            RootObject WebContent = await WebRequestAPI(SearchLink.Replace("{0}", title), cancellationToken);
            var count = 1;
            foreach (Medium media in WebContent.data.Page.media)
            {
                // Less restrictive check against first result (Which should in theory, be the most accurate result)
                if (count == 1)
                {
                    if (Equals_check.Compare_strings_less_strict(media.title.romaji, title))
                    {
                        result.Add(media.id.ToString());
                    }
                    if (Equals_check.Compare_strings_less_strict(media.title.english, title))
                    {
                        result.Add(media.id.ToString());
                    }
                }

                if (Equals_check.Compare_strings(media.title.romaji, title))
                {
                    result.Add(media.id.ToString());
                }
                if (Equals_check.Compare_strings(media.title.english, title))
                {
                    result.Add(media.id.ToString());
                }
                //count++;
            }
            return result;
        }

        /// <summary>
        /// SEARCH Title
        /// </summary>
        public async Task<string> FindSeries(string title, CancellationToken cancellationToken)
        {
            string aid = await Search_GetSeries(title, cancellationToken);
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }
            aid = await Search_GetSeries(Equals_check.Clear_name(title), cancellationToken);
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }
            return null;
        }

        public async Task<RootObject> WebRequestAPI(string link, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = link
            };

            options.SetPostData(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            using (var response = await _httpClient.SendAsync(options, "POST").ConfigureAwait(false))
            {
                using (var stream = response.Content)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                        return _jsonSerializer.DeserializeFromString<RootObject>(json);
                    }
                }
            }
        }
    }
}