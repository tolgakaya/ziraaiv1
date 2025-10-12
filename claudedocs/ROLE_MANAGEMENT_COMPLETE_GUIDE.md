# ZiraAI Role Management - Complete Guide

**Version**: 1.0
**Date**: 2025-10-08
**For**: Backend Team, Mobile Developers, System Administrators

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Database Schema](#database-schema)
4. [API Endpoints](#api-endpoints)
5. [Business Logic](#business-logic)
6. [Security & Authorization](#security--authorization)
7. [JWT Claims Integration](#jwt-claims-integration)
8. [Common Scenarios](#common-scenarios)
9. [Testing Guide](#testing-guide)
10. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### What is Role Management?

ZiraAI uses **role-based access control (RBAC)** to manage user permissions. The system supports:

- **Multiple roles per user**: Users can be Farmer AND Sponsor simultaneously
- **Dynamic role assignment**: Admins can add/remove roles without affecting user data
- **JWT-based authorization**: Roles stored as claims in authentication tokens
- **Aspect-oriented security**: `[SecuredOperation]` attribute for fine-grained control

### Available Roles

| Role ID | Role Name | Purpose | Default Assignment |
|---------|-----------|---------|-------------------|
| 1 | Admin | System administration | Manual (by existing Admin) |
| 2 | Farmer | End users who analyze plants | Auto (on registration) |
| 3 | Sponsor | Companies who purchase packages | Manual (Admin approval) |

### Key Features

- ✅ Users can have **multiple roles** simultaneously
- ✅ Role changes take effect **after JWT refresh** (re-login)
- ✅ **Admin-only** role assignment/removal via `[SecuredOperation]`
- ✅ Role hierarchy: Admin > Sponsor > Farmer
- ✅ Audit trail: CreatedDate, CreatedUserId tracking

---

## 🏗️ Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     ZiraAI Role System                       │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────▼──────┐    ┌────────▼────────┐    ┌──────▼──────┐
│   Database   │    │  Business Logic │    │   Security  │
│              │    │                 │    │             │
│ • Groups     │◄───┤ • Commands      │◄───┤ • JWT       │
│ • UserGroups │    │ • Queries       │    │ • Claims    │
│ • Users      │    │ • Validation    │    │ • Aspects   │
└──────────────┘    └─────────────────┘    └─────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │   API Controllers  │
                    │                    │
                    │ • GroupsController │
                    │ • UserGroupsCtrl   │
                    └────────────────────┘
```

### Data Flow

1. **Admin Request** → Controller validates JWT → `[SecuredOperation]` checks permissions
2. **Command Execution** → MediatR handler validates business rules → Repository updates database
3. **JWT Refresh** → User re-authenticates → New token includes updated roles
4. **Authorization** → Each protected endpoint checks role claims → Allow/Deny access

---

## 💾 Database Schema

### Groups Table

Defines available roles in the system.

```sql
CREATE TABLE "Groups" (
    "Id" SERIAL PRIMARY KEY,
    "GroupName" VARCHAR(50) NOT NULL UNIQUE
);

-- Seed Data
INSERT INTO "Groups" ("Id", "GroupName") VALUES
(1, 'Admin'),
(2, 'Farmer'),
(3, 'Sponsor');
```

**Fields**:
- `Id`: Primary key, role identifier
- `GroupName`: Unique role name (Admin, Farmer, Sponsor)

---

### UserGroups Table

Maps users to roles (many-to-many relationship).

```sql
CREATE TABLE "UserGroups" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "GroupId" INTEGER NOT NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedUserId" INTEGER,

    CONSTRAINT "FK_UserGroups_Users" FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserGroups_Groups" FOREIGN KEY ("GroupId")
        REFERENCES "Groups"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_UserGroups_UserId_GroupId" UNIQUE ("UserId", "GroupId")
);

CREATE INDEX "IX_UserGroups_UserId" ON "UserGroups"("UserId");
CREATE INDEX "IX_UserGroups_GroupId" ON "UserGroups"("GroupId");
```

**Fields**:
- `Id`: Primary key, relationship identifier
- `UserId`: Foreign key to Users table
- `GroupId`: Foreign key to Groups table
- `CreatedDate`: When role was assigned
- `CreatedUserId`: Admin who assigned the role

**Constraints**:
- Unique constraint on (UserId, GroupId) prevents duplicate assignments
- Cascade delete: if user deleted, roles are removed
- Cascade delete: if group deleted, assignments are removed

---

### Users Table

User accounts (simplified view for role management).

```sql
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "FirstName" VARCHAR(50),
    "LastName" VARCHAR(50),
    "PasswordHash" BYTEA,
    "PasswordSalt" BYTEA,
    "Status" BOOLEAN DEFAULT TRUE,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW()
);
```

---

### Entity Relationship Diagram

```
┌─────────────┐        ┌──────────────┐        ┌─────────────┐
│   Users     │        │  UserGroups  │        │   Groups    │
├─────────────┤        ├──────────────┤        ├─────────────┤
│ Id (PK)     │◄──────┤ UserId (FK)  │        │ Id (PK)     │
│ Email       │        │ GroupId (FK) ├───────►│ GroupName   │
│ FirstName   │        │ CreatedDate  │        └─────────────┘
│ LastName    │        │ CreatedUserId│
│ Status      │        └──────────────┘
└─────────────┘
```

---

## 🔌 API Endpoints

### Base Configuration

- **Base URL**: `{baseUrl}/api/v1/`
- **Version Header**: `x-dev-arch-version: 1.0`
- **Authorization**: `Authorization: Bearer {jwt_token}`

---

### 1. Get All Groups

**Purpose**: List all available roles in the system

**Endpoint**: `GET /api/v1/groups`

**Controller**: `GroupsController.cs`

**Authorization**: Any authenticated user

**Request**:
```http
GET {{baseUrl}}/api/v1/groups
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "groupName": "Admin"
    },
    {
      "id": 2,
      "groupName": "Farmer"
    },
    {
      "id": 3,
      "groupName": "Sponsor"
    }
  ],
  "message": null
}
```

**Implementation**:
```csharp
// File: WebAPI/Controllers/GroupsController.cs
[HttpGet]
public async Task<IActionResult> GetList()
{
    var result = await Mediator.Send(new GetGroupsQuery());
    if (result.Success)
        return Ok(result);
    return BadRequest(result);
}
```

---

### 2. Get User's Roles

**Purpose**: Retrieve all roles assigned to a specific user

**Endpoint**: `GET /api/v1/user-groups/users/{userId}/groups`

**Controller**: `UserGroupsController.cs`

**Authorization**: Admin or the user themselves

**Path Parameters**:
- `userId` (integer, required): Target user's ID

**Request**:
```http
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 150,
      "userId": 131,
      "groupId": 2,
      "groupName": "Farmer"
    },
    {
      "id": 151,
      "userId": 131,
      "groupId": 3,
      "groupName": "Sponsor"
    }
  ],
  "message": null
}
```

**Response** (404 Not Found):
```json
{
  "success": false,
  "data": null,
  "message": "User not found or has no roles"
}
```

**Implementation**:
```csharp
// File: WebAPI/Controllers/UserGroupsController.cs
[HttpGet("users/{userId}/groups")]
public async Task<IActionResult> GetUserGroups(int userId)
{
    var result = await Mediator.Send(new GetUserGroupsQuery { UserId = userId });
    if (result.Success)
        return Ok(result);
    return NotFound(result);
}
```

---

### 3. Assign Role to User

**Purpose**: Add a role to a user (e.g., make Farmer a Sponsor)

**Endpoint**: `POST /api/v1/user-groups`

**Controller**: `UserGroupsController.cs`

**Authorization**: **Admin only** (via `[SecuredOperation("UserGroup.Add")]`)

**Request**:
```http
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "userId": 131,
  "groupId": 3
}
```

**Request Body**:
```json
{
  "userId": 131,      // Target user's ID
  "groupId": 3        // Role ID (2=Farmer, 3=Sponsor, 1=Admin)
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": 152,
    "userId": 131,
    "groupId": 3,
    "groupName": "Sponsor",
    "createdDate": "2025-10-08T16:00:00"
  },
  "message": "Role successfully assigned to user"
}
```

**Response** (400 Bad Request - Already Has Role):
```json
{
  "success": false,
  "data": null,
  "message": "User already has this role"
}
```

**Response** (403 Forbidden - Not Admin):
```json
{
  "success": false,
  "data": null,
  "message": "You do not have permission to perform this operation"
}
```

**Response** (404 Not Found):
```json
{
  "success": false,
  "data": null,
  "message": "User or Group not found"
}
```

**Implementation**:
```csharp
// File: WebAPI/Controllers/UserGroupsController.cs
[HttpPost]
[SecuredOperation("UserGroup.Add")]
public async Task<IActionResult> Add([FromBody] CreateUserGroupCommand command)
{
    var result = await Mediator.Send(command);
    if (result.Success)
        return Ok(result);
    return BadRequest(result);
}

