using FluentAssertions;
using MailVolt.Core.Models;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class AttachmentBuilderTests
{
    private static AttachmentBuilder CreateBuilder() => new();

    // ─── FromBytes ─────────────────────────────────────────────────────

    [Fact]
    public void FromBytes_sets_file_name_and_content()
    {
        var bytes = "hello"u8.ToArray();
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("readme.txt", bytes))
            .Build();

        attachment.FileName.Should().Be("readme.txt");
        attachment.Content.Should().BeAssignableTo<MemoryStream>();
        attachment.ContentType.Should().Be("text/plain");
        attachment.ContentId.Should().BeNull();
        attachment.IsInline.Should().BeFalse();
    }

    [Fact]
    public void FromBytes_detects_mime_type_from_extension()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("report.pdf", bytes))
            .Build();

        attachment.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public void FromBytes_falls_back_to_octet_stream_for_unknown_extension()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("file.xyz", bytes))
            .Build();

        attachment.ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public void FromBytes_with_no_extension_falls_back_to_octet_stream()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("README", bytes))
            .Build();

        attachment.ContentType.Should().Be("application/octet-stream");
    }

    // ─── FromStream ────────────────────────────────────────────────────

    [Fact]
    public void FromStream_sets_file_name_and_content()
    {
        var stream = new MemoryStream("data"u8.ToArray());
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromStream("image.png", stream))
            .Build();

        attachment.FileName.Should().Be("image.png");
        attachment.Content.Should().BeSameAs(stream);
        attachment.ContentType.Should().Be("image/png");
    }

    [Fact]
    public void FromStream_accepts_custom_content_type_via_fluent_override()
    {
        var stream = new MemoryStream();
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromStream("data.bin", stream)
            .WithContentType("application/octet-stream"))
            .Build();

        attachment.ContentType.Should().Be("application/octet-stream");
    }

    // ─── FromFile ──────────────────────────────────────────────────────

    [Fact]
    public void FromFile_detects_mime_type()
    {
        // Use a few well-known MIME types
        var builder = CreateBuilder();

        // This will fail if file doesn't exist, but we test the logic via Bytes
        // The FromFile opens a real file, so we test MIME detection via the other paths
        // and just verify the method signature works with an existing file
        var tempFile = Path.GetTempFileName() + ".html";
        try
        {
            File.WriteAllText(tempFile, "<html></html>");
            var builder2 = CreateBuilder();
            var attachment = ((AttachmentBuilder)builder2
                .FromFile(tempFile))
                .Build();

            attachment.FileName.Should().Be(Path.GetFileName(tempFile));
            attachment.ContentType.Should().Be("text/html");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FromFile_falls_back_to_octet_stream_for_unknown_extension()
    {
        var tempFile = Path.GetTempFileName() + ".unknown";
        try
        {
            File.WriteAllText(tempFile, "test");
            var builder = CreateBuilder();
            var attachment = ((AttachmentBuilder)builder
                .FromFile(tempFile))
                .Build();

            attachment.ContentType.Should().Be("application/octet-stream");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    // ─── AsInlineImage ─────────────────────────────────────────────────

    [Fact]
    public void AsInlineImage_sets_content_id_and_is_inline()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("logo.png", bytes)
            .AsInlineImage("logo@mailvolt"))
            .Build();

        attachment.ContentId.Should().Be("logo@mailvolt");
        attachment.IsInline.Should().BeTrue();
    }

    [Fact]
    public void AsInlineImage_defaults_content_type_to_image_png()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("image.unknown", bytes)
            .AsInlineImage("cid@test"))
            .Build();

        attachment.ContentType.Should().Be("image/png");
    }

    [Fact]
    public void AsInlineImage_does_not_override_explicit_content_type()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("image.jpg", bytes)
            .AsInlineImage("cid@test")
            .WithContentType("image/jpeg"))
            .Build();

        // Explicit WithContentType should take precedence
        attachment.ContentType.Should().Be("image/jpeg");
        attachment.IsInline.Should().BeTrue();
    }

    // ─── WithContentType ───────────────────────────────────────────────

    [Fact]
    public void WithContentType_overrides_detected_mime()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("doc.pdf", bytes)
            .WithContentType("application/custom"))
            .Build();

        attachment.ContentType.Should().Be("application/custom");
    }

    // ─── WithFileName ──────────────────────────────────────────────────

    [Fact]
    public void WithFileName_overrides_file_name()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes("original.txt", bytes)
            .WithFileName("renamed.csv"))
            .Build();

        attachment.FileName.Should().Be("renamed.csv");
        // Content type still detected from original file name since it's set before override
        attachment.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void WithFileName_before_content_sets_file_name_for_mime_detection()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        // Since WithFileName is called before FromBytes, the mime detection
        // happens in FromBytes using the fileName parameter, not WithFileName.
        // This test validates the order matters.
        var attachment = ((AttachmentBuilder)builder
            .WithFileName("final.csv")
            .FromBytes("original.txt", bytes))
            .Build();

        attachment.FileName.Should().Be("final.csv");
        // FromBytes detects MIME from "original.txt" (its fileName arg), not the overridden name
        attachment.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void WithFileName_after_FromStream_changes_name()
    {
        var stream = new MemoryStream();
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromStream("image.png", stream)
            .WithFileName("photo.png"))
            .Build();

        attachment.FileName.Should().Be("photo.png");
    }

    // ─── Build validation ──────────────────────────────────────────────

    [Fact]
    public void Build_throws_if_file_name_not_set()
    {
        var builder = CreateBuilder();
        builder.FromBytes("test.txt", new byte[] { 1 });

        // This should work fine, test when FileName is truly missing
        var builder2 = CreateBuilder();

        var act = () => builder2.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File name must be set*");
    }

    [Fact]
    public void Build_throws_if_content_not_set()
    {
        var builder = CreateBuilder();
        builder.WithFileName("empty.txt");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Content must be set*");
    }

    // ─── Known MIME types dictionary ───────────────────────────────────

    [Theory]
    [InlineData("file.txt", "text/plain")]
    [InlineData("file.html", "text/html")]
    [InlineData("file.htm", "text/html")]
    [InlineData("file.css", "text/css")]
    [InlineData("file.js", "application/javascript")]
    [InlineData("file.json", "application/json")]
    [InlineData("file.xml", "application/xml")]
    [InlineData("file.csv", "text/csv")]
    [InlineData("file.pdf", "application/pdf")]
    [InlineData("file.doc", "application/msword")]
    [InlineData("file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("file.xls", "application/vnd.ms-excel")]
    [InlineData("file.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("file.ppt", "application/vnd.ms-powerpoint")]
    [InlineData("file.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("file.png", "image/png")]
    [InlineData("file.jpg", "image/jpeg")]
    [InlineData("file.jpeg", "image/jpeg")]
    [InlineData("file.gif", "image/gif")]
    [InlineData("file.svg", "image/svg+xml")]
    [InlineData("file.ico", "image/x-icon")]
    [InlineData("file.zip", "application/zip")]
    [InlineData("file.gz", "application/gzip")]
    [InlineData("file.tar", "application/x-tar")]
    public void Known_mime_types_are_detected_correctly(string fileName, string expectedMime)
    {
        var bytes = new byte[] { 1, 2, 3 };
        var builder = CreateBuilder();

        var attachment = ((AttachmentBuilder)builder
            .FromBytes(fileName, bytes))
            .Build();

        attachment.ContentType.Should().Be(expectedMime);
    }
}
