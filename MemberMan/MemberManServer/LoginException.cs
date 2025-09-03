using System.Runtime.Serialization;

namespace MemberManServer;

/// <summary>
/// Indicates that a login failed.
/// </summary>
public class LoginException : Exception
{
    public LoginException() { }
    public LoginException(string message) : base(message) { }
    public LoginException(string message, Exception inner) : base(message, inner) { }
    protected LoginException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
