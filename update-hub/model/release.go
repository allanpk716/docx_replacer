package model

import "regexp"

// ReleaseFeed represents the Velopack releases JSON structure.
type ReleaseFeed struct {
	Assets []ReleaseAsset `json:"Assets"`
}

// ReleaseAsset represents a single release artifact in the feed.
type ReleaseAsset struct {
	PackageId string `json:"PackageId"`
	Version   string `json:"Version"`
	Type      string `json:"Type"`   // "Full" or "Delta"
	FileName  string `json:"FileName"`
	SHA1      string `json:"SHA1"`
	Size      int64  `json:"Size"`
}

// feedFilenamePattern matches releases.*.json filenames (e.g. releases.win.json, releases.linux.json).
var feedFilenamePattern = regexp.MustCompile(`^releases\.[a-zA-Z0-9_]+\.json$`)

// IsFeedFilename returns true if the filename matches the releases.*.json pattern
// used by Velopack for OS-specific feed files.
func IsFeedFilename(filename string) bool {
	return feedFilenamePattern.MatchString(filename)
}
