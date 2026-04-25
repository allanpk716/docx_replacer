package storage

import (
	"fmt"
	"log"
	"sort"
	"strings"

	"docufiller-update-server/model"
)

// DefaultMaxKeep is the default number of versions to retain per channel.
const DefaultMaxKeep = 10

// CleanupOldVersions removes old versions from a channel, keeping only the
// most recent maxKeep versions (sorted by semver descending). It deletes the
// associated .nupkg files from disk and removes their entries from the release
// feed. Returns the list of removed versions.
func (s *Store) CleanupOldVersions(channel string, maxKeep int) ([]string, error) {
	if maxKeep < 1 {
		maxKeep = DefaultMaxKeep
	}

	feed, err := s.ReadReleaseFeed(channel)
	if err != nil {
		return nil, fmt.Errorf("cleanup %s: read feed: %w", channel, err)
	}

	if len(feed.Assets) == 0 {
		return nil, nil
	}

	// Collect unique versions
	versionSet := map[string]bool{}
	for _, a := range feed.Assets {
		if a.Version != "" {
			versionSet[a.Version] = true
		}
	}

	versions := make([]string, 0, len(versionSet))
	for v := range versionSet {
		versions = append(versions, v)
	}

	// Sort descending by semver
	sort.Slice(versions, func(i, j int) bool {
		return compareSemver(versions[i], versions[j]) > 0
	})

	// Nothing to remove
	if len(versions) <= maxKeep {
		return nil, nil
	}

	toRemove := versions[maxKeep:]
	removeSet := map[string]bool{}
	for _, v := range toRemove {
		removeSet[v] = true
	}

	// Delete .nupkg files for removed versions
	for _, ver := range toRemove {
		if err := s.DeleteVersion(channel, ver); err != nil {
			log.Printf(`{"event":"cleanup_delete_error","channel":"%s","version":"%s","error":"%s"}`, channel, ver, err)
			// Continue removing other versions even if one fails
		}
	}

	// Filter assets to keep only retained versions
	var kept []model.ReleaseAsset
	for _, a := range feed.Assets {
		if !removeSet[a.Version] {
			kept = append(kept, a)
		}
	}
	feed.Assets = kept

	if err := s.WriteReleaseFeed(channel, feed); err != nil {
		return nil, fmt.Errorf("cleanup %s: write feed: %w", channel, err)
	}

	log.Printf(`{"event":"cleanup_complete","channel":"%s","removed":%d,"kept":%d,"removed_versions":["%s"]}`,
		channel, len(toRemove), maxKeep, strings.Join(toRemove, `","`))

	return toRemove, nil
}

// compareSemver compares two semver strings (e.g. "1.2.3").
// Returns -1 if a < b, 0 if equal, 1 if a > b.
func compareSemver(a, b string) int {
	ap := parseSemver(a)
	bp := parseSemver(b)
	for i := 0; i < 3; i++ {
		if ap[i] < bp[i] {
			return -1
		}
		if ap[i] > bp[i] {
			return 1
		}
	}
	return 0
}

// parseSemver parses "1.2.3" into [1, 2, 3].
func parseSemver(v string) [3]int {
	var result [3]int
	parts := strings.SplitN(v, ".", 4)
	for i, p := range parts {
		if i >= 3 {
			break
		}
		// Parse digits only, stop at first non-digit
		num := 0
		for _, c := range p {
			if c >= '0' && c <= '9' {
				num = num*10 + int(c-'0')
			} else {
				break
			}
		}
		result[i] = num
	}
	return result
}
