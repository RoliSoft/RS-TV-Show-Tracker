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
                { "it", "Italian"    },
                { "se", "Swedish"    },
                { "nl", "Dutch"      },
                { "cz", "Czech"      },
                { "no", "Norwegian"  },
                { "pl", "Polish"     },
                { "hr", "Croatian"   },
                { "ru", "Russian"    },
                { "gr", "Greek"      },
                { "ar", "Arabic"     },
                { "id", "Indonesian" },
                { "il", "Hebrew"     },
                { "tr", "Turkish"    },
                { "ch", "Chinese"    },
                { "dk", "Danish"     },
                { "ee", "Estonian"   },
                { "fi", "Finnish"    },
                { "is", "Icelandic"  },
                { "jp", "Japanese"   },
                { "kr", "Korean"     },
                { "pt", "Portuguese" },
                { "rs", "Serbian"    },
                { "sk", "Slovak"     },
                { "si", "Slovenian"  },
                { "br", "Brazilian"  },
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
