using CommandLine;
using Org.BouncyCastle.Ocsp;

// ==========================================================================
[Verb("list-users")]
public class ListUsersOptions
{
  [Option("db-file", Required = false, HelpText = "Path to the database file.")]
  public string? DatabaseFile { get; set; }
}

// ==========================================================================
[Verb("delete-user")]
public class DeleteUserOptions
{
  [Option("username", Required = true, HelpText = "Name of the user to delete.")]
  public string Username { get; set; } = default!;

  [Option("db-file", Required = false, HelpText = "Path to the database file.")]
  public string? DatabaseFile { get; set; }
}

// ==========================================================================
[Verb("create-user")]
public class CreateUserOptions
{

  [Option("username", Required = false, HelpText = "Name of the user to create.  If omitted, --email will be used instead.")]
  public string? Username { get; set; } = null;

  [Option("email", Required = true, HelpText = "Email address of the user to be created.")]
  public string Email { get; set; } = default!;

  [Option("password", Required = true, HelpText = "Password for the user.")]
  public string Password { get; set; } = default!;

  [Option("permissions", Required = false, HelpText = "Permissions that the user should be created with.")]
  public string? Permissions { get; set; } = null;

  /// <remarks>
  /// In lieu of actual DB options, we will assume that we are using a sqlite DB to store the data. 
  /// If Omitted, we will simply use the default location for the database.
  /// </remarks>
  [Option("db-file", Required = false, HelpText = "Path to the database file.")]
  public string? DatabaseFile { get; set; }
}