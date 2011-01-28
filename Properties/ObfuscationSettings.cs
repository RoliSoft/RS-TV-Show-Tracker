using System.Reflection;

[assembly: Obfuscation(Feature = "code control flow obfuscation", Exclude = false)]
//[assembly: Obfuscation(Feature = "rename symbol names with printable characters", Exclude = false)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Remote.Objects.*: renaming", Exclude = true, ApplyToMembers = true)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.OverviewListViewItem: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.GuideListViewItem: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.StatisticsListViewItem: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.DownloadsListViewItem: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.LinkItem: renaming", Exclude = true, ApplyToMembers = true)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Downloads.Link: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Downloads.Qualities: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Downloads.Types: renaming", Exclude = true, ApplyToMembers = true)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Subtitles.Subtitle: renaming", Exclude = true, ApplyToMembers = true)]
[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Subtitles.Subtitle.Languages: renaming", Exclude = true, ApplyToMembers = true)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Subtitles.Engines.OpenSubtitles.IOpenSubtitles: renaming", Exclude = true, ApplyToMembers = true)]

[assembly: Obfuscation(Feature = "Apply to RoliSoft.TVShowTracker.Parsers.Recommendations.RecommendedShow: renaming", Exclude = true, ApplyToMembers = true)]
