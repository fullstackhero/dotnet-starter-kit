﻿namespace FSH.Framework.Identity.Contracts.v1.Tokens.RefreshToken;
public sealed record RefreshTokenCommandResponse(
    string Token,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime);