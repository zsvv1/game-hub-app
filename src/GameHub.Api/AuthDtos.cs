using System.ComponentModel.DataAnnotations;

namespace GameHub.Api;

// These are simple DTOs used by Program.cs.
// Keeping them in a separate file avoids the CS8803 error.
internal record RegisterRequest([property: EmailAddress] string Email, [property: MinLength(6)] string Password);
internal record LoginRequest([property: EmailAddress] string Email, string Password);
