namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the languages the search engines were designed to support.
    /// </summary>
    public static class Languages
    {
        /// <summary>
        /// A list of languages with their full name and ISO 639-1 code.
        /// </summary>
        public static Dictionary<string, string> List = new Dictionary<string, string>
            {
                { "en", "English"    },
                { "hu", "Hungarian"  },
                { "ro", "Romanian"   },
                { "de", "German"     },
                { "fr", "French"     },
                { "es", "Spanish"    },
                { "sv", "Swedish"    },
                { "it", "Italian"    },
                { "nl", "Dutch"      },
                { "da", "Danish"     },
                { "no", "Norwegian"  },
                { "et", "Estonian"   },
                { "fi", "Finnish"    },
                { "pl", "Polish"     },
                { "is", "Icelandic"  },
                { "cs", "Czech"      },
                { "hr", "Croatian"   },
                { "sr", "Serbian"    },
                { "sk", "Slovak"     },
                { "sl", "Slovenian"  },
                { "ru", "Russian"    },
                { "br", "Brazilian"  },
                { "pt", "Portuguese" },
                { "el", "Greek"      },
                { "tr", "Turkish"    },
                { "zh", "Chinese"    },
                { "ja", "Japanese"   },
                { "ko", "Korean"     },
                { "ar", "Arabic"     },
                { "he", "Hebrew"     },
                { "id", "Indonesian" },
                { "fa", "Persian"    },
                { "lt", "Lithuanian" },
                { "cy", "Welsh"      }
            };

        /// <summary>
        /// Extracts the language from the string and returns its ISO 639-1 code.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>ISO 639-1 code of the language.</returns>
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