// File: Business/Handlers/UserGroups/Commands/CreateUserGroupCommand.cs
public class CreateUserGroupCommand : IRequest<IResult>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

// Handler validation:
// 1. Check if user exists
// 2. Check if group exists
// 3. Check if user already has this role (unique constraint)
// 4. Create UserGroup record
```

---

### 4. Remove Role from User

**Purpose**: Revoke a role from a user

**Endpoint**: `DELETE /api/v1/user-groups/{id}`

**Controller**: `UserGroupsController.cs`

**Authorization**: **Admin only** (via `[SecuredOperation("UserGroup.Delete")]`)

**Path Parameters**:
- `id` (integer, required): UserGroup relationship ID (NOT groupId)

**Request**:
```http
DELETE {{baseUrl}}/api/v1/user-groups/152
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": null,
  "message": "Role successfully removed from user"
}
```

**Response** (404 Not Found):
```json
{
  "success": false,
  "data": null,
  "message": "UserGroup relationship not found"
}
```

**Response** (403 Forbidden):
```json
{
  "success": false,
  "data": null,
  "message": "You do not have permission to perform this operation"
}
```

**Implementation**:
```csharp
// File: WebAPI/Controllers/UserGroupsController.cs
[HttpDelete("{id}")]
[SecuredOperation("UserGroup.Delete")]
public async Task<IActionResult> Delete(int id)
{
    var result = await Mediator.Send(new DeleteUserGroupCommand { Id = id });
    if (result.Success)
        return Ok(result);
    return BadRequest(result);
}

