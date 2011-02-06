namespace RoliSoft.TVShowTracker.Parsers.Subtitles
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the languages the search engines were designed to support.
    /// </summary>
    public static class Languages
    {
        /// <summary>
        /// A list of languages with their full name and ISO 3166-1 alpha-2 code.
        /// </summary>
        public static Dictionary<string, string> List = new Dictionary<string, string>
            {
                { "en", "English"    },
                { "hu", "Hungarian"  },
                { "ro", "Romanian"   },
                { "de", "German"     },
                { "fr", "French"     },
                { "es", "Spanish"    },
                { "se", "Swedish"    },
                { "it", "Italian"    },
                { "nl", "Dutch"      },
                { "dk", "Danish"     },
                { "no", "Norwegian"  },
                { "ee", "Estonian"   },
                { "fi", "Finnish"    },
                { "pl", "Polish"     },
                { "is", "Icelandic"  },
                { "cz", "Czech"      },
                { "hr", "Croatian"   },
                { "rs", "Serbian"    },
                { "sk", "Slovak"     },
                { "si", "Slovenian"  },
                { "ru", "Russian"    },
                { "br", "Brazilian"  },
                { "pt", "Portuguese" },
                { "gr", "Greek"      },
                { "tr", "Turkish"    },
                { "ch", "Chinese"    },
                { "jp", "Japanese"   },
                { "kr", "Korean"     },
                { "ar", "Arabic"     },
                { "il", "Hebrew"     },
                { "id", "Indonesian" },
                { "ir", "Persian"    },
            };

        /// <summary>
        /// Extracts the language from the string and returns its ISO 3166-1 alpha-2 code.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>ISO 3166-1 alpha-2 code of the language.</returns>
        public static string Parse(string language)
        {
            foreach (var lang in List)
            {
                if (language.IndexOf(lang.Value, StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    return lang.Key;
                }
            }

            return "null";
        }
    }
}
