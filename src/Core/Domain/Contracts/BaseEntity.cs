using System;
using MassTransit;

namespace DN.WebApi.Domain.Contracts
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// Global Unique Identifier.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// String representation of Unique Identifier.
        /// </summary>
        /// <example>abc (max 50 characters).</example>
        public string Referral { get;  set; }

        /// <summary>
        /// Concurrency token.
        /// </summary>
        public Guid ConcurrencyStamp { get; set; }

        protected BaseEntity()
        {
            Id = NewId.Next().ToGuid();
            Referral = string.IsNullOrWhiteSpace(Referral) ? Id.ToString() : Referral;
        }
    }
}