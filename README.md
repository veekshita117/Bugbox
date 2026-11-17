# Bugbox

A lightweight, Jira-style issue tracker for student and hackathon teams. Teams create projects, track issues on a Kanban board, plan work in sprints, and follow progress with a burndown chart.

Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, **SQL Server**, and **React + TypeScript**, with CI/CD on GitHub Actions and hosting on Azure.

---

## Features

- **Authentication**: register and log in with JWT bearer tokens; passwords hashed with BCrypt.
- **Projects and roles**: each project has a short key (for example `BUG`) and members with `Admin` or `Member` roles. Only members can see a project; only admins can edit it or manage members.
- **Issues**: bugs, features and tasks with priority, status, assignee, reporter and labels. Issues get readable per-project keys such as `BUG-12`.
- **Filtering and search**: filter by status, assignee, label, sprint, type and priority, with text search and pagination.
- **Kanban board**: columns for To Do, In Progress, In Review and Done, with drag and drop. Moves update the UI immediately and roll back if the server rejects them.
- **Backlog and sprints**: unscheduled issues live in the backlog. Sprints move through Planned, Active and Completed, with one active sprint per project. Completing a sprint sends unfinished issues back to the backlog or into another sprint.
- **Burndown chart**: daily remaining work compared with an ideal line for each sprint.
- **Labels and comments**: project-scoped colour labels, and comment threads on every issue.
- **Sample data**: one endpoint (and a button in the UI) creates a populated sample project.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8), Entity Framework Core 8 |
| Database | SQL Server (LocalDB or Docker locally, Azure SQL in production) |
| Auth | JWT bearer tokens, BCrypt.Net |
| Validation | FluentValidation |
| API docs | Swagger / OpenAPI with JWT support |
| Telemetry | Application Insights |
| Frontend | React 18, TypeScript, Vite |
| Routing and data | React Router 6, TanStack Query 5 |
| UI | Tailwind CSS, dnd-kit (drag and drop), Recharts (charts), Lucide icons |
| Backend tests | xUnit, FluentAssertions, EF Core InMemory, Coverlet |
| Frontend tests | Vitest, React Testing Library, jsdom |
| CI/CD | GitHub Actions |
| Hosting | Azure App Service (API), Azure Static Web Apps (frontend), Azure SQL Database |

---

## Architecture

The backend is a layered solution with thin controllers, business logic behind interfaces, and EF Core for data access.

```
React SPA (Vite)
      |  HTTPS + JWT
      v
Controllers            thin, route and authorise only
      |
FluentValidation       request validation
      |
Services (interfaces)  business rules, permissions, transactions
      |
EF Core DbContext
      |
SQL Server / Azure SQL
```

### Project structure

```
Bugbox/
├── .github/workflows/        ci.yml, deploy.yml
├── backend/
│   ├── Bugbox.sln
│   ├── src/
│   │   ├── Bugbox.Api/            controllers, validators, middleware, Program.cs
│   │   ├── Bugbox.Core/           entities, enums, DTOs, interfaces, exceptions
│   │   └── Bugbox.Infrastructure/ DbContext, configurations, migrations, services
│   └── tests/Bugbox.Tests/        xUnit tests
├── frontend/
│   └── src/
│       ├── api/                   typed API client and types
│       ├── components/            shared UI and layout
│       ├── context/               auth and toast providers
│       ├── features/              auth, projects, board, backlog, sprints, issues, settings
│       └── test/                  Vitest tests
└── docs/api.md                    endpoint reference
```

### Design rules

- Controllers never touch the database; they call services through interfaces such as `IIssueService` and `ISprintService`.
- API input and output use DTOs only, so EF entities are never exposed.
- A global exception middleware turns domain exceptions (not found, forbidden, conflict, bad request) into consistent `ProblemDetails` responses.
- The current user always comes from the JWT claims through `ICurrentUserService`.

---

## Data Model

| Table | Purpose | Key columns |
|---|---|---|
| `Users` | Accounts | `Id`, `Name`, `Email` (unique), `PasswordHash`, `CreatedAt` |
| `Projects` | Workspaces | `Id`, `Name`, `Key` (unique), `Description`, `OwnerId`, `CreatedAt` |
| `ProjectMembers` | Membership and role | `ProjectId`, `UserId` (composite key), `Role` |
| `Sprints` | Time-boxed work periods | `Id`, `ProjectId`, `Name`, `Goal`, `StartDate`, `EndDate`, `Status` |
| `Issues` | Work items | `Id`, `ProjectId`, `SprintId` (nullable), `Number`, `Title`, `Description`, `Type`, `Priority`, `Status`, `AssigneeId`, `ReporterId`, `BoardOrder`, `CreatedAt`, `UpdatedAt`, `CompletedAt` |
| `Labels` | Project-scoped tags | `Id`, `ProjectId`, `Name`, `Color` |
| `IssueLabels` | Issue to label link | `IssueId`, `LabelId` (composite key) |
| `Comments` | Discussion | `Id`, `IssueId`, `AuthorId`, `Body`, `CreatedAt` |

Enums: `IssueStatus` (ToDo, InProgress, InReview, Done), `IssuePriority` (Low, Medium, High), `IssueType` (Bug, Feature, Task), `ProjectRole` (Admin, Member), `SprintStatus` (Planned, Active, Completed).

Indexes cover every foreign key, plus `(ProjectId, Status)` and a unique `(ProjectId, Number)` on issues. Schema changes are managed with EF Core migrations.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18 or later](https://nodejs.org/) and npm
- SQL Server (LocalDB, Docker, or Azure SQL)
- The EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Backend

```bash
cd backend/src/Bugbox.Api

# Store local secrets outside the repository
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=BugboxDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SecretKey" "<a-random-string-of-at-least-32-characters>"

# Create the database
dotnet ef database update --project ../Bugbox.Infrastructure --startup-project .

# Run the API on port 5000 (the frontend dev server proxies to this port)
dotnet run --urls http://localhost:5000
```

- Swagger UI: `http://localhost:5000/swagger`
- Health check: `http://localhost:5000/health`

### 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

The app runs at `http://localhost:5173`. During development, requests to `/api` are proxied to `http://localhost:5000`.

### 3. Tests

```bash
# Backend, with coverage
cd backend
dotnet test tests/Bugbox.Tests --collect:"XPlat Code Coverage"

# Frontend
cd frontend
npm test
```

---

## Configuration

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | API config / user-secrets / App Service settings | SQL Server connection string |
| `Jwt:SecretKey` | API config / user-secrets / App Service settings | Token signing key (set your own; use at least 32 characters) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `appsettings.json` | Token settings (default expiry is 24 hours) |
| `Cors:AllowedOrigins` | `appsettings.json` / App Service settings | Origins allowed to call the API |
| `ApplicationInsights:ConnectionString` | App Service settings | Enables telemetry when set |
| `VITE_API_URL` | Frontend build environment | Base URL of the API for production builds; leave empty in development to use the proxy |

---

## API Overview

Every endpoint except register and login requires a `Bearer` token. The full reference with payloads and status codes is in [docs/api.md](docs/api.md), and Swagger documents the live API.

| Resource | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` |
| Projects | `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}` |
| Members | `GET/POST /api/projects/{id}/members`, `DELETE /api/projects/{id}/members/{userId}` |
| Issues | `GET/POST /api/projects/{projectId}/issues`, `GET/PUT/DELETE /api/issues/{id}` |
| Issue actions | `PATCH /api/issues/{id}/status`, `PATCH /api/issues/{id}/move`, `POST/DELETE /api/issues/{id}/labels/{labelId}` |
| Sprints | `GET/POST /api/projects/{projectId}/sprints`, `GET/PUT/DELETE /api/sprints/{id}` |
| Sprint actions | `POST /api/sprints/{id}/start`, `POST /api/sprints/{id}/complete`, `POST /api/sprints/{id}/issues`, `DELETE /api/sprints/{id}/issues/{issueId}`, `GET /api/sprints/{id}/burndown` |
| Labels | `GET/POST /api/projects/{projectId}/labels`, `PUT/DELETE /api/labels/{id}` |
| Comments | `GET/POST /api/issues/{issueId}/comments`, `DELETE /api/comments/{id}` |
| Sample data | `POST /api/demo/seed` |

---

## Design Decisions

### Layered architecture instead of CQRS or MediatR
For an application of this size, controllers, services and a DbContext keep the code easy to follow and debug. Service interfaces keep business logic testable without adding handlers and pipelines.

### Stateless JWT authentication
The API keeps no session state, so any instance can validate any request. User identity is read only from verified token claims, never from request bodies.

### Per-project issue numbers
Issues are shown as `KEY-n`. When an issue is created, the service opens a `Serializable` transaction, reads the highest number for that project, and uses the next one. A unique index on `(ProjectId, Number)` guarantees no duplicates even if two requests race.

### Status, completion time and re-opening
Moving an issue into `Done` sets `CompletedAt`; moving it out of `Done` clears it. This keeps the burndown correct when work is re-opened.

### Burndown calculation
`GET /api/sprints/{id}/burndown` returns one point per day from the sprint start to its end:
- **ideal**: a straight line from the total issue count down to zero.
- **remaining**: total sprint issues minus those that are `Done` with a `CompletedAt` on or before that day.

### Sprint rules
- Only one sprint can be active per project at a time.
- Only planned sprints can be deleted; their issues return to the backlog.
- Completed sprints cannot be edited or receive new issues.
- On completion, unfinished issues go back to the backlog or move to a chosen target sprint.

### Optimistic board updates
When a card is dragged, TanStack Query updates the cached board immediately and sends `PATCH /api/issues/{id}/move` with the new status and order. If the request fails, the previous board state is restored and an error toast is shown.

---

## CI/CD and Deployment

### GitHub Actions

- **`ci.yml`** runs on pushes and pull requests. It restores, builds and tests the backend with coverage, then installs, tests and builds the frontend.
- **`deploy.yml`** runs on pushes to `main`. It runs the tests, applies EF Core migrations to Azure SQL, publishes the API to Azure App Service, and deploys the frontend to Azure Static Web Apps.

### Azure resources

- App Service (Linux, .NET 8) for the API
- Azure SQL Database
- Static Web Apps for the frontend (`frontend/staticwebapp.config.json` provides the single-page-app routing fallback)
- Application Insights (optional)

### Required GitHub secrets

| Secret | Used for |
|---|---|
| `AZURE_SQL_CONNECTION_STRING` | Applying migrations in the deploy workflow |
| `AZURE_WEBAPP_NAME` | Target App Service name |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | App Service deployment credentials |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Static Web Apps deployment token |

Also configure these on the App Service: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, and `Cors__AllowedOrigins__0` set to the frontend URL. Set `VITE_API_URL` to the API URL when building the frontend.

---

## License

Released under the MIT License.
