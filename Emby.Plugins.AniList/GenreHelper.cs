using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Anime
{
    /* query {
      GenreCollection
    } */

    /* query {
        MediaTagCollection {
             name
         }
    } */
    public static class GenreHelper
    {
        /* private static readonly Dictionary<string, string> GenreMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Actual Genres -> Genres, only really needed when the Input Category is different than the output, otherwise it is wasted cycles.
            // {"Action", "Action"},
            // {"Adventure", "Adventure"},
            // {"Comedy", "Comedy"},
            // {"Drama", "Drama"},
            // {"Ecchi", "Ecchi"},
            // {"Fantasy", "Fantasy"},
            // {"Horror", "Horror"},
            // {"Mahou Shoujo", "Mahou Shoujo"},
            // {"Mecha", "Mecha"},
            // {"Music", "Music"},
            // {"Mystery", "Mystery"},
            // {"Psychological", "Psychological"},
            // {"Romance", "Romance"},
            // {"Sci-Fi", "Sci-Fi"},
            // {"Slice of Life", "Slice of Life"},
            // {"Sports", "Sports"},
            // {"Supernatural", "Supernatural"},
            // {"Thriller", "Thriller"},
        }; */

        private static readonly string[] GenresAsTags =
        {
            // Tags to Tag with
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
                genres.Add(genre);

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