using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class CleanupControlProcessorTests
    {
        private readonly CleanupControlProcessor _processor;

        public CleanupControlProcessorTests()
        {
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = new Logger<CleanupControlProcessor>(loggerFactory);
            _processor = new CleanupControlProcessor(logger);
        }

        private string CreateTempDocx()
        {
            var path = Path.GetTempFileName();
            File.Move(path, path + ".docx");
            path += ".docx";
            return path;
        }

        private SdtRun CreateSdtRunWithTag(string tag, string text, RunProperties? runProps = null)
        {
            var sdt = new SdtRun();
            var sdtPr = new SdtProperties();
            if (!string.IsNullOrEmpty(tag))
                sdtPr.AppendChild(new Tag { Val = tag });
            sdt.AppendChild(sdtPr);

            var content = new SdtContentRun();
            var run = new Run();
            if (runProps != null)
                run.AppendChild(runProps.CloneNode(true));
            run.AppendChild(new Text(text));
            content.AppendChild(run);
            sdt.AppendChild(content);
            return sdt;
        }

        private SdtBlock CreateSdtBlockWithTag(string tag, params Paragraph[] paragraphs)
        {
            var sdt = new SdtBlock();
            var sdtPr = new SdtProperties();
            if (!string.IsNullOrEmpty(tag))
                sdtPr.AppendChild(new Tag { Val = tag });
            sdt.AppendChild(sdtPr);

            var content = new SdtContentBlock();
            foreach (var para in paragraphs)
                content.AppendChild(para.CloneNode(true));
            sdt.AppendChild(content);
            return sdt;
        }

        private Paragraph CreateParagraphWithRun(string text, RunProperties? runProps = null)
        {
            var para = new Paragraph();
            var run = new Run();
            if (runProps != null)
                run.AppendChild(runProps.CloneNode(true));
            run.AppendChild(new Text(text));
            para.AppendChild(run);
            return para;
        }

        private string CreateDocxWithControls(Action<Body> customizeBody)
        {
            var path = CreateTempDocx();
            using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                var body = new Body();
                customizeBody(body);
                mainPart.Document = new Document(body);
                mainPart.Document.Save();
            }
            return path;
        }

        private string CreateDocxWithMixedControls()
        {
            return CreateDocxWithControls(body =>
            {
                var para1 = new Paragraph();

                // 关键词控件: #产品名称#
                var keywordSdt = CreateSdtRunWithTag("#产品名称#", "D-二聚体测定试剂盒");
                para1.AppendChild(keywordSdt);

                // 普通文本
                para1.AppendChild(new Run(new Text(" - ")));

                // 非关键词控件: Title
                var normalSdt = CreateSdtRunWithTag("Title", "文档标题");
                para1.AppendChild(normalSdt);

                // 另一个关键词控件
                var keywordSdt2 = CreateSdtRunWithTag("#注册证编号#", "国械注准20240001");
                para1.AppendChild(keywordSdt2);

                body.AppendChild(para1);
            });
        }

        private string CreateDocxWithControlsInHeadersAndFooters()
        {
            var path = CreateTempDocx();
            using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                var body = new Body(new Paragraph(new Run(new Text("Body"))));
                mainPart.Document = new Document(body);

                // Header with keyword control
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                var headerPara = new Paragraph();
                headerPara.AppendChild(CreateSdtRunWithTag("#产品名称#", "HeaderProduct"));
                headerPart.Header = new Header(headerPara);

                // Footer with non-keyword control
                var footerPart = mainPart.AddNewPart<FooterPart>();
                var footerPara = new Paragraph();
                footerPara.AppendChild(CreateSdtRunWithTag("FooterInfo", "FooterValue"));
                footerPart.Footer = new Footer(footerPara);

                mainPart.Document.Save();
            }
            return path;
        }

        [Fact]
        public void ProcessControls_OnlyUnwrapsKeywordTagControls()
        {
            var path = CreateDocxWithMixedControls();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(2, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    var sdts = doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>().ToList();
                    Assert.Single(sdts);
                    var remainingTag = sdts[0].SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                    Assert.Equal("Title", remainingTag);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_PreservesControlsWithoutTag()
        {
            var path = CreateDocxWithControls(body =>
            {
                var para = new Paragraph();
                var noTagSdt = new SdtRun();
                noTagSdt.AppendChild(new SdtProperties());
                var noTagContent = new SdtContentRun();
                noTagContent.AppendChild(new Run(new Text("no tag content")));
                noTagSdt.AppendChild(noTagContent);
                para.AppendChild(noTagSdt);
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(0, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Single(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_PreservesFormattingAfterUnwrap()
        {
            var path = CreateDocxWithControls(body =>
            {
                var para = new Paragraph();
                var runProps = new RunProperties();
                runProps.AppendChild(new Bold());
                runProps.AppendChild(new Color { Val = "FF0000" });
                runProps.AppendChild(new FontSize { Val = "28" });
                var sdt = CreateSdtRunWithTag("#产品名称#", "测试文本", runProps);
                para.AppendChild(sdt);
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var run = doc.MainDocumentPart!.Document.Body!.Descendants<Run>()
                        .First(r => r.GetFirstChild<Text>()?.Text == "测试文本");
                    Assert.NotNull(run.RunProperties);
                    Assert.NotNull(run.RunProperties!.GetFirstChild<Bold>());
                    Assert.Equal("FF0000", run.RunProperties.GetFirstChild<Color>()?.Val?.Value);
                    Assert.Equal("28", run.RunProperties.GetFirstChild<FontSize>()?.Val?.Value);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesSdtBlock()
        {
            var para1 = CreateParagraphWithRun("段落一");
            var para2 = CreateParagraphWithRun("段落二");
            var path = CreateDocxWithControls(body =>
            {
                var blockSdt = CreateSdtBlockWithTag("#块级控件#", para1, para2);
                body.AppendChild(blockSdt);
                body.AppendChild(new Paragraph(new Run(new Text("其他段落"))));
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
                    Assert.True(paragraphs.Count >= 3);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesSdtRun()
        {
            var path = CreateDocxWithControls(body =>
            {
                var para = new Paragraph();
                para.AppendChild(CreateSdtRunWithTag("#行内控件#", "行内内容"));
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var text = doc.MainDocumentPart!.Document.Body!.Descendants<Text>()
                        .FirstOrDefault(t => t.Text == "行内内容");
                    Assert.NotNull(text);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesWrappedTableCell()
        {
            var path = CreateTempDocx();
            try
            {
                using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    var body = new Body();

                    var table = new Table();
                    var tr = new TableRow();

                    // SdtCell wrapping a TableCell
                    var sdtCell = new SdtCell();
                    var sdtPr = new SdtProperties();
                    sdtPr.AppendChild(new Tag { Val = "#表格单元格#" });
                    sdtCell.AppendChild(sdtPr);
                    var sdtContent = new SdtContentCell();
                    var tc = new TableCell();
                    tc.AppendChild(new Paragraph(new Run(new Text("单元格内容"))));
                    sdtContent.AppendChild(tc);
                    sdtCell.AppendChild(sdtContent);
                    tr.AppendChild(sdtCell);
                    table.AppendChild(tr);
                    body.AppendChild(table);

                    mainPart.Document = new Document(body);
                    mainPart.Document.Save();
                }

                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }

                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var cell = doc.MainDocumentPart!.Document.Body!.Descendants<TableCell>().FirstOrDefault();
                    Assert.NotNull(cell);
                    Assert.Equal("单元格内容", cell!.Descendants<Text>().First().Text);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesNestedControls_DifferentTags()
        {
            var path = CreateDocxWithControls(body =>
            {
                var innerSdt = CreateSdtRunWithTag("#内层关键词#", "内层内容");
                var outerContent = new SdtContentBlock();
                var outerPara = new Paragraph();
                outerPara.AppendChild(innerSdt);
                outerContent.AppendChild(outerPara);
                var outerSdt = new SdtBlock();
                var outerPr = new SdtProperties();
                outerPr.AppendChild(new Tag { Val = "#外层关键词#" });
                outerSdt.AppendChild(outerPr);
                outerSdt.AppendChild(outerContent);
                body.AppendChild(outerSdt);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(2, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var text = doc.MainDocumentPart!.Document.Body!.Descendants<Text>()
                        .FirstOrDefault(t => t.Text == "内层内容");
                    Assert.NotNull(text);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesEmptyControl()
        {
            var path = CreateDocxWithControls(body =>
            {
                var sdt = new SdtRun();
                var sdtPr = new SdtProperties();
                sdtPr.AppendChild(new Tag { Val = "#空控件#" });
                sdt.AppendChild(sdtPr);
                sdt.AppendChild(new SdtContentRun());
                var para = new Paragraph();
                para.AppendChild(sdt);
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_HandlesMultiParagraphBlock()
        {
            var path = CreateDocxWithControls(body =>
            {
                var paragraphs = new[]
                {
                    CreateParagraphWithRun("第一段"),
                    CreateParagraphWithRun("第二段"),
                    CreateParagraphWithRun("第三段"),
                };
                var block = CreateSdtBlockWithTag("#多段落#", paragraphs);
                body.AppendChild(block);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                    var texts = doc.MainDocumentPart!.Document.Body!.Descendants<Text>()
                        .Where(t => t.Text.StartsWith("第") && t.Text.EndsWith("段"))
                        .Select(t => t.Text)
                        .ToList();
                    Assert.Equal(3, texts.Count);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_PreservesHeaderNonKeywordControls()
        {
            var path = CreateDocxWithControlsInHeadersAndFooters();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    // Header: #产品名称# removed
                    var headerSdts = doc.MainDocumentPart!.HeaderParts.First().Header.Descendants<SdtElement>().ToList();
                    Assert.Empty(headerSdts);

                    // Footer: FooterInfo preserved
                    var footerSdts = doc.MainDocumentPart!.FooterParts.First().Footer.Descendants<SdtElement>().ToList();
                    Assert.Single(footerSdts);
                    Assert.Equal("FooterInfo", footerSdts[0].SdtProperties?.GetFirstChild<Tag>()?.Val?.Value);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_NoKeywordControls_ReturnsZero()
        {
            var path = CreateDocxWithControls(body =>
            {
                var para = new Paragraph();
                para.AppendChild(CreateSdtRunWithTag("NormalTag", "content"));
                para.AppendChild(CreateSdtRunWithTag("AnotherTag", "content2"));
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(0, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Equal(2, doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>().Count());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_SkipsNestedControlWithSameTag()
        {
            var path = CreateDocxWithControls(body =>
            {
                var innerSdt = CreateSdtRunWithTag("#产品名称#", "内层内容");
                var outerContent = new SdtContentBlock();
                var outerPara = new Paragraph();
                outerPara.AppendChild(innerSdt);
                outerContent.AppendChild(outerPara);
                var outerSdt = new SdtBlock();
                var outerPr = new SdtProperties();
                outerPr.AppendChild(new Tag { Val = "#产品名称#" });
                outerSdt.AppendChild(outerPr);
                outerSdt.AppendChild(outerContent);
                body.AppendChild(outerSdt);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    // Inner control with same tag is skipped (HasAncestorWithSameTag) and remains
                    var remaining = doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>().ToList();
                    Assert.Single(remaining);
                    Assert.Equal("#产品名称#", remaining[0].SdtProperties?.GetFirstChild<Tag>()?.Val?.Value);
                    var text = doc.MainDocumentPart!.Document.Body!.Descendants<Text>()
                        .FirstOrDefault(t => t.Text == "内层内容");
                    Assert.NotNull(text);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessControls_SkipsDetachedControl()
        {
            var path = CreateDocxWithControls(body =>
            {
                var para = new Paragraph();
                para.AppendChild(CreateSdtRunWithTag("#产品名称#", "内容"));
                body.AppendChild(para);
            });
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessControls(doc);
                    doc.MainDocumentPart!.Document.Save();
                    Assert.Equal(1, count);
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<SdtElement>());
                }
            }
            finally { File.Delete(path); }
        }
    }
}