// File: Business/Handlers/UserGroups/Commands/DeleteUserGroupCommand.cs
public class DeleteUserGroupCommand : IRequest<IResult>
{
    public int Id { get; set; }
}

// Handler validation:
// 1. Check if UserGroup exists
// 2. Optional: Prevent removing last role from user
// 3. Delete UserGroup record
```

---

## 💼 Business Logic

### CQRS Implementation

#### Commands

**CreateUserGroupCommand**

```csharp
// File: Business/Handlers/UserGroups/Commands/CreateUserGroupCommand.cs

public class CreateUserGroupCommand : IRequest<IResult>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
}

public class CreateUserGroupCommandHandler : IRequestHandler<CreateUserGroupCommand, IResult>
{
    private readonly IUserGroupRepository _userGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;

    public CreateUserGroupCommandHandler(
        IUserGroupRepository userGroupRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository)
    {
        _userGroupRepository = userGroupRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }

    public async Task<IResult> Handle(CreateUserGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate user exists
        var user = await _userRepository.GetAsync(u => u.Id == request.UserId);
        if (user == null)
            return new ErrorResult("User not found");

        // 2. Validate group exists
        var group = await _groupRepository.GetAsync(g => g.Id == request.GroupId);
        if (group == null)
            return new ErrorResult("Group not found");

        // 3. Check if user already has this role
        var existingUserGroup = await _userGroupRepository.GetAsync(
            ug => ug.UserId == request.UserId && ug.GroupId == request.GroupId);

        if (existingUserGroup != null)
            return new ErrorResult("User already has this role");

        // 4. Create UserGroup
        var userGroup = new UserGroup
        {
            UserId = request.UserId,
            GroupId = request.GroupId,
            CreatedDate = DateTime.Now,
            CreatedUserId = GetCurrentUserId() // From JWT claims
        };

        _userGroupRepository.Add(userGroup);
        await _userGroupRepository.SaveChangesAsync();

        return new SuccessResult("Role successfully assigned to user");
    }
}
```

---

**DeleteUserGroupCommand**

```csharp
// File: Business/Handlers/UserGroups/Commands/DeleteUserGroupCommand.cs

public class DeleteUserGroupCommand : IRequest<IResult>
{
    public int Id { get; set; }
}

public class DeleteUserGroupCommandHandler : IRequestHandler<DeleteUserGroupCommand, IResult>
{
    private readonly IUserGroupRepository _userGroupRepository;

    public DeleteUserGroupCommandHandler(IUserGroupRepository userGroupRepository)
    {
        _userGroupRepository = userGroupRepository;
    }

    public async Task<IResult> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate UserGroup exists
        var userGroup = await _userGroupRepository.GetAsync(ug => ug.Id == request.Id);
        if (userGroup == null)
            return new ErrorResult("UserGroup not found");

        // 2. Optional: Prevent removing last role
        var userGroupCount = await _userGroupRepository.CountAsync(
            ug => ug.UserId == userGroup.UserId);

        if (userGroupCount <= 1)
            return new ErrorResult("Cannot remove last role from user");

        // 3. Delete UserGroup
        _userGroupRepository.Delete(userGroup);
        await _userGroupRepository.SaveChangesAsync();

        return new SuccessResult("Role successfully removed from user");
    }
}
```

---

#### Queries

**GetGroupsQuery**

```csharp
// File: Business/Handlers/Groups/Queries/GetGroupsQuery.cs

public class GetGroupsQuery : IRequest<IDataResult<List<Group>>>
{
}

public class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, IDataResult<List<Group>>>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<IDataResult<List<Group>>> Handle(GetGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.GetListAsync();
        return new SuccessDataResult<List<Group>>(groups.ToList(), "Groups retrieved successfully");
    }
}
```

---

**GetUserGroupsQuery**

```csharp
// File: Business/Handlers/UserGroups/Queries/GetUserGroupsQuery.cs

public class GetUserGroupsQuery : IRequest<IDataResult<List<UserGroupDto>>>
{
    public int UserId { get; set; }
}

public class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, IDataResult<List<UserGroupDto>>>
{
    private readonly IUserGroupRepository _userGroupRepository;

    public GetUserGroupsQueryHandler(IUserGroupRepository userGroupRepository)
    {
        _userGroupRepository = userGroupRepository;
    }

    public async Task<IDataResult<List<UserGroupDto>>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        var userGroups = await _userGroupRepository.GetUserGroupsWithDetailsAsync(request.UserId);

