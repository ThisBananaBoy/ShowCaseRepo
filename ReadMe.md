# ProjectME



Projektplanungssoftware für Kreative und Einzelpersonen. Eine moderne Anwendung zur Verwaltung von Projekten, Tasks, Terminen und Zeiterfassung – entwickelt für den individuellen Workflow ohne Team-Features.

## Features

- **Projektverwaltung** – Projekte mit Status-Tracking (Aktiv, Pausiert, Abgeschlossen, Archiviert)
- **Task-Management** – Aufgaben mit Prioritäten, Fälligkeitsdaten und Projekt-/Milestone-Zuordnung
- **Milestones & Deadlines** – Meilensteine und Fristen mit automatischer Deadline-Verwaltung
- **Wiederkehrende Tasks** – Automatisierte Aufgaben mit Recurring-Patterns
- **Terminplanung** – Appointments mit Zeitplanung
- **Zeiterfassung** – Time Entries für Projekt-Tracking
- **Visualisierungen** – Gantt-Charts und Aktivitäts-Heatmaps
- **Pomodoro & Stopwatch** – Integrierte Arbeits-Tools

## Architektur

Die Anwendung folgt einer Microservice-Architektur mit drei separaten Services:

### Auth_Server
Microservice für Authentifizierung und Benutzerverwaltung

**Technologien:**
- .NET 9.0
- FastEndpoints (Minimal API Framework)
- ASP.NET Core Identity
- Entity Framework Core 9.0
- PostgreSQL (Npgsql)
- JWT (System.IdentityModel.Tokens.Jwt)

**Funktionen:**
- Benutzer-Registrierung und -Login
- JWT Token-Generierung
- Rollenbasierte Autorisierung

### ProjectME_Backend
Haupt-API mit Clean Architecture

**Technologien:**
- .NET 9.0
- FastEndpoints (Minimal API Framework)
- Clean Architecture (Domain, Application, Infrastructure)
- Entity Framework Core 9.0
- PostgreSQL (Npgsql)
- JWT Bearer Authentication
- HttpClient mit Resilience (HTTP Client Factory)

**Architektur-Schichten:**
- **Domain** – Domain-Modelle und Business-Logik (Aggregates, Entities)
- **Application** – Application Services, DTOs, User Context
- **Infrastructure** – EF Core, Repositories, HTTP Clients, Migrations

**Endpoints:**
- Projects (CRUD)
- Tasks (CRUD)
- RecurringTasks (CRUD)
- Appointments (CRUD)
- TimeEntries (CRUD)
- Activities (CRUD)

### ProjectME_Frontend
Moderne Single-Page-Application

**Technologien:**
- Nuxt 4 (Vue 3 Framework, SSR-ready)
- Vue 3 (Composition API)
- TypeScript
- Pinia (State Management)
- Nuxt UI 4 (Tailwind CSS basiertes Component Framework)
- VueUse (Composition Utilities)

**Features:**
- Server-Side Rendering (SSR)
- Responsive Design
- Dark Mode Support
- Drag & Drop Funktionalität
- Real-time Updates
 

## 📁 Projektstruktur

```
ProjectME_ShowCase/
├── Auth_Server/              # Authentifizierungs-Service
│   └── Auth_Server/
│       ├── Endpoints/         # FastEndpoints
│       ├── Model/            # Domain Models
│       ├── Services/          # JWT, Seeding
│       └── Extensions/        # DI, Migrations
│
├── ProjectME_Backend/         # Haupt-API
│   ├── ProjectME_BE/         # API Layer (Endpoints)
│   ├── ProjectME_BE.Domain/  # Domain Layer
│   ├── ProjectME_BE.Application/  # Application Layer
│   └── ProjectME_BE.Infrastructure/  # Infrastructure Layer
│
└── ProjectME_Frontend/        # Web-Application
    └── nuxt-app/
        ├── app/
        │   ├── pages/        # Vue Pages (Routing)
        │   ├── components/   # Vue Components
        │   ├── stores/       # Pinia Stores
        │   └── composables/  # Vue Composables
        └── nuxt.config.ts
```

## Deployment & Entwicklung

### Containerisierung mit Docker

Die Anwendung ist für Container-basiertes Deployment konzipiert. Jeder Service wird als separater Docker-Container bereitgestellt:

- **Auth_Server** – Eigenständiger Container für Authentifizierung
- **ProjectME_Backend** – Container für die Haupt-API
- **ProjectME_Frontend** – Container für die Nuxt-Anwendung (geplant)

Dockerfiles und docker-compose Konfigurationen sind für Backend und Auth_Server vorhanden, um eine einfache Orchestrierung und Skalierung zu ermöglichen.

### KI-unterstützte Entwicklung mit Cursor

Die Entwicklung erfolgt mit **Cursor**, einem KI-gestützten Code-Editor. Cursor unterstützt die Entwicklung durch:

- Code-Generierung und -Vervollständigung
- Kontextbewusste Vorschläge basierend auf dem gesamten Codebase
- Automatische Refactorings und Code-Optimierungen
- Schnellere Implementierung komplexer Features

Diese Kombination aus modernen Entwicklungswerkzeugen und Container-Technologie ermöglicht eine effiziente Entwicklung und einfaches Deployment.

## Hinweis

Dieses Repository dient **ausschließlich als ShowCase** und präsentiert den aktuellen Entwicklungsstand. Die Anwendung befindet sich in aktiver Entwicklung.
