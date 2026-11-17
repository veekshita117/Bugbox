# Bugbox REST API Reference

The Bugbox REST API is built with ASP.NET Core (.NET 8 LTS) and serves the Bugbox issue tracker frontend. All responses follow standard REST patterns and return JSON formatted data. Errors conform to RFC 7807 ProblemDetails.

---

## Base URLs & Interactive Documentation

- **Development API Base URL**: `http://localhost:5000/api` (or `https://localhost:5001/api`)
- **Swagger / OpenAPI Documentation**: `/swagger` (enabled across all environments including production)
- **Health Check Endpoint**: `/health` (returns `Healthy` with status `200 OK`)

---

## Authentication & Authorization

Bugbox uses stateless **JWT (JSON Web Token)** bearer tokens with passwords hashed using **BCrypt** (work factor 11).

Include the JWT in the HTTP `Authorization` header of all protected requests:

```http
Authorization: Bearer <your_jwt_token>
```

When an unauthenticated request is made or the token is expired, the API returns `401 Unauthorized`. If a user attempts an action outside their permissions (e.g., non-admin editing project settings or non-author deleting a comment), the API returns `403 Forbidden`.

---

## Standard Error Response (RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Invalid input provided.",
  "instance": "/api/projects",
  "errors": {
    "Name": ["Project name cannot exceed 100 characters."],
    "Key": ["Project key must be 2-10 uppercase alphanumeric characters."]
  }
}
```

---

## Endpoints

### 1. Authentication (`/api/auth`)

#### Register User
`POST /api/auth/register`
- **Request Body**:
  ```json
  {
    "name": "Jane Doe",
    "email": "jane@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Response**: `200 OK`
  ```json
  {
    "token": "eyJhbGciOi...",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Jane Doe",
      "email": "jane@example.com",
      "createdAt": "2026-11-17T10:00:00Z"
    }
  }
  ```
- **Error Codes**: `400 Bad Request` (validation failed), `409 Conflict` (email already exists).

#### Login User
`POST /api/auth/login`
- **Request Body**:
  ```json
  {
    "email": "jane@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Response**: `200 OK` (returns `AuthResponse` with JWT token and user info).
- **Error Codes**: `401 Unauthorized` (invalid email or password).

#### Get Current User
`GET /api/auth/me`
- **Headers**: `Authorization: Bearer <token>`
- **Response**: `200 OK` (returns `UserDto`).

---

### 2. Projects (`/api/projects`)

#### List User Projects
`GET /api/projects`
- **Headers**: `Authorization: Bearer <token>`
- **Response**: `200 OK` - Array of `ProjectDto` (only projects where current user is Admin or Member).

#### Create Project
`POST /api/projects`
- **Headers**: `Authorization: Bearer <token>`
- **Request Body**:
  ```json
  {
    "name": "E-Commerce Core",
    "key": "ECOMM",
    "description": "Next-gen checkout pipeline and inventory service."
  }
  ```
- **Response**: `201 Created`
  - Current user is automatically enrolled as `Admin` and Owner.
- **Error Codes**: `400 Bad Request` (Key format), `409 Conflict` (Key already in use).

#### Get Project By ID
`GET /api/projects/{id}`
- **Headers**: `Authorization: Bearer <token>`
- **Response**: `200 OK` - `ProjectDto` with current user's role.
- **Error Codes**: `404 Not Found`, `403 Forbidden` (non-member).

#### Update Project
`PUT /api/projects/{id}`
- **Headers**: `Authorization: Bearer <token>`
- **Permission**: Project `Admin` only.
- **Request Body**:
  ```json
  {
    "name": "E-Commerce Core Platform",
    "description": "Updated project description."
  }
  ```
- **Response**: `200 OK` - Updated `ProjectDto`.

#### Delete Project
`DELETE /api/projects/{id}`
- **Headers**: `Authorization: Bearer <token>`
- **Permission**: Project `Admin` only.
- **Response**: `204 No Content`. Cascades delete to members, sprints, issues, labels, comments.

---

### 3. Project Members (`/api/projects/{id}/members`)

#### List Members
`GET /api/projects/{id}/members`
- **Headers**: `Authorization: Bearer <token>`
- **Response**: `200 OK` - Array of `ProjectMemberDto` with user details and roles.

#### Add Member by Email
`POST /api/projects/{id}/members`
- **Headers**: `Authorization: Bearer <token>`
- **Permission**: Project `Admin` only.
- **Request Body**:
  ```json
  {
    "email": "sarah.chen@example.com",
    "role": "Member"
  }
  ```
- **Response**: `200 OK` - `ProjectMemberDto`.
- **Error Codes**: `404 Not Found` (user not found), `409 Conflict` (already a member).

#### Remove Member
`DELETE /api/projects/{id}/members/{userId}`
- **Headers**: `Authorization: Bearer <token>`
- **Permission**: Project `Admin` only (Owner cannot be removed).
- **Response**: `204 No Content`.

---

### 4. Issues (`/api/projects/{projectId}/issues` & `/api/issues`)

#### List Issues with Filtering & Pagination
`GET /api/projects/{projectId}/issues`
- **Query Parameters**:
  - `status` (Optional): `ToDo`, `InProgress`, `InReview`, `Done`
  - `assigneeId` (Optional GUID)
  - `sprintId` (Optional GUID)
  - `type` (Optional): `Bug`, `Feature`, `Task`
  - `priority` (Optional): `Low`, `Medium`, `High`
  - `labelId` (Optional GUID)
  - `search` (Optional string for title search)
  - `page` (Default: `1`)
  - `pageSize` (Default: `20`, Max: `100`)
- **Response**: `200 OK` - `PagedResult<IssueDto>`:
  ```json
  {
    "items": [
      {
        "id": "8c593450-96f7-4148-bb0e-cc1846b0a944",
        "projectId": "1c3852ba-8ae0-4f51-a968-3e421096734d",
        "projectKey": "ECOMM",
        "issueKey": "ECOMM-12",
        "number": 12,
        "title": "Fix cart calculation rounding error",
        "description": "Prices ending in .99 occasionally lose a cent during discount application.",
        "type": "Bug",
        "priority": "High",
        "status": "InProgress",
        "boardOrder": 0,
        "assignee": { "id": "...", "name": "Jane Doe", "email": "..." },
        "reporter": { "id": "...", "name": "Marcus R", "email": "..." },
        "sprintId": "5865e94b-970a-4861-a548-dbde7e0965d8",
        "labels": [{ "id": "...", "name": "backend", "color": "#10B981" }],
        "createdAt": "2026-11-17T08:00:00Z",
        "updatedAt": "2026-11-17T09:30:00Z",
        "completedAt": null,
        "commentCount": 3
      }
    ],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
  ```

#### Create Issue
`POST /api/projects/{projectId}/issues`
- **Request Body**:
  ```json
  {
    "title": "Payment gateway webhook timeout",
    "description": "Stripe webhook retry bursts cause temporary connection pool starvation.",
    "type": "Bug",
    "priority": "High",
    "status": "ToDo",
    "assigneeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "sprintId": null,
    "labelIds": ["2b704c77-4958-452f-a3ff-18d09f7b11d9"]
  }
  ```
- **Sequential Per-Project Issue Key Generation**:
  - Issue number is allocated inside a `Serializable` database transaction using `MAX(Number) + 1`. Key becomes `PROJECTKEY-NUMBER` (e.g. `ECOMM-13`).
- **Response**: `201 Created` - `IssueDto`.

#### Get Issue By ID
`GET /api/issues/{id}`
- **Response**: `200 OK` - `IssueDto`.

#### Update Issue
`PUT /api/issues/{id}`
- **Request Body**: Same fields as create (updates title, description, priority, type, status, assignee, sprint, labels).
- **CompletedAt Logic**: If status moves to `Done`, `CompletedAt` is set to `DateTime.UtcNow`. If it moves out of `Done`, `CompletedAt` is set to `null`.
- **Response**: `200 OK` - `IssueDto`.

#### Move Kanban Card (Optimistic Drag & Drop)
`PATCH /api/issues/{id}/move`
- **Request Body**:
  ```json
  {
    "status": "InReview",
    "boardOrder": 2
  }
  ```
- **Response**: `200 OK` - `IssueDto`. Shifts neighbor board orders and sets/clears `CompletedAt` automatically.

#### Update Issue Status Only
`PATCH /api/issues/{id}/status`
- **Request Body**:
  ```json
  {
    "status": "Done"
  }
  ```
- **Response**: `200 OK` - `IssueDto`.

#### Delete Issue
`DELETE /api/issues/{id}`
- **Permission**: Project `Admin` or the original `Reporter` only.
- **Response**: `204 No Content`.

---

### 5. Sprints & Burndown (`/api/sprints` & `/api/projects/{projectId}/sprints`)

#### List Project Sprints
`GET /api/projects/{projectId}/sprints`
- **Response**: `200 OK` - Array of `SprintDto` ordered by Active first, then Planned, then Completed. Includes `totalIssues` and `completedIssues` counts.

#### Create Sprint
`POST /api/projects/{projectId}/sprints`
- **Request Body**:
  ```json
  {
    "name": "Sprint 3 - Checkout Flow",
    "goal": "Finalize checkout review page and one-click purchase.",
    "startDate": "2026-11-20T00:00:00Z",
    "endDate": "2026-12-04T00:00:00Z"
  }
  ```
- **Response**: `201 Created` - `SprintDto` (initial status `Planned`).

#### Start Sprint
`POST /api/sprints/{id}/start`
- **Constraint**: Only **one** Active sprint allowed per project.
- **Error Codes**: `400 Bad Request` if another sprint is currently Active.
- **Response**: `200 OK` - Updated `SprintDto` (status `Active`).

#### Complete Sprint
`POST /api/sprints/{id}/complete`
- **Request Body**:
  ```json
  {
    "moveUnfinishedToSprintId": null
  }
  ```
- **Behavior**: Unfinished issues (status != `Done`) either roll over to `moveUnfinishedToSprintId` (if provided) or return to the project backlog (`SprintId = null`).
- **Response**: `200 OK` - Updated `SprintDto` (status `Completed`).

#### Delete Sprint
`DELETE /api/sprints/{id}`
- **Constraint**: Only `Planned` sprints can be deleted. Active and Completed sprints are protected.
- **Response**: `204 No Content`.

#### Add Issues to Sprint
`POST /api/sprints/{id}/issues`
- **Request Body**:
  ```json
  {
    "issueIds": ["8c593450-96f7-4148-bb0e-cc1846b0a944"]
  }
  ```
- **Response**: `204 No Content`.

#### Remove Issue from Sprint (Return to Backlog)
`DELETE /api/sprints/{id}/issues/{issueId}`
- **Response**: `204 No Content` (sets issue `SprintId = null`).

#### Burndown Chart Analytics
`GET /api/sprints/{id}/burndown`
- **Response**: `200 OK` - Array of `BurndownPointDto`:
  ```json
  [
    { "date": "2026-11-20T00:00:00Z", "remaining": 12, "ideal": 12.0 },
    { "date": "2026-11-21T00:00:00Z", "remaining": 10, "ideal": 11.14 },
    { "date": "2026-11-22T00:00:00Z", "remaining": 9, "ideal": 10.29 },
    ...
    { "date": "2026-12-04T00:00:00Z", "remaining": 0, "ideal": 0.0 }
  ]
  ```
- **Algorithm**:
  - `ideal` represents the linear slope from total sprint issues at Day 0 down to 0 on the sprint end date.
  - `remaining` counts issues where `Status == Done && CompletedAt <= currentDay`. Re-opened issues have `CompletedAt` cleared to `null` so they return to the remaining count.

---

### 6. Labels (`/api/labels` & `/api/projects/{projectId}/labels`)

#### List Project Labels
`GET /api/projects/{projectId}/labels`
- **Response**: `200 OK` - Array of `LabelDto`.

#### Create Label
`POST /api/projects/{projectId}/labels`
- **Request Body**:
  ```json
  {
    "name": "performance",
    "color": "#F59E0B"
  }
  ```
- **Response**: `201 Created` - `LabelDto`.

#### Assign Label to Issue
`POST /api/issues/{id}/labels/{labelId}`
- **Response**: `200 OK` - Updated `IssueDto`.

#### Remove Label from Issue
`DELETE /api/issues/{id}/labels/{labelId}`
- **Response**: `200 OK` - Updated `IssueDto`.

---

### 7. Comments (`/api/comments` & `/api/issues/{issueId}/comments`)

#### List Comments for Issue
`GET /api/issues/{issueId}/comments`
- **Response**: `200 OK` - Array of `CommentDto` ordered chronologically.

#### Add Comment
`POST /api/issues/{issueId}/comments`
- **Request Body**:
  ```json
  {
    "body": "Fixed in PR #42. Added unit tests for decimal price rounding."
  }
  ```
- **Response**: `201 Created` - `CommentDto`.

#### Delete Comment
`DELETE /api/comments/{id}`
- **Permission**: Comment author or project `Admin` only.
- **Response**: `204 No Content`.

---

### 8. Demo Seed (`/api/demo/seed`)

#### Seed Realistic Demo Project
`POST /api/demo/seed`
- **Headers**: `Authorization: Bearer <token>`
- **Description**: Generates a populated showcase project `Bugbox Mobile Client` (`MOB`) with sample team members (Sarah, Marcus, Elena), standard sprint cycles (1 Completed sprint and 1 Active sprint), issues distributed across Kanban columns (ToDo, InProgress, InReview, Done with `CompletedAt`), project backlog items, and issue comments.
- **Response**: `201 Created` - `ProjectDto`.
