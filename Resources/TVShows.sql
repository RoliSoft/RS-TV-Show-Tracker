/*
	The TVShows table contains the name of the TV shows,
	their ID and position when displayed by the software.
*/
CREATE TABLE tvshows (
	rowid INTEGER,
	showid INTEGER PRIMARY KEY,
	name TEXT,
	release TEXT
)

/*
	The Episodes table contains all the episodes of all the
	tracked TV shows with their associated Show ID.
	The EpisodeId field is not random or auto incrementing,
	it is calculated by concating Show ID with Season and Episode.
	The AirDate field is a UNIX timestamp.
	The Pic field contains an URL to a small screenshot.
*/
CREATE TABLE episodes (
	episodeid INTEGER PRIMARY KEY ON CONFLICT REPLACE,
	showid INTEGER,
	season INTEGER,
	episode INTEGER,
	airdate INTEGER,
	name TEXT,
	descr TEXT,
	pic TEXT,
	url TEXT
)

/*
	The ShowData table contains a key-value store, where each
	entry is associated to a Show ID.
	It is intended as a metadata store for the tracked TV shows.
*/
CREATE TABLE showdata (
	id INTEGER PRIMARY KEY ON CONFLICT REPLACE,
	showid INTEGER,
	key TEXT,
	value TEXT
)

/*
	The Tracking table contains all the Episode IDs (along with their
	Show ID) which the user has marked as seen.
*/
CREATE TABLE tracking (
	showid INTEGER,
	episodeid INTEGER
)

/*
	The Settings database contains a key-value store for application-wide
	use for various purposes, where the key is the primary key and unique.
*/
CREATE TABLE settings (
	key TEXT PRIMARY KEY ON CONFLICT REPLACE,
	value TEXT
)

INSERT INTO settings VALUES ("Version", "2")