        var dtos = userGroups.Select(ug => new UserGroupDto
        {
            Id = ug.Id,
            UserId = ug.UserId,
            GroupId = ug.GroupId,
            GroupName = ug.Group?.GroupName
        }).ToList();

        return new SuccessDataResult<List<UserGroupDto>>(dtos, "User groups retrieved successfully");
    }
}
```

---

### Validation Rules

#### Business Validation

1. **User Existence**: User must exist in database before role assignment
2. **Group Existence**: Group must exist in database
3. **Duplicate Prevention**: User cannot have same role twice (unique constraint)
4. **Minimum Roles**: Users should have at least one role (optional enforcement)
5. **Role Hierarchy**: Only admins can assign Admin role (optional enforcement)

#### Data Validation

```csharp
// File: Business/Handlers/UserGroups/Validators/CreateUserGroupCommandValidator.cs

public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0");

        RuleFor(x => x.GroupId)
            .GreaterThan(0)
            .WithMessage("GroupId must be greater than 0")
            .LessThanOrEqualTo(3)
            .WithMessage("Invalid GroupId (valid: 1=Admin, 2=Farmer, 3=Sponsor)");
    }
}
```

---

## 🔒 Security & Authorization

### SecuredOperation Aspect

ZiraAI uses aspect-oriented programming for authorization via `[SecuredOperation]` attribute.

**Implementation**:

```csharp
// File: Core/Aspects/Autofac/SecuredOperation.cs

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class SecuredOperationAttribute : MethodInterceptionAspect
{
    private readonly string _claim;

    public SecuredOperationAttribute(string claim)
    {
        _claim = claim;
    }

    public override void OnInvoke(MethodInvocationArgs args)
    {
        var httpContextAccessor = args.Instance.GetType()
            .GetProperty("HttpContextAccessor")?.GetValue(args.Instance) as IHttpContextAccessor;

        var user = httpContextAccessor?.HttpContext?.User;

        if (user == null || !user.Identity.IsAuthenticated)
        {
            throw new UnauthorizedException("User is not authenticated");
        }

        // Check if user has required claim
        var claims = user.Claims.Select(c => c.Value).ToList();

        if (!claims.Contains(_claim) && !claims.Contains("Admin"))
        {
            throw new ForbiddenException($"You do not have permission: {_claim}");
        }

        args.Proceed();
    }
}
```

**Usage in Controllers**:

```csharp
// File: WebAPI/Controllers/UserGroupsController.cs

[HttpPost]
[SecuredOperation("UserGroup.Add")]  // Only users with this claim can execute
public async Task<IActionResult> Add([FromBody] CreateUserGroupCommand command)
{
    var result = await Mediator.Send(command);
    return Ok(result);
}

[HttpDelete("{id}")]
[SecuredOperation("UserGroup.Delete")]  // Only users with this claim can execute
public async Task<IActionResult> Delete(int id)
{
    var result = await Mediator.Send(new DeleteUserGroupCommand { Id = id });
    return Ok(result);
}
```

---

### Operation Claims

**Database Table**:

```sql
CREATE TABLE "OperationClaims" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE
);

-- Seed Data for Role Management
INSERT INTO "OperationClaims" ("Name") VALUES
('UserGroup.Add'),
('UserGroup.Delete'),
('UserGroup.Update'),
('Admin');
```

**UserOperationClaims Mapping**:

```sql
CREATE TABLE "UserOperationClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "OperationClaimId" INTEGER NOT NULL,

    CONSTRAINT "FK_UserOperationClaims_Users" FOREIGN KEY ("UserId")
        REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserOperationClaims_OperationClaims" FOREIGN KEY ("OperationClaimId")
        REFERENCES "OperationClaims"("Id") ON DELETE CASCADE
);
```

**Admin Auto-Assignment**:

When user is assigned Admin role (GroupId = 1), they automatically get all operation claims including `UserGroup.Add` and `UserGroup.Delete`.

---

## 🎫 JWT Claims Integration

### Claims Structure

When user authenticates, JWT token includes role claims:

```json
{
  "nameid": "131",
  "email": "user@example.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Farmer", "Sponsor"],
  "exp": 1696800000,
  "iss": "ziraai.com",
  "aud": "ziraai.com"
}
```

**Key Points**:
- Roles stored in standard Microsoft claim type
- Multiple roles = array of strings
- Single role = string (not array)

---

### Token Generation

**Authentication Service**:

```csharp
// File: Business/Services/Authentication/AuthenticationService.cs

