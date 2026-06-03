using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Office2013Word = DocumentFormat.OpenXml.Office2013.Word;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class CleanupCommentProcessorTests
    {
        private readonly CleanupCommentProcessor _processor;

        public CleanupCommentProcessorTests()
        {
            using var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = new Logger<CleanupCommentProcessor>(loggerFactory);
            _processor = new CleanupCommentProcessor(logger);
        }

        private string CreateTempDocx()
        {
            var path = Path.GetTempFileName();
            File.Move(path, path + ".docx");
            path += ".docx";
            return path;
        }

        private string CreateDocxWithComments(
            Action<WordprocessingDocument>? customize = null)
        {
            var path = CreateTempDocx();

            using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("Hello")))));

                // Add comments part
                var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
                commentsPart.Comments = new Comments();
                var comment = new Comment { Id = "0", Author = "Test", Date = DateTime.Now };
                comment.AppendChild(new Paragraph(new Run(new Text("Test comment"))));
                commentsPart.Comments.AppendChild(comment);

                // Add commentsExtended part (commentsExtended.xml)
                var commentsExPart = mainPart.AddNewPart<WordprocessingCommentsExPart>();
                commentsExPart.CommentsEx = new Office2013Word.CommentsEx();
                commentsExPart.CommentsEx.AppendChild(
                    new Office2013Word.CommentEx { ParaId = new HexBinaryValue("00000000") });

                // Add people part
                var peoplePart = mainPart.AddNewPart<WordprocessingPeoplePart>();
                peoplePart.People = new Office2013Word.People();
                peoplePart.People.AppendChild(
                    new Office2013Word.Person { Author = "Test" });

                // Add comment range markers in the document body
                var body = mainPart.Document.Body!;
                var para = body.GetFirstChild<Paragraph>()!;
                para.InsertBefore(new CommentRangeStart { Id = "0" }, para.GetFirstChild<Run>());
                para.AppendChild(new CommentRangeEnd { Id = "0" });
                para.AppendChild(new Run(new CommentReference { Id = "0" }));

                customize?.Invoke(doc);
                mainPart.Document.Save();
            }

            return path;
        }

        [Fact]
        public void ProcessComments_RemovesCommentsPart()
        {
            var path = CreateDocxWithComments();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    _processor.ProcessComments(doc);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Null(doc.MainDocumentPart!.WordprocessingCommentsPart);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_RemovesCommentsExtendedPart()
        {
            var path = CreateDocxWithComments();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    _processor.ProcessComments(doc);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Null(doc.MainDocumentPart!.WordprocessingCommentsExPart);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_RemovesPeoplePart()
        {
            var path = CreateDocxWithComments();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    _processor.ProcessComments(doc);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Null(doc.MainDocumentPart!.WordprocessingPeoplePart);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_RemovesCommentMarkers()
        {
            var path = CreateDocxWithComments();
            try
            {
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    _processor.ProcessComments(doc);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<CommentRangeStart>());
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<CommentRangeEnd>());
                    Assert.Empty(doc.MainDocumentPart!.Document.Body!.Descendants<CommentReference>());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_ChangesRunColorToBlack()
        {
            var path = CreateTempDocx();
            try
            {
                using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
                    commentsPart.Comments = new Comments();
                    var comment = new Comment { Id = "0", Author = "Test", Date = DateTime.Now };
                    comment.AppendChild(new Paragraph(new Run(new Text("comment"))));
                    commentsPart.Comments.AppendChild(comment);

                    var body = new Body();
                    var para = new Paragraph();
                    para.AppendChild(new CommentRangeStart { Id = "0" });
                    var run = new Run();
                    var runProps = new RunProperties();
                    runProps.AppendChild(new Color { Val = "FF0000" });
                    run.AppendChild(runProps);
                    run.AppendChild(new Text("colored text"));
                    para.AppendChild(run);
                    para.AppendChild(new CommentRangeEnd { Id = "0" });
                    para.AppendChild(new Run(new CommentReference { Id = "0" }));
                    body.AppendChild(para);
                    mainPart.Document.AppendChild(body);
                    mainPart.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    _processor.ProcessComments(doc);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    var run = doc.MainDocumentPart!.Document.Body!.Descendants<Run>()
                        .First(r => r.GetFirstChild<Text>()?.Text == "colored text");
                    var color = run.RunProperties?.GetFirstChild<Color>();
                    Assert.NotNull(color);
                    Assert.Equal("000000", color!.Val?.Value);
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_HandlesCrossNestedCommentRange()
        {
            var path = CreateTempDocx();
            try
            {
                using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
                    commentsPart.Comments = new Comments();
                    var comment = new Comment { Id = "0", Author = "Test", Date = DateTime.Now };
                    comment.AppendChild(new Paragraph(new Run(new Text("comment"))));
                    commentsPart.Comments.AppendChild(comment);

                    var body = new Body();
                    var table = new Table();
                    var tr = new TableRow();
                    var tc = new TableCell();
                    var cellPara = new Paragraph();
                    cellPara.AppendChild(new CommentRangeStart { Id = "0" });
                    var run = new Run();
                    var runProps = new RunProperties();
                    runProps.AppendChild(new Color { Val = "FF0000" });
                    run.AppendChild(runProps);
                    run.AppendChild(new Text("text in cell"));
                    cellPara.AppendChild(run);
                    tc.AppendChild(cellPara);
                    tr.AppendChild(tc);
                    table.AppendChild(tr);

                    var endPara = new Paragraph();
                    endPara.AppendChild(new CommentRangeEnd { Id = "0" });
                    endPara.AppendChild(new Run(new CommentReference { Id = "0" }));

                    body.AppendChild(table);
                    body.AppendChild(endPara);
                    mainPart.Document.AppendChild(body);
                    mainPart.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessComments(doc);
                    Assert.Equal(1, count);
                    doc.MainDocumentPart!.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, false))
                {
                    var run = doc.MainDocumentPart!.Document.Body!.Descendants<Run>()
                        .First(r => r.GetFirstChild<Text>()?.Text == "text in cell");
                    var color = run.RunProperties?.GetFirstChild<Color>();
                    Assert.NotNull(color);
                    Assert.Equal("000000", color!.Val?.Value);
                    Assert.Empty(doc.MainDocumentPart.Document.Body.Descendants<CommentRangeStart>());
                    Assert.Empty(doc.MainDocumentPart.Document.Body.Descendants<CommentRangeEnd>());
                }
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void ProcessComments_NoComments_ReturnsZero()
        {
            var path = CreateTempDocx();
            try
            {
                using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("No comments")))));
                    mainPart.Document.Save();
                }
                using (var doc = WordprocessingDocument.Open(path, true))
                {
                    var count = _processor.ProcessComments(doc);
                    Assert.Equal(0, count);
                }
            }
            finally { File.Delete(path); }
        }
    }
}
