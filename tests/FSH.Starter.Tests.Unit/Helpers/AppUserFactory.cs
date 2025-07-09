using FSH.Framework.Core.Auth.Domain;
using System;

namespace FSH.Starter.Tests.Unit.Helpers
{
    public static class AppUserFactory
    {
        public static AppUser Create(
            Guid? id = null,
            string? email = null,
            string? username = null,
            string? phoneNumber = null,
            string? tckn = null,
            string? passwordHash = null,
            string? firstName = null,
            string? lastName = null,
            int? professionId = null,
            DateTime? birthDate = null,
            string? memberNumber = null,
            bool isEmailVerified = false,
            bool marketingConsent = false,
            bool electronicCommunicationConsent = false,
            bool membershipAgreementConsent = false,
            string status = "ACTIVE",
            string? registrationIp = null,
            DateTime? createdAt = null,
            DateTime? updatedAt = null
        )
        {
            return AppUser.FromRepository(
                id ?? Guid.NewGuid(),
                email ?? $"test_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
                username ?? $"testuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
                phoneNumber ?? "+905551112233",
                tckn ?? "12345678901",
                passwordHash ?? "hashedpassword",
                firstName ?? "Test",
                lastName ?? "User",
                professionId,
                birthDate ?? new DateTime(1990, 1, 1),
                memberNumber ?? null,
                isEmailVerified,
                marketingConsent,
                electronicCommunicationConsent,
                membershipAgreementConsent,
                status,
                registrationIp ?? "127.0.0.1",
                createdAt ?? DateTime.UtcNow,
                updatedAt ?? DateTime.UtcNow
            );
        }
    }
}
