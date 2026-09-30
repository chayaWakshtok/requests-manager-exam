namespace RequestsManager.Application.Abstractions;

/// <summary>The user performing the current operation (used for ChangedBy in the audit).</summary>
public interface ICurrentUser
{
    string Name { get; }
}
