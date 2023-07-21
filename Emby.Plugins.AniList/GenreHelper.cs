using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Anime
{
    public static class GenreHelper
    {
        private static readonly Dictionary<string, string> GenreMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Action", "Action"},
            {"Advanture", "Adventure"},
            {"Contemporary Fantasy", "Fantasy"},
            {"Comedy", "Comedy"},
            {"Dark Fantasy", "Fantasy"},
            {"Dementia", "Psychological Thriller"},
            {"Demons", "Fantasy"},
            {"Drama", "Drama"},
            {"Ecchi", "Ecchi"},
            {"Fantasy", "Fantasy"},
            {"Harem", "Harem"},
            {"Hentai", "Adult"},
            {"Historical", "Period & Historical"},
            {"Horror", "Horror"},
            {"Josei", "Josei"},
            {"Kids", "Kids"},
            {"Magic", "Fantasy"},
            {"Martial Arts", "Martial Arts"},
            {"Mahou Shoujo", "Mahou Shoujo"},
            {"Mecha", "Mecha"},
            {"Music", "Music"},
            {"Mystery", "Mystery"},
            {"Parody", "Comedy"},
            {"Psychological", "Psychological Thriller"},
            {"Romance", "Romance"},
            {"Sci-Fi", "Sci-Fi"},
            {"Seinen", "Seinen"},
            {"Shoujo", "Shoujo"},
            {"Shounen", "Shounen"},
            {"Slice of Life", "Slice of Life"},
            {"Space", "Sci-Fi"},
            {"Sports", "Sport"},
            {"Supernatural", "Supernatural"},
            {"Thriller", "Thriller"},
            {"Tragedy", "Tragedy"},
            {"Witch", "Supernatural"},
            {"Vampire", "Supernatural"},
            {"Yaoi", "Adult"},
            {"Yuri", "Adult"},
            {"Zombie", "Supernatural"},
            //Proxer
            {"Slice_of_Life", "Slice of Life"},
        };

        private static readonly string[] GenresAsTags =
        {
            "Hentai",
            "Space",
            "Weltraum",
            "Yaoi",
            "Yuri",
            "Demons",
            "Witch",
            //AniSearchTags
            "Satire",
            "Monster",
            "Slapstick",
            "4-panel",
            "CG-Anime",
            "Moe",
            //Themen
            "Gender Bender",
            //Schule (School)
            "Grundschule",
            "Kindergarten",
            "Klubs",
            "Mittelschule",
            "Oberschule",
            "Schule",
            "Universität",
            //Zeit (Time)
            "Altes Asien",
            "Frühe Neuzeit",
            "Gegenwart",
            "industrialisierung",
            "Meiji-Ära",
            "Mittelalter",
            "Weltkriege",
            //Setting
            "Cyberpunk",
            "Endzeit",
            "Space Opera",
            //Figuren
            "Diva",
            "Genie",
            "Schul-Delinquent",
            "Tomboy",
            "Tsundere",
            "Yandere",
            //Kampf (fight)
            "Martial Arts",
            "Real Robots",
            "Super Robots",
            //Sports (Sport)
            "Baseball",
            "Football",
            "Tennis",
            //Kunst (Art)
            "Anime & Film",
            "Malerei",
            "Manga & Doujinshi",
            "Musik",
            "Theater",
            //Tätigkeit
            "Band",
            "Idol",
            "Ninja",
            "Polizist",
            "Ritter",
            "Samurai",
            //Wesen
            "Cyborgs",
            "Elfen",
            "Geister",
            "Hexen",
            "Kamis",
            "Kemonomimi",
            "Monster",
            "Roboter & Androiden",
            "Tiermenschen",
            "Vampire",
            "Youkai",
            "Zombie",
            //Proxer
            "Virtual Reality",
            "Game",
            "Survival",
            "Fanservice",
        };

        private static readonly Dictionary<string, string> IgnoreIfPresent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Psychological Thriller", "Thriller"}
        };

        public static void CleanupGenres(BaseItem item)
        {
            item.Genres = RemoveRedundantGenres(item.Genres)
                                       .Distinct(StringComparer.OrdinalIgnoreCase)
                                       .ToArray();

            TidyGenres(item);

            item.Genres = item.Genres.Except(new[] { "Animation", "Anime" }).ToArray();

            item.Genres = item.Genres.ToArray();

            if (!item.Genres.Contains("Anime", StringComparer.OrdinalIgnoreCase))
            {
                item.Genres = item.Genres.Except(new[] { "Animation" }).ToArray();

                item.AddGenre("Anime");
            }

            item.Genres = item.Genres.OrderBy(i => i).ToArray();
        }

        public static void TidyGenres(BaseItem item)
        {
            var genres = new HashSet<string>();
            var tags = new HashSet<string>(item.Tags);

            foreach (string genre in item.Genres)
            {
                if (GenreMappings.TryGetValue(genre, out string mapped))
                    genres.Add(mapped);
                else
                {
                    genres.Add(genre);
                }

                if (GenresAsTags.Contains(genre, StringComparer.OrdinalIgnoreCase))
                {
                    genres.Add(genre);
                }
            }

            item.Genres = genres.ToArray();
            item.Tags = tags.ToArray();
        }

        public static IEnumerable<string> RemoveRedundantGenres(IEnumerable<string> genres)
        {
            var list = genres as IList<string> ?? genres.ToList();

            var toRemove = list.Where(IgnoreIfPresent.ContainsKey).Select(genre => IgnoreIfPresent[genre]).ToList();
            return list.Where(genre => !toRemove.Contains(genre));
        }
    }
}