public async Task<AccessToken> CreateAccessToken(User user)
{
    // 1. Get user's roles
    var userGroups = await _userGroupRepository.GetUserGroupsWithDetailsAsync(user.Id);
    var roles = userGroups.Select(ug => ug.Group.GroupName).ToList();

    // 2. Get user's operation claims
    var operationClaims = await _userOperationClaimRepository
        .GetOperationClaimsByUserIdAsync(user.Id);

    // 3. Build claims list
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

    // Add role claims
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    // Add operation claims
    foreach (var claim in operationClaims)
    {
        claims.Add(new Claim("OperationClaim", claim.Name));
    }

    // 4. Generate JWT
    var tokenOptions = new JwtSecurityToken(
        issuer: _tokenOptions.Issuer,
        audience: _tokenOptions.Audience,
        claims: claims,
        expires: DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration),
        signingCredentials: _tokenOptions.SigningCredentials
    );

    return new AccessToken
    {
        Token = new JwtSecurityTokenHandler().WriteToken(tokenOptions),
        Expiration = tokenOptions.ValidTo
    };
}
```

---

### Token Refresh After Role Changes

**Important**: Role changes do NOT auto-update existing tokens. Users must re-authenticate.

**Workflow**:
1. Admin assigns role to user
2. User's database record updated
3. **Existing JWT still has old roles**
4. User logs out and logs back in
5. New JWT generated with updated roles
6. User can now access role-protected endpoints

**Mobile Implementation**:

```dart
// After role assignment notification
void handleRoleAssigned() async {
  // 1. Show notification
  await showDialog(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('Role Updated'),
      content: Text('Your account permissions have been updated. Please log in again.'),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: Text('OK'),
        ),
      ],
    ),
  );

  // 2. Clear stored token
  await _secureStorage.delete(key: 'jwt_token');
  await _secureStorage.delete(key: 'refresh_token');

  // 3. Navigate to login
  Navigator.pushAndRemoveUntil(
    context,
    MaterialPageRoute(builder: (context) => LoginScreen()),
    (route) => false,
  );
}
```

---

## 📚 Common Scenarios

### Scenario 1: New User Registration (Auto Farmer Role)

**Flow**:
1. User registers via `/api/v1/auth/register`
2. User record created
3. **Automatic Farmer role assignment** in registration handler
4. JWT token generated with Farmer role
5. User can immediately access farmer features

**Implementation**:

```csharp
// File: Business/Handlers/Authentication/Commands/RegisterUserCommand.cs

public async Task<IDataResult<AccessToken>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
{
    // 1. Create user
    var user = new User
    {
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Status = true,
        CreatedDate = DateTime.Now
    };

    byte[] passwordHash, passwordSalt;
    HashingHelper.CreatePasswordHash(request.Password, out passwordHash, out passwordSalt);
    user.PasswordHash = passwordHash;
    user.PasswordSalt = passwordSalt;

    _userRepository.Add(user);
    await _userRepository.SaveChangesAsync();

    // 2. Auto-assign Farmer role (GroupId = 2)
    var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
    if (farmerGroup != null)
    {
        var userGroup = new UserGroup
        {
            UserId = user.Id,
            GroupId = farmerGroup.Id,
            CreatedDate = DateTime.Now
        };
        _userGroupRepository.Add(userGroup);
        await _userGroupRepository.SaveChangesAsync();
    }

    // 3. Generate JWT with Farmer role
    var accessToken = await _authenticationService.CreateAccessToken(user);

    return new SuccessDataResult<AccessToken>(accessToken, "User registered successfully");
}
```

---

### Scenario 2: Farmer Becomes Sponsor (Self-Service)

**Flow** (v2.0 - NEW):
1. Farmer clicks "Become Sponsor" button in profile
2. Farmer fills sponsor profile form:
   - Company information
   - Business email (will become login email)
   - Password (will enable email+password login)
3. Farmer submits `POST /api/v1/sponsorship/create-profile`
4. Backend automatically:
   - Creates sponsor profile record
   - Assigns Sponsor role (GroupId: 3)
   - Updates user's email to business email (if phone registration)
   - Sets user's password (if phone registration - they had no password before)
5. Farmer notified of success
6. Farmer logs out and back in (token refresh)
7. New JWT includes both Farmer AND Sponsor roles
8. Farmer can now login with:
   - Business email + password (NEW - for phone registrations)
   - Phone number + SMS OTP (still works)
9. Farmer can access sponsor features

**Note**: No admin approval needed - fully self-service!

**Phone Registration Benefit**: Users who registered with phone number (no email/password) can now login with traditional email+password after becoming a sponsor.

**API Calls**:

```http
# Admin checks user's current roles
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {admin_token}

# Response: User currently has only Farmer role
{
  "data": [
    { "id": 100, "groupId": 2, "groupName": "Farmer" }
  ]
}

# Admin assigns Sponsor role
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": 131,
  "groupId": 3
}

# Response: Sponsor role added
{
  "success": true,
  "data": {
    "id": 101,
    "userId": 131,
    "groupId": 3,
    "groupName": "Sponsor"
  }
}

# Verify assignment
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {admin_token}

# Response: User now has both roles
{
  "data": [
    { "id": 100, "groupId": 2, "groupName": "Farmer" },
    { "id": 101, "groupId": 3, "groupName": "Sponsor" }
  ]
}
```

---

### Scenario 3: Sponsor Wants to Stop Sponsoring (Keep Farmer)

**Flow**:
1. Sponsor submits "Stop sponsoring" request
2. Admin reviews (check for active sponsorships, outstanding codes, etc.)
3. Admin calls `DELETE /api/v1/user-groups/{userGroupId}`
4. Sponsor role removed, Farmer role retained
5. User notified
6. User logs out and back in
7. New JWT only includes Farmer role
8. User loses access to sponsor endpoints

**API Calls**:

```http
# Admin gets user's roles
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {admin_token}

