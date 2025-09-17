package comment

import (
	"testing"

	"github.com/stretchr/testify/assert"
)

// TestCommentManager_ParseComment 测试注释解析
func TestCommentManager_ParseComment(t *testing.T) {
	tests := []struct {
		name        string
		commentText string
		expected    *ReplacementComment
		wantErr     bool
	}{
		{
			name:        "valid comment",
			commentText: "<!-- DOCX_REPLACER_ORIGINAL:#name# LAST_VALUE:John REPLACE_COUNT:1 LAST_MODIFIED:2023-01-01T00:00:00Z -->",
			expected: &ReplacementComment{
				OriginalKeyword: "#name#",
				LastValue:      "John",
				ReplaceCount:   1,
			},
			wantErr: false,
		},
		{
			name:        "invalid format",
			commentText: "<!-- invalid comment -->",
			expected:    nil,
			wantErr:     true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			cm := NewCommentManager(&CommentConfig{
				EnableCommentTracking: true,
				CommentFormat:        "DOCX_REPLACER_ORIGINAL",
			})

			result, err := cm.ParseComment(tt.commentText)

			if tt.wantErr {
				assert.Error(t, err)
				assert.Nil(t, result)
			} else {
				assert.NoError(t, err)
				assert.Equal(t, tt.expected.OriginalKeyword, result.OriginalKeyword)
				assert.Equal(t, tt.expected.LastValue, result.LastValue)
				assert.Equal(t, tt.expected.ReplaceCount, result.ReplaceCount)
			}
		})
	}
}

// TestCommentManager_GenerateComment 测试注释生成
func TestCommentManager_GenerateComment(t *testing.T) {
	cm := NewCommentManager(&CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
		MaxCommentHistory:    10,
	})

	result := cm.GenerateComment("#name#", "John", 1)
	assert.Contains(t, result, "DOCX_REPLACER_ORIGINAL")
	assert.Contains(t, result, "#name#")
	assert.Contains(t, result, "John")
	assert.Contains(t, result, "REPLACE_COUNT:1")
}

// TestCommentManager_ParseComments 测试文档注释解析
func TestCommentManager_ParseComments(t *testing.T) {
	cm := NewCommentManager(&CommentConfig{
		EnableCommentTracking: true,
		CommentFormat:        "DOCX_REPLACER_ORIGINAL",
	})

	xmlContent := `<w:document><!-- DOCX_REPLACER_ORIGINAL:#name# LAST_VALUE:John REPLACE_COUNT:1 LAST_MODIFIED:2023-01-01T00:00:00Z --><w:p><w:t>Hello</w:t></w:p></w:document>`
	err := cm.ParseComments(xmlContent)

	assert.NoError(t, err)
	assert.Equal(t, 1, cm.GetCommentCount())
	comment, exists := cm.GetComment("#name#")
	assert.True(t, exists)
	assert.Equal(t, "#name#", comment.OriginalKeyword)
}

// TestCommentManager_EdgeCases 测试边界情况
func TestCommentManager_EdgeCases(t *testing.T) {
	cm := NewCommentManager(&CommentConfig{
		EnableCommentTracking: false, // 禁用注释追踪
	})

	// 禁用状态下应该返回空字符串
	result := cm.GenerateComment("#name#", "John", 1)
	assert.Empty(t, result)

	// 解析注释应该返回错误
	_, err := cm.ParseComment("<!-- DOCX_REPLACER_ORIGINAL:#name# LAST_VALUE:John REPLACE_COUNT:1 LAST_MODIFIED:2023-01-01T00:00:00Z -->")
	assert.Error(t, err)
}

// TestCommentManager_IsEnabled 测试启用状态检查
func TestCommentManager_IsEnabled(t *testing.T) {
	t.Run("启用状态", func(t *testing.T) {
		cm := NewCommentManager(&CommentConfig{
			EnableCommentTracking: true,
		})
		assert.True(t, cm.IsEnabled())
	})

	t.Run("禁用状态", func(t *testing.T) {
		cm := NewCommentManager(&CommentConfig{
			EnableCommentTracking: false,
		})
		assert.False(t, cm.IsEnabled())
	})
}