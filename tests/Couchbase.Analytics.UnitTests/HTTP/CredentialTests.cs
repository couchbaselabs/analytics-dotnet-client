using Couchbase.AnalyticsClient.HTTP;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.HTTP;

public class CredentialTests
{
    [Fact]
    public void Create_ReturnsCredentialWithBasicHeader()
    {
        // Arrange & Act
        var credential = Credential.Create("admin", "password");

        // Assert
        Assert.NotNull(credential.AuthorizationHeader);
        Assert.Equal("Basic", credential.AuthorizationHeader.Scheme);
    }

    [Fact]
    public void Constructor_ReturnsCredentialWithBasicHeader()
    {
        // Arrange & Act
        var credential = new Credential("admin", "password");

        // Assert
        Assert.NotNull(credential.AuthorizationHeader);
        Assert.Equal("Basic", credential.AuthorizationHeader.Scheme);
    }

    [Fact]
    public void AuthorizationHeader_EncodesUsernameAndPassword()
    {
        // Arrange
        var credential = Credential.Create("admin", "password");
        var expected = Convert.ToBase64String("admin:password"u8.ToArray());

        // Act & Assert
        Assert.Equal(expected, credential.AuthorizationHeader.Parameter);
    }

    [Fact]
    public void AuthorizationHeader_EncodesUtf8()
    {
        // Arrange — spec requires UTF-8 encoding
        var credential = Credential.Create("user", "pässwörd");
        var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user:pässwörd"));

        // Act & Assert
        Assert.Equal(expected, credential.AuthorizationHeader.Parameter);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = Credential.Create("admin", "password");
        var b = Credential.Create("admin", "password");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = Credential.Create("admin", "password1");
        var b = Credential.Create("admin", "password2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Credential_ImplementsICredential()
    {
        ICredential credential = Credential.Create("admin", "password");

        Assert.NotNull(credential.AuthorizationHeader);
        Assert.Equal("Basic", credential.AuthorizationHeader!.Scheme);
    }
    [Fact]
    public void ToString_DoesNotLeakAuthorizationHeader()
    {
        var credential = Credential.Create("admin", "password");
        var str = credential.ToString();

        Assert.Contains("Username = admin", str);
        Assert.DoesNotContain("Basic", str);
        Assert.DoesNotContain("AuthorizationHeader", str);
        Assert.DoesNotContain("password", str);
    }
}

public class JwtCredentialTests
{
    [Fact]
    public void Create_ReturnsBearerHeader()
    {
        // Arrange & Act
        var credential = JwtCredential.Create("xxxxx.yyyyy.zzzzz");

        // Assert
        Assert.NotNull(credential.AuthorizationHeader);
        Assert.Equal("Bearer", credential.AuthorizationHeader.Scheme);
        Assert.Equal("xxxxx.yyyyy.zzzzz", credential.AuthorizationHeader.Parameter);
    }

    [Fact]
    public void Constructor_ReturnsBearerHeader()
    {
        // Arrange & Act
        var credential = new JwtCredential("my-jwt-token");

        // Assert
        Assert.Equal("Bearer", credential.AuthorizationHeader.Scheme);
        Assert.Equal("my-jwt-token", credential.AuthorizationHeader.Parameter);
    }

    [Fact]
    public void Equals_SameToken_ReturnsTrue()
    {
        var a = JwtCredential.Create("token123");
        var b = JwtCredential.Create("token123");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentTokens_ReturnsFalse()
    {
        var a = JwtCredential.Create("token1");
        var b = JwtCredential.Create("token2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void JwtCredential_ImplementsICredential()
    {
        ICredential credential = JwtCredential.Create("xxxxx.yyyyy.zzzzz");

        Assert.NotNull(credential.AuthorizationHeader);
        Assert.Equal("Bearer", credential.AuthorizationHeader!.Scheme);
    }

    [Fact]
    public void JwtCredential_NotEqualToBasicCredential()
    {
        ICredential basic = Credential.Create("user", "pass");
        ICredential jwt = JwtCredential.Create("token");

        Assert.NotEqual(basic, jwt);
    }
    [Fact]
    public void ToString_DoesNotLeakToken()
    {
        var credential = JwtCredential.Create("xxxxx.yyyyy.zzzzz");
        var str = credential.ToString();

        Assert.DoesNotContain("xxxxx.yyyyy.zzzzz", str);
        Assert.DoesNotContain("Bearer", str);
        Assert.DoesNotContain("AuthorizationHeader", str);
        Assert.Contains($"<{"xxxxx.yyyyy.zzzzz".Length} chars>", str);
    }
}

public class CertificateCredentialTests
{
    /// <summary>
    /// Creates a self-signed X.509 certificate with a private key for testing.
    /// </summary>
    private static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateSelfSignedCert()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=TestClient", rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        // Add client auth EKU
        request.CertificateExtensions.Add(
            new System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension(
                new System.Security.Cryptography.OidCollection
                {
                    new System.Security.Cryptography.Oid("1.3.6.1.5.5.7.3.2") // Client Authentication
                }, false));

        return request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
    }

    /// <summary>
    /// Creates a certificate WITHOUT a private key (public cert only).
    /// </summary>
    private static System.Security.Cryptography.X509Certificates.X509Certificate2 CreatePublicOnlyCert()
    {
        var fullCert = CreateSelfSignedCert();
        // Export only the public portion (DER) and reimport — strips the private key
        var publicBytes = fullCert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
        return System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificate(publicBytes);
    }

    [Fact]
    public void Create_WithValidCert_ReturnsNullAuthHeader()
    {
        using var cert = CreateSelfSignedCert();
        var credential = CertificateCredential.Create(cert);

        Assert.Null(credential.AuthorizationHeader);
    }

    [Fact]
    public void Create_WithoutPrivateKey_ThrowsArgumentException()
    {
        using var publicCert = CreatePublicOnlyCert();

        Assert.Throws<ArgumentException>(() => CertificateCredential.Create(publicCert));
    }

    [Fact]
    public void Create_WithNullCert_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CertificateCredential.Create(null!));
    }

    [Fact]
    public void Constructor_ExposesOriginalCertificate()
    {
        using var cert = CreateSelfSignedCert();
        var credential = new CertificateCredential(cert);

        Assert.Same(cert, credential.Certificate);
        Assert.True(credential.Certificate.HasPrivateKey);
    }

    [Fact]
    public void ToString_RedactsCertDetails()
    {
        using var cert = CreateSelfSignedCert();
        var credential = CertificateCredential.Create(cert);
        var str = credential.ToString();

        Assert.Contains("Subject = CN=TestClient", str);
        Assert.Contains("Thumbprint =", str);
        Assert.DoesNotContain("AuthorizationHeader", str);
    }

    [Fact]
    public void CertificateCredential_ImplementsICredential()
    {
        using var cert = CreateSelfSignedCert();
        ICredential credential = CertificateCredential.Create(cert);

        Assert.Null(credential.AuthorizationHeader);
    }

    [Fact]
    public void CertificateCredential_NotEqualToOtherCredentialTypes()
    {
        using var cert = CreateSelfSignedCert();
        ICredential certCred = CertificateCredential.Create(cert);
        ICredential basicCred = Credential.Create("user", "pass");
        ICredential jwtCred = JwtCredential.Create("token");

        Assert.NotEqual(certCred, basicCred);
        Assert.NotEqual(certCred, jwtCred);
    }
}
