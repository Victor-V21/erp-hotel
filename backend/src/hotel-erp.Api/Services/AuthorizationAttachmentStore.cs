using Microsoft.AspNetCore.Http;

namespace hotel_erp.Api.Services;

public sealed class AuthorizationAttachmentOptions
{
    public const string SectionName = "AuthorizationAttachments";
    public const long DefaultMaximumSizeBytes = 10 * 1024 * 1024;

    public string Directory { get; init; } = "Uploads/authorizations";
    public long MaximumSizeBytes { get; init; } = DefaultMaximumSizeBytes;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Directory))
        {
            throw new InvalidOperationException($"{SectionName}:Directory es obligatorio.");
        }

        if (MaximumSizeBytes <= 0 || MaximumSizeBytes > 100 * 1024 * 1024)
        {
            throw new InvalidOperationException(
                $"{SectionName}:MaximumSizeBytes debe estar entre 1 y 104857600 bytes.");
        }
    }
}

public sealed class AttachmentValidationException(string message) : Exception(message);

public sealed class AuthorizationAttachmentStore
{
    private static readonly byte[] PdfHeader = "%PDF-"u8.ToArray();
    private static readonly byte[] PdfEndMarker = "%%EOF"u8.ToArray();
    private const int PdfTailInspectionBytes = 4096;

    private readonly AuthorizationAttachmentOptions _options;
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;
    private readonly StringComparison _pathComparison;

    public AuthorizationAttachmentStore(
        AuthorizationAttachmentOptions options,
        IWebHostEnvironment environment)
    {
        _options = options;
        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(options.Directory)
                ? options.Directory
                : Path.Combine(environment.ContentRootPath, options.Directory));
        _rootPathWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public async Task<string> SavePdfAsync(
        Guid authorizationId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateMetadata(file);
        Directory.CreateDirectory(_rootPath);

        var storageKey = $"{authorizationId:N}.pdf";
        var destinationPath = ResolvePath(storageKey)
            ?? throw new InvalidOperationException("No se pudo resolver el almacén de adjuntos.");
        var temporaryPath = Path.Combine(_rootPath, $".{authorizationId:N}-{Guid.NewGuid():N}.uploading");

        try
        {
            await using (var source = file.OpenReadStream())
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalBytes += bytesRead;
                    if (totalBytes > _options.MaximumSizeBytes)
                    {
                        throw new AttachmentValidationException(
                            $"El PDF excede el máximo permitido de {FormatMegabytes(_options.MaximumSizeBytes)} MB.");
                    }

                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                await destination.FlushAsync(cancellationToken);
            }

            if (!HasValidPdfEnvelope(temporaryPath))
            {
                throw new AttachmentValidationException(
                    "El contenido del archivo no corresponde a un PDF completo y válido.");
            }

            File.Move(temporaryPath, destinationPath, overwrite: false);
            return storageKey;
        }
        catch
        {
            DeletePathIfExists(temporaryPath);
            throw;
        }
    }

    public FileStream? OpenPdf(string? storageKey)
    {
        var path = ResolvePath(storageKey);
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.LinkTarget is not null || !HasValidPdfEnvelope(path))
        {
            return null;
        }

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public void DeleteIfExists(string? storageKey)
    {
        var path = ResolvePath(storageKey);
        if (path is not null)
        {
            DeletePathIfExists(path);
        }
    }

    private void ValidateMetadata(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new AttachmentValidationException("El archivo PDF está vacío.");
        }

        if (file.Length > _options.MaximumSizeBytes)
        {
            throw new AttachmentValidationException(
                $"El PDF excede el máximo permitido de {FormatMegabytes(_options.MaximumSizeBytes)} MB.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new AttachmentValidationException("Solo se permiten archivos con extensión PDF.");
        }

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new AttachmentValidationException("El tipo declarado del archivo no corresponde a un PDF.");
        }
    }

    private string? ResolvePath(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || !string.Equals(Path.GetExtension(storageKey), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(
                Path.IsPathRooted(storageKey)
                    ? storageKey
                    : Path.Combine(_rootPath, storageKey));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return fullPath.StartsWith(_rootPathWithSeparator, _pathComparison) ? fullPath : null;
    }

    private static bool HasValidPdfEnvelope(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < PdfHeader.Length + PdfEndMarker.Length)
            {
                return false;
            }

            Span<byte> header = stackalloc byte[PdfHeader.Length];
            if (stream.Read(header) != header.Length || !header.SequenceEqual(PdfHeader))
            {
                return false;
            }

            var tailLength = (int)Math.Min(PdfTailInspectionBytes, stream.Length);
            var tail = new byte[tailLength];
            stream.Seek(-tailLength, SeekOrigin.End);
            stream.ReadExactly(tail);
            return tail.AsSpan().IndexOf(PdfEndMarker) >= 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string FormatMegabytes(long bytes)
        => Math.Ceiling(bytes / 1024d / 1024d).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void DeletePathIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // El error original de persistencia tiene prioridad sobre el fallo de limpieza.
        }
        catch (UnauthorizedAccessException)
        {
            // No se oculta el error principal de validación o persistencia.
        }
    }
}
