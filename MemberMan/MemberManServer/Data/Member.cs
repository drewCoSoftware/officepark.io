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
  private static Member _Anon = null!;
  public static Member Anonymous
  {
    get
    {
      return _Anon ?? (_Anon = new Member()
      {
        Username = "anonymous",
        Email = "anon@noemail.com",
        Permissions = null,
        IsAnonymous = true,
      });
    }
  }

  public int ID { get; set; }

  [Unique]
  public string Username { get; set; } = null!;

  [Unique]
  public string Email { get; set; } = null!;

  /// <summary>
  /// When was this member created?
  /// </summary>
  public DateTimeOffset CreatedOn { get; set; } = DateTime.MinValue;

  /// <summary>
  /// When was this member modified?  Useful for caching permissions, etc.
  /// </summary>
  public DateTimeOffset ModifiedOn { get; set; } = DateTimeOffset.MinValue;

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

  // TODO: This should go live in the database!
  [JsonIgnore]
  public DateTimeOffset LastActive { get; set; }

  [JsonIgnore]
  public string IP { get; set; }
  [JsonIgnore]
  public string CookieVal { get; set; }

  [JsonIgnore]
  public bool IsLoggedIn { get; set; } = false;

  [JsonIgnore]
  public bool IsAnonymous { get; set; } = false;

  // --------------------------------------------------------------------------------------------------------------------------
  // REFACTOR:  This can go live with the permissions helpers....
  /// <summary>
  /// Checks to see if this member has a certain permission.
  /// </summary>
  public bool HasPermission(string permissionName, params int[]? ids)
  {
    if (ids != null)
    {
      string idPart = ":" + string.Join(";", ids);
      permissionName = permissionName + idPart;
    }

    string[] perms = this.Permissions.Split(",");
    bool res = perms.Contains(permissionName);
    return res;
  }
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


