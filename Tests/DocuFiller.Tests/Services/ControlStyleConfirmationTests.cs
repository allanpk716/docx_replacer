using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Services
{
    /// <summary>
    /// 测试控件样式确认功能：修复旧程序遗留的格式覆盖（如标红色）问题
    /// </summary>
    public class ControlStyleConfirmationTests
    {
        private readonly SafeTextReplacer _replacer;

        public ControlStyleConfirmationTests()
        {
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = new Logger<SafeTextReplacer>(loggerFactory);
            _replacer = new SafeTextReplacer(logger);
        }

        /// <summary>
        /// 测试：替换后应正确应用控件 sdtPr 中定义的 rStyle，并移除旧程序遗留的标红色
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_WithRedColorOverrideAndRStyle_AppliesCorrectStyle()
        {
            // Arrange - 模拟旧程序替换后的结构：
            // sdtPr 中有 rStyle="38" (样式1 Char)
            // 内容 Run 中有 color=FF0000 (红色覆盖)
            var sdtRpr = new RunProperties();
            sdtRpr.AppendChild(new RunStyle { Val = "38" });
            sdtRpr.AppendChild(new Languages { Val = "en-US", EastAsia = "zh-CN" });

            var sdtProps = new SdtProperties();
            sdtProps.AppendChild(sdtRpr);
            sdtProps.AppendChild(new SdtAlias { Val = "#产品名称#" });
            sdtProps.AppendChild(new Tag { Val = "#产品名称#" });
            sdtProps.AppendChild(new SdtId { Val = 123456 });

            var contentRunProps = new RunProperties();
            contentRunProps.AppendChild(new Color { Val = "FF0000" });  // 旧程序遗留的标红色

            var sdtContent = new SdtContentRun();
            sdtContent.AppendChild(new Run(contentRunProps, new Text("Fluorescent dye")));

            var sdtRun = new SdtRun();
            sdtRun.AppendChild(sdtProps);
            sdtRun.AppendChild(sdtContent);

            // Act - 执行替换
            _replacer.ReplaceTextInControl(sdtRun, "新产品名称");

            // Assert - 验证替换后的 Run 应该：
            // 1. 包含正确的文本
            // 2. 具有 rStyle="38"（来自控件的样式定义）
            // 3. 不再包含 color=FF0000（旧程序的标红色应被移除）
            var run = sdtRun.Descendants<Run>().First();
            var text = run.Descendants<Text>().First();
            Assert.Equal("新产品名称", text.Text);

            var runProps = run.RunProperties;
            Assert.NotNull(runProps);

            // 验证 rStyle 被正确应用
            var rStyle = runProps.GetFirstChild<RunStyle>();
            Assert.NotNull(rStyle);
            Assert.Equal("38", rStyle.Val?.Value);

            // 验证旧的标红色被移除
            var color = runProps.GetFirstChild<Color>();
            Assert.Null(color);
        }

        /// <summary>
        /// 测试：没有 rStyle 的控件不应被影响
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_WithoutRStyle_NoStyleApplied()
        {
            // Arrange - 没有 rStyle 的控件
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag { Val = "test" }),
                new SdtContentRun(new Run(new Text("old")))
            );

            // Act
            _replacer.ReplaceTextInControl(sdtRun, "new");

            // Assert - 文本应被正确替换，rStyle 不存在
            var run = sdtRun.Descendants<Run>().First();
            Assert.Equal("new", run.Descendants<Text>().First().Text);

            var rStyle = run.RunProperties?.GetFirstChild<RunStyle>();
            Assert.Null(rStyle);  // 没有 rStyle 是正常的
        }

        /// <summary>
        /// 测试：已经正确设置了 rStyle 且没有格式覆盖的控件应保持不变
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_AlreadyCorrectStyle_NoChangesNeeded()
        {
            // Arrange - 已经正确设置了 rStyle 的控件
            var sdtRpr = new RunProperties();
            sdtRpr.AppendChild(new RunStyle { Val = "41" });

            var sdtProps = new SdtProperties();
            sdtProps.AppendChild(sdtRpr);
            sdtProps.AppendChild(new Tag { Val = "test" });

            var contentRpr = new RunProperties();
            contentRpr.AppendChild(new RunStyle { Val = "41" });  // 已经是正确的 rStyle

            var sdtContent = new SdtContentRun();
            sdtContent.AppendChild(new Run(contentRpr, new Text("old")));

            var sdtRun = new SdtRun();
            sdtRun.AppendChild(sdtProps);
            sdtRun.AppendChild(sdtContent);

            // Act
            _replacer.ReplaceTextInControl(sdtRun, "new");

            // Assert
            var run = sdtRun.Descendants<Run>().First();
            Assert.Equal("new", run.Descendants<Text>().First().Text);

            var rStyle = run.RunProperties?.GetFirstChild<RunStyle>();
            Assert.NotNull(rStyle);
            Assert.Equal("41", rStyle.Val?.Value);
        }

        /// <summary>
        /// 测试：SdtBlock 类型的控件也应正确应用样式
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_SdtBlockWithRedOverride_AppliesCorrectStyle()
        {
            // Arrange - SdtBlock 控件，sdtPr 有 rStyle，内容有红色覆盖
            var sdtRpr = new RunProperties();
            sdtRpr.AppendChild(new RunStyle { Val = "38" });
            sdtRpr.AppendChild(new Languages { Val = "en-US", EastAsia = "zh-CN" });

            var sdtProps = new SdtProperties();
            sdtProps.AppendChild(sdtRpr);
            sdtProps.AppendChild(new Tag { Val = "#产品型号#" });

            var contentRpr = new RunProperties();
            contentRpr.AppendChild(new Color { Val = "FF0000" });  // 旧程序遗留的标红色

            var sdtContent = new SdtContentBlock();
            sdtContent.AppendChild(new Paragraph(new Run(contentRpr, new Text("BH-URIT HC 60"))));

            var sdtBlock = new SdtBlock();
            sdtBlock.AppendChild(sdtProps);
            sdtBlock.AppendChild(sdtContent);

            // Act
            _replacer.ReplaceTextInControl(sdtBlock, "新型号123");

            // Assert
            var run = sdtBlock.Descendants<Run>().First();
            Assert.Equal("新型号123", run.Descendants<Text>().First().Text);

            var runProps = run.RunProperties;
            Assert.NotNull(runProps);

            var rStyle = runProps.GetFirstChild<RunStyle>();
            Assert.NotNull(rStyle);
            Assert.Equal("38", rStyle.Val?.Value);

            var color = runProps.GetFirstChild<Color>();
            Assert.Null(color);
        }

        /// <summary>
        /// 使用实际的测试文档验证样式确认功能
        /// </summary>
        [Fact]
        public void ReplaceTextInControl_RealDocxWithRedOverride_AppliesCorrectStyle()
        {
            // Arrange - 使用实际的测试文档
            var testDocPath = @"test_data\字体没有跟随\IVDR-BH-FR68-CE00 Overview.docx";
            if (!File.Exists(testDocPath))
            {
                // 在 CI 环境中可能不存在此文件，跳过测试
                return;
            }

            // 复制到临时文件
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_style_{Guid.NewGuid()}.docx");
            File.Copy(testDocPath, tempFile, overwrite: true);

            try
            {
                using var doc = WordprocessingDocument.Open(tempFile, true);
                var mainPart = doc.MainDocumentPart!;
                var body = mainPart.Document.Body!;

                // 找到第一个 "#产品名称#" 控件
                var control = body.Descendants<SdtElement>()
                    .FirstOrDefault(c =>
                    {
                        var tag = c.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                        return tag == "#产品名称#";
                    });

                Assert.NotNull(control);

                // 验证初始状态：控件的 sdtPr 中应该有 rStyle
                var sdtRpr = control.SdtProperties?.GetFirstChild<RunProperties>();
                var controlStyle = sdtRpr?.GetFirstChild<RunStyle>();
                Assert.NotNull(controlStyle);
                Assert.Equal("38", controlStyle.Val?.Value);  // 样式1 Char

                // 验证初始状态：内容 Run 应该有标红色 FF0000
                var initialRun = control.Descendants<SdtContentRun>().First()
                    .Descendants<Run>().First();
                var initialColor = initialRun.RunProperties?.GetFirstChild<Color>();
                Assert.NotNull(initialColor);
                Assert.Equal("FF0000", initialColor.Val?.Value);

                // Act - 执行替换
                _replacer.ReplaceTextInControl(control, "测试产品名称");

                // Assert - 替换后
                var contentRun = control.Descendants<SdtContentRun>().First();
                var replacedRun = contentRun.Descendants<Run>().First();
                var replacedText = replacedRun.Descendants<Text>().First();
                Assert.Equal("测试产品名称", replacedText.Text);

                // 关键验证：rStyle 应该被正确应用
                var runRStyle = replacedRun.RunProperties?.GetFirstChild<RunStyle>();
                Assert.NotNull(runRStyle);
                Assert.Equal("38", runRStyle.Val?.Value);

                // 关键验证：旧的标红色应该被移除
                var runColor = replacedRun.RunProperties?.GetFirstChild<Color>();
                Assert.Null(runColor);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
