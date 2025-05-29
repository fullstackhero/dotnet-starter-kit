using System;

namespace FSH.Framework.Infrastructure.Auth;

public class User
{
    public Guid id { get; set; }
    public string email { get; set; } = default!;
    public string phone_number { get; set; } = default!;
    public string tckn { get; set; } = default!;
    public string password_hash { get; set; } = default!;
    public string first_name { get; set; } = default!;
    public string last_name { get; set; } = default!;
    public DateTime birth_date { get; set; }
    public bool is_identity_verified { get; set; }
    public bool is_phone_verified { get; set; }
    public bool is_email_verified { get; set; }
    public string status { get; set; } = default!;
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
} 