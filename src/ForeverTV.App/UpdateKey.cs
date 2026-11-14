namespace ForeverTV.App;

/// <summary>
/// Public half of the update signing key (ADR 0010). The release workflow signs update.json with the private half
/// (GitHub secret UPDATE_SIGNING_KEY); the updater accepts only manifests signed by it.
/// </summary>
internal static class UpdateKey
{
    public const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEm0jiJzgwlXPXp7c0MW9Li2KFV8ev
        XnOJaUY9NoxRYoxT8Jb3+6rPXVOb5WcY0gLkhg0GjRvxygT+Dw14xiEWMQ==
        -----END PUBLIC KEY-----
        """;
}