# Response: User has both roles
{
  "data": [
    { "id": 100, "groupId": 2, "groupName": "Farmer" },
    { "id": 101, "groupId": 3, "groupName": "Sponsor" }
  ]
}

# Admin removes Sponsor role (id: 101)
DELETE {{baseUrl}}/api/v1/user-groups/101
Authorization: Bearer {admin_token}

# Response: Success
{
  "success": true,
  "message": "Role successfully removed from user"
}

# Verify removal
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {admin_token}

# Response: User now only has Farmer role
{
  "data": [
    { "id": 100, "groupId": 2, "groupName": "Farmer" }
  ]
}
```

---

### Scenario 4: Admin Promotes User to Admin

**Flow**:
1. Existing admin identifies trusted user
2. Admin calls `POST /api/v1/user-groups` with `groupId: 1`
3. User gets Admin role (in addition to existing roles)
4. User logs out and back in
5. New JWT includes Admin role + all operation claims
6. User can now manage other users' roles

**API Calls**:

```http
# Assign Admin role
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": 131,
  "groupId": 1
}

# Response: Admin role added
{
  "success": true,
  "data": {
    "id": 102,
    "userId": 131,
    "groupId": 1,
    "groupName": "Admin"
  }
}

# User's final roles
GET {{baseUrl}}/api/v1/user-groups/users/131/groups

# Response: User has all three roles
{
  "data": [
    { "id": 100, "groupId": 2, "groupName": "Farmer" },
    { "id": 101, "groupId": 3, "groupName": "Sponsor" },
    { "id": 102, "groupId": 1, "groupName": "Admin" }
  ]
}
```

---

### Scenario 5: Bulk Role Assignment (External Script)

**Use Case**: Assign Sponsor role to 100 users at once (e.g., migration, promotional campaign)

**PowerShell Script**:

```powershell
# File: scripts/bulk-assign-sponsor-role.ps1

$baseUrl = "https://api.ziraai.com"
$adminToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

$userIds = @(131, 132, 133, 134, 135) # ... up to 100

foreach ($userId in $userIds) {
    $body = @{
        userId = $userId
        groupId = 3  # Sponsor
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod `
            -Uri "$baseUrl/api/v1/user-groups" `
            -Method POST `
            -Headers @{
                "Authorization" = "Bearer $adminToken"
                "Content-Type" = "application/json"
                "x-dev-arch-version" = "1.0"
            } `
            -Body $body

        Write-Host "✅ User $userId: $($response.message)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ User $userId: $_" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 100  # Rate limiting
}

Write-Host "Bulk assignment complete"
```

---

## 🧪 Testing Guide

### Manual Testing with Postman

#### Test 1: Get All Groups

```http
GET {{baseUrl}}/api/v1/groups
Authorization: Bearer {{token}}
x-dev-arch-version: 1.0
```

**Expected**: 200 OK with 3 groups (Admin, Farmer, Sponsor)

---

#### Test 2: Get User's Roles (No Roles)

```http
GET {{baseUrl}}/api/v1/user-groups/users/999/groups
Authorization: Bearer {{admin_token}}
```

**Expected**: 404 Not Found (user doesn't exist or has no roles)

---

#### Test 3: Assign Sponsor Role to Farmer

```http
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{
  "userId": 131,
  "groupId": 3
}
```

**Expected**: 200 OK with new UserGroup record

---

#### Test 4: Duplicate Role Assignment (Should Fail)

```http
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{
  "userId": 131,
  "groupId": 3
}
```

**Expected**: 400 Bad Request ("User already has this role")

---

#### Test 5: Non-Admin Tries to Assign Role (Should Fail)

```http
POST {{baseUrl}}/api/v1/user-groups
Authorization: Bearer {{farmer_token}}
Content-Type: application/json

{
  "userId": 200,
  "groupId": 3
}
```

**Expected**: 403 Forbidden ("You do not have permission")

---

#### Test 6: Remove Role

```http
# First, get UserGroup ID
GET {{baseUrl}}/api/v1/user-groups/users/131/groups
Authorization: Bearer {{admin_token}}

# Then delete by UserGroup ID (e.g., 101)
DELETE {{baseUrl}}/api/v1/user-groups/101
Authorization: Bearer {{admin_token}}
```

**Expected**: 200 OK with success message

---

### Automated Testing

#### Unit Test: CreateUserGroupCommandHandler

```csharp
// File: Tests/Business/Handlers/UserGroups/CreateUserGroupCommandHandlerTests.cs

[Fact]
public async Task Handle_ValidRequest_CreatesUserGroup()
{
    // Arrange
    var mockUserRepo = new Mock<IUserRepository>();
    var mockGroupRepo = new Mock<IGroupRepository>();
    var mockUserGroupRepo = new Mock<IUserGroupRepository>();

    mockUserRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
        .ReturnsAsync(new User { Id = 131, Email = "test@example.com" });

    mockGroupRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Group, bool>>>()))
        .ReturnsAsync(new Group { Id = 3, GroupName = "Sponsor" });

    mockUserGroupRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<UserGroup, bool>>>()))
        .ReturnsAsync((UserGroup)null);  // No existing role

    var handler = new CreateUserGroupCommandHandler(
        mockUserGroupRepo.Object,
        mockUserRepo.Object,
        mockGroupRepo.Object
    );

    var command = new CreateUserGroupCommand
    {
        UserId = 131,
        GroupId = 3
    };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    mockUserGroupRepo.Verify(x => x.Add(It.IsAny<UserGroup>()), Times.Once);
    mockUserGroupRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
}

[Fact]
public async Task Handle_DuplicateRole_ReturnsError()
{
    // Arrange
    var mockUserRepo = new Mock<IUserRepository>();
    var mockGroupRepo = new Mock<IGroupRepository>();
    var mockUserGroupRepo = new Mock<IUserGroupRepository>();

    mockUserRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<User, bool>>>()))
        .ReturnsAsync(new User { Id = 131 });

    mockGroupRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<Group, bool>>>()))
        .ReturnsAsync(new Group { Id = 3, GroupName = "Sponsor" });

    mockUserGroupRepo.Setup(x => x.GetAsync(It.IsAny<Expression<Func<UserGroup, bool>>>()))
        .ReturnsAsync(new UserGroup { Id = 100, UserId = 131, GroupId = 3 });  // Existing role

    var handler = new CreateUserGroupCommandHandler(
        mockUserGroupRepo.Object,
        mockUserRepo.Object,
        mockGroupRepo.Object
    );

    var command = new CreateUserGroupCommand { UserId = 131, GroupId = 3 };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.False(result.Success);
    Assert.Equal("User already has this role", result.Message);
    mockUserGroupRepo.Verify(x => x.Add(It.IsAny<UserGroup>()), Times.Never);
}
```

---

#### Integration Test: Role Assignment E2E

```csharp
// File: Tests/Integration/RoleManagementIntegrationTests.cs

