using System.Security.Cryptography;
using FotoTime.Application.Invitations;

namespace Infrastructure.Invitations;

public sealed class SecureInviteTokenGenerator : IInviteTokenGenerator
{
    private const int TokenBytes = 16;

    public string CreateToken()
    {
        Span<byte> buffer = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer);
    }
}
