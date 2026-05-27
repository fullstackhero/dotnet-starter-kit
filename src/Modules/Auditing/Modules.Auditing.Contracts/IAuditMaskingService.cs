namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Result of running a masking pass over a payload. Carries the masked
/// payload and a count of how many fields were redacted, so the caller
/// can tag the envelope with <see cref="AuditTag.PiiMasked"/> only when
/// masking actually applied (and audit consumers can show a "redacted"
/// indicator with confidence).
/// </summary>
public readonly record struct MaskingResult(object Payload, int MaskedFieldCount)
{
    public bool Masked => MaskedFieldCount > 0;
}

/// <summary>
/// Masks or hashes sensitive fields before persistence or externalization.
/// </summary>
public interface IAuditMaskingService
{
    /// <summary>
    /// Returns the masked payload along with a count of redacted fields.
    /// Implementations must return the original payload reference unchanged
    /// (and <c>MaskedFieldCount = 0</c>) when no fields matched the masking
    /// rules — callers rely on the count to decide whether to tag the
    /// envelope with <see cref="AuditTag.PiiMasked"/>.
    /// </summary>
    MaskingResult ApplyMasking(object payload);
}
