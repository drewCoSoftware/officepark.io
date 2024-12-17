using CommandLine;
using DataHelpers.Data;
using DotLiquid;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace officepark.io.Membership;



// ============================================================================================================================
public class Member : IHasPrimary
{
  public int ID { get; set; }

  [Unique]
  public string Username { get; set; } = null!;

  [Unique]
  public string Email { get; set; } = null!;

  public DateTimeOffset CreatedOn { get; set; } = DateTime.MinValue;

  [Unique]
  [IsNullable]
  public string? VerificationCode { get; set; } = null;

  public DateTimeOffset VerificationExpiration { get; set; } = DateTime.MinValue;

  [IsNullable]
  public DateTimeOffset? VerifiedOn { get; set; } = null;

  public bool IsVerified { get { return VerifiedOn != null && VerifiedOn > CreatedOn; } }

  [IsNullable]
  [Unique]
  public string? ResetToken { get; set; } = null;

  [IsNullable]
  public DateTimeOffset? TokenExpires { get; set; } = null;

  // TODO:
  // Indicates that the user should be prompted to change their password on the next login.
  // public bool ResetPassword { get; set; } = true;

  /// <summary>
  /// Comma delimited list of permissions, to be interpreted by the application.
  /// </summary>
  public string? Permissions { get; set; } = null;

  /// <summary>
  /// The hashed password.
  /// </summary>
  public string Password { get; set; } = null!;

  // This data is used with the current session.
  [JsonIgnore]
  public DateTimeOffset LoggedInSince { get; set; }
  [JsonIgnore]
  public DateTimeOffset LastActive { get; set; }
  [JsonIgnore]
  public string IP { get; set; }
  [JsonIgnore]
  public string CookieVal { get; set; }

  [JsonIgnore]
  public bool IsLoggedIn { get; set; } = false;
}







// ============================================================================================================================
// NOTE: This is .netcore specific.
public class HttpException : Exception
{
  public int StatusCode { get; private set; }
  public HttpException(int statusCode, string msg) : base(msg)
  {
    StatusCode = statusCode;
  }
}


