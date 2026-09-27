namespace FSH.Framework.Storage.S3;

public sealed class S3StorageOptions
{
    public string? Bucket { get; set; }
    public string? Region { get; set; }
    public string? Prefix { get; set; }
    public bool PublicRead { get; set; } = true;
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Custom S3 endpoint URL. Set this to point at MinIO or any other S3-compatible
    /// service (e.g. "http://localhost:9000"). Leave empty to target AWS S3.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Endpoint that presigned upload/download URLs point at, for when browsers reach the store on a
    /// different address than the API does (e.g. compose: <see cref="ServiceUrl"/> "http://rustfs:9000",
    /// this "https://s3.example.com"). SigV4 signs the host, so the URL has to be signed for the public
    /// one; every other S3 call keeps using <see cref="ServiceUrl"/>. Leave empty to presign against
    /// <see cref="ServiceUrl"/>.
    /// </summary>
    public string? PresignServiceUrl { get; set; }

    // One place decides the presign endpoint, so the client's host and the URL's scheme cannot disagree.
    internal string? PresignEndpoint => string.IsNullOrWhiteSpace(PresignServiceUrl) ? ServiceUrl : PresignServiceUrl;

    /// <summary>
    /// Explicit access key. When either <see cref="AccessKey"/> or <see cref="SecretKey"/>
    /// is empty, the AWS SDK's ambient credential chain is used instead.
    /// </summary>
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>
    /// Required for MinIO and most non-AWS S3-compatible services (they do not support
    /// virtual-hosted-style subdomains). Ignored when <see cref="ServiceUrl"/> is empty.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}
