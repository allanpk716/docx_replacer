package matcher

import (
	"testing"
)

func TestNewKeywordMatcher(t *testing.T) {
	matcher := NewKeywordMatcher()
	if matcher == nil {
		t.Fatal("Expected non-nil matcher")
	}
}

func TestKeywordMatcher_FindMatches(t *testing.T) {
	keywordMap := map[string]string{
		"#NAME#":    "John Doe",
		"#AGE#":     "30",
		"#COMPANY#": "Tech Corp",
	}

	matcher := NewKeywordMatcher()

	tests := []struct {
		name     string
		text     string
		expected int
	}{
		{
			name:     "single match",
			text:     "Hello #NAME#, welcome!",
			expected: 1,
		},
		{
			name:     "multiple matches",
			text:     "#NAME# is #AGE# years old and works at #COMPANY#",
			expected: 3,
		},
		{
			name:     "no matches",
			text:     "This is a normal text without keywords",
			expected: 0,
		},
		{
			name:     "duplicate matches",
			text:     "#NAME# and #NAME# again",
			expected: 2,
		},
		{
			name:     "partial matches",
			text:     "#NAME and NAME# are not valid",
			expected: 0,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			matches := matcher.FindMatches(tt.text, keywordMap)
			if len(matches) != tt.expected {
				t.Errorf("FindMatches() = %v matches, expected %v", len(matches), tt.expected)
			}
		})
	}
}

func TestKeywordMatcher_ReplaceMatches(t *testing.T) {
	keywordMap := map[string]string{
		"#NAME#": "John Doe",
		"#AGE#":  "30",
	}

	matcher := NewKeywordMatcher()

	tests := []struct {
		name     string
		text     string
		expected string
	}{
		{
			name:     "single replacement",
			text:     "Hello #NAME#!",
			expected: "Hello John Doe!",
		},
		{
			name:     "multiple replacements",
			text:     "#NAME# is #AGE# years old",
			expected: "John Doe is 30 years old",
		},
		{
			name:     "no replacements",
			text:     "No keywords here",
			expected: "No keywords here",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			matches := matcher.FindMatches(tt.text, keywordMap)
			result := matcher.ReplaceMatches(tt.text, matches)
			if result != tt.expected {
				t.Errorf("ReplaceMatches() = %v, expected %v", result, tt.expected)
			}
		})
	}
}

// Benchmark tests
func BenchmarkKeywordMatcher_FindMatches(b *testing.B) {
	keywordMap := map[string]string{
		"#NAME#": "John Doe",
		"#AGE#":  "30",
		"#CITY#": "New York",
	}

	matcher := NewKeywordMatcher()
	text := "Hello #NAME#, you are #AGE# years old and live in #CITY#. #NAME# is a great person!"

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		matcher.FindMatches(text, keywordMap)
	}
}

func BenchmarkKeywordMatcher_ReplaceMatches(b *testing.B) {
	keywordMap := map[string]string{
		"#NAME#": "John Doe",
		"#AGE#":  "30",
		"#CITY#": "New York",
	}

	matcher := NewKeywordMatcher()
	text := "Hello #NAME#, you are #AGE# years old and live in #CITY#. #NAME# is a great person!"

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		matches := matcher.FindMatches(text, keywordMap)
		matcher.ReplaceMatches(text, matches)
	}
}