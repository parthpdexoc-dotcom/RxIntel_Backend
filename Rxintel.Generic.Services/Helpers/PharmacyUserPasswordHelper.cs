using Microsoft.AspNetCore.Identity;
using Rxintel.Generic.Entities.Entities;

namespace Rxintel.Generic.Services.Helpers
{
    public static class PharmacyUserPasswordHelper
    {
        private static readonly PasswordHasher<PharmacyUser> Hasher = new();

        public static string HashPassword(PharmacyUser user, string plainTextPassword) =>
            Hasher.HashPassword(user, plainTextPassword);

        public static bool VerifyPassword(PharmacyUser user, string providedPassword, out bool rehashNeeded)
        {
            rehashNeeded = false;

            if (string.IsNullOrEmpty(user.Password))
                return false;

            var result = Hasher.VerifyHashedPassword(user, user.Password, providedPassword);
            switch (result)
            {
                case PasswordVerificationResult.Success:
                    return true;
                case PasswordVerificationResult.SuccessRehashNeeded:
                    rehashNeeded = true;
                    return true;
                case PasswordVerificationResult.Failed:
                    if (string.Equals(user.Password, providedPassword, StringComparison.Ordinal))
                    {
                        rehashNeeded = true;
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }
    }
}
