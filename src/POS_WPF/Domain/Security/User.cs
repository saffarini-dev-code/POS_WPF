using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Security;

public sealed class User : Entity
{
    private readonly List<UserRole> _roles = [];

    private User() { }

    public User(string username, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        Username = username.Trim();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
    }

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<UserRole> Roles => _roles;

    public void ChangePasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new ArgumentException("Password hash is required.", nameof(hash));
        PasswordHash = hash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignRole(UserRole role)
    {
        if (_roles.All(x => x.RoleId != role.RoleId)) _roles.Add(role);
    }
}

public sealed class Role : Entity
{
    private readonly List<RolePermission> _permissions = [];

    private Role() { }

    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name is required.", nameof(name));
        Name = name.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public void AddPermission(RolePermission permission)
    {
        if (_permissions.All(x => x.PermissionId != permission.PermissionId)) _permissions.Add(permission);
    }
}

public sealed class UserRole
{
    private UserRole() { }
    public UserRole(Guid userId, Guid roleId) { UserId = userId; RoleId = roleId; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}

public sealed class RolePermission
{
    private RolePermission() { }
    public RolePermission(Guid roleId, string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        RoleId = roleId;
        PermissionId = Guid.NewGuid();
        PermissionCode = permissionCode.Trim();
    }
    public Guid PermissionId { get; private set; }
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
}