[Fact]
public async Task AssignRole_ValidRequest_UserCanAccessProtectedEndpoint()
{
    // Arrange
    var client = _factory.CreateClient();
    var adminToken = await GetAdminTokenAsync();

    // 1. Create test user
    var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
    {
        Email = "testuser@example.com",
        FirstName = "Test",
        LastName = "User",
        Password = "Test1234!"
    });
    registerResponse.EnsureSuccessStatusCode();
    var userData = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
    var userId = userData.Data.Id;

    // 2. Assign Sponsor role
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
    var assignRoleResponse = await client.PostAsJsonAsync("/api/v1/user-groups", new
    {
        UserId = userId,
        GroupId = 3  // Sponsor
    });
    assignRoleResponse.EnsureSuccessStatusCode();

    // 3. User logs in again (to get new JWT with Sponsor role)
    var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        Email = "testuser@example.com",
        Password = "Test1234!"
    });
    var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AccessToken>>();
    var newUserToken = loginData.Data.Token;

    // 4. Try accessing sponsor endpoint
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserToken);
    var sponsorEndpointResponse = await client.GetAsync("/api/v1/sponsorship/profile");

    // Assert: User with Sponsor role can access sponsor endpoint
    Assert.Equal(HttpStatusCode.OK, sponsorEndpointResponse.StatusCode);
}
```

---

### Testing Checklist

#### Functional Tests
- [ ] Get all groups (3 groups returned)
- [ ] Get user roles (empty array if no roles)
- [ ] Assign Farmer role to new user
- [ ] Assign Sponsor role to existing Farmer
- [ ] Assign Admin role to user
- [ ] Prevent duplicate role assignment
- [ ] Remove role from user
- [ ] Prevent non-admin from assigning roles
- [ ] Verify JWT refresh after role change

#### Edge Cases
- [ ] Assign role to non-existent user (404)
- [ ] Assign non-existent role (404)
- [ ] Delete non-existent UserGroup (404)
- [ ] Remove last role from user (optional business rule)
- [ ] Assign role with invalid groupId (validation error)
- [ ] Concurrent role assignments (race condition)

#### Security Tests
- [ ] Unauthorized user cannot access endpoints (401)
- [ ] Farmer cannot assign roles (403)
- [ ] Sponsor cannot assign roles (403)
- [ ] Only Admin can assign roles (200)
- [ ] JWT without role claim rejected (403)
- [ ] Expired JWT rejected (401)

---

## 🔧 Troubleshooting

### Issue 1: "User already has this role"

**Symptoms**: POST /user-groups returns 400 with duplicate message

**Cause**: Unique constraint on (UserId, GroupId) in database

**Solution**:
1. Check user's current roles: `GET /api/v1/user-groups/users/{userId}/groups`
2. Verify if role already exists
3. If duplicate, this is expected behavior (not an error)

---

### Issue 2: Role Not Appearing in JWT After Assignment

**Symptoms**: User assigned role but still can't access protected endpoint

**Cause**: JWT claims are set at login, not dynamically updated

**Solution**:
1. User must **log out and log back in**
2. New JWT will include updated roles
3. Alternative: Implement token refresh endpoint

**Prevention**:
- Show "Please re-login for changes to take effect" message after role assignment
- Implement auto-logout after role change

---

### Issue 3: "You do not have permission to perform this operation"

**Symptoms**: Admin gets 403 when trying to assign roles

**Cause**: Admin user lacks operation claims

**Solution**:
1. Check UserOperationClaims table for admin user:
   ```sql
   SELECT * FROM "UserOperationClaims" WHERE "UserId" = {adminUserId};
   ```
2. If missing `UserGroup.Add` claim, add it:
   ```sql
   INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
   SELECT {adminUserId}, "Id" FROM "OperationClaims" WHERE "Name" = 'UserGroup.Add';
   ```
3. Admin logs out and back in

---

### Issue 4: Cannot Remove Role (Last Role Constraint)

**Symptoms**: DELETE /user-groups returns error "Cannot remove last role"

**Cause**: Business rule prevents users from having zero roles

**Solution**:
1. Assign a different role first
2. Then remove the unwanted role
3. Or disable this business rule if not needed:
   ```csharp
   // Comment out in DeleteUserGroupCommandHandler
   // if (userGroupCount <= 1)
   //     return new ErrorResult("Cannot remove last role from user");
   ```

---

### Issue 5: Database Unique Constraint Violation

**Symptoms**: 500 error when assigning role, logs show SQL exception

**Cause**: Race condition - multiple requests trying to assign same role simultaneously

**Solution**:
1. Application already checks for existing role before insert
2. If this happens, it's a timing issue
3. Frontend should prevent double-click on "Assign Role" button
4. Backend: Catch unique constraint exception and return user-friendly message:
   ```csharp
   try
   {
       _userGroupRepository.Add(userGroup);
       await _userGroupRepository.SaveChangesAsync();
   }
   catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_UserGroups") == true)
   {
       return new ErrorResult("User already has this role");
   }
   ```

---

### Issue 6: JWT Token Size Too Large

**Symptoms**: HTTP headers exceed size limit, 431 error

**Cause**: User has too many roles or operation claims

**Solution**:
1. Limit number of roles per user (e.g., max 3)
2. Use role-based claims instead of individual operation claims
3. Implement claim compression
4. Increase server header size limit:
   ```csharp
   // Program.cs
   builder.Services.Configure<KestrelServerOptions>(options =>
   {
       options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB
   });
   ```

---

## 📊 Monitoring & Logging

### Key Metrics to Track

1. **Role Assignment Rate**: Number of role assignments per day/week
2. **Role Distribution**: Count of users per role (Farmer, Sponsor, Admin)
3. **Role Change Frequency**: How often users change roles
4. **Failed Authorization Attempts**: 403 errors on protected endpoints
5. **Duplicate Assignment Attempts**: How often duplicate role assignments are tried

### Logging Implementation

```csharp
// File: Business/Handlers/UserGroups/Commands/CreateUserGroupCommandHandler.cs

public async Task<IResult> Handle(CreateUserGroupCommand request, CancellationToken cancellationToken)
{
    _logger.LogInformation("Role assignment requested: UserId={UserId}, GroupId={GroupId}",
        request.UserId, request.GroupId);

    // ... validation logic

    try
    {
        _userGroupRepository.Add(userGroup);
        await _userGroupRepository.SaveChangesAsync();

        _logger.LogInformation("Role successfully assigned: UserGroupId={UserGroupId}, UserId={UserId}, GroupId={GroupId}",
            userGroup.Id, request.UserId, request.GroupId);

        return new SuccessResult("Role successfully assigned to user");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to assign role: UserId={UserId}, GroupId={GroupId}",
            request.UserId, request.GroupId);
        throw;
    }
}
```

---

## 📝 Summary

### Key Takeaways

1. **Multiple Roles**: Users can have Farmer + Sponsor + Admin simultaneously
2. **Admin-Only Management**: Only admins can assign/remove roles via `[SecuredOperation]`
3. **JWT Refresh Required**: Role changes take effect after re-login (token refresh)
4. **Database Constraints**: Unique constraint prevents duplicate role assignments
5. **Clean Architecture**: CQRS pattern with commands/queries for all operations

### Quick Reference

| Task | Endpoint | Method | Auth |
|------|----------|--------|------|
| List roles | `/api/v1/groups` | GET | Any |
| Get user's roles | `/api/v1/user-groups/users/{id}/groups` | GET | Admin/Self |
| Assign role | `/api/v1/user-groups` | POST | Admin |
| Remove role | `/api/v1/user-groups/{id}` | DELETE | Admin |

---

**Last Updated**: 2025-10-08
**Version**: 1.0
**Maintained By**: Backend Team
