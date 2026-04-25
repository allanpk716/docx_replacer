package model

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
