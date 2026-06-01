namespace Online.Models;

public record EmailPayload(
    List<EmailTarget> To,
    List<EmailTarget>? Cc = null,
    List<EmailTarget>? Bcc = null,
    bool IsHtml = true,
    string? ReplyTo = null
);

public record EmailTarget(string Email, string Name = "");
