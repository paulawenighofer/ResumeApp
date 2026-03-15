# AI Resume Builder — 5-Week MVP Plan

---

## Sprint 1 — Foundation & Setup

**Goal:** Project skeleton running, auth works end-to-end

### Project Setup
- [ ] Create GitHub repo with folder structure (API, Mobile, Shared Library)
- [ ] Set up Docker + docker-compose for local dev (PostgreSQL container)
- [ ] Define database schema and shared DTOs/models
- [ ] Agree on branching strategy and naming conventions
- [ ] Set up basic GitHub Actions workflow (build only)

### Backend (ASP.NET Core Web API)
- [ ] Scaffold ASP.NET Core Web API project
- [ ] Set up Entity Framework Core with PostgreSQL
- [ ] Create DB migrations (Users, Education, Experience, Skills, Projects, Resumes)
- [ ] Add ASP.NET Core Identity for user management
- [ ] Implement Register endpoint with JWT
- [ ] Implement Login endpoint with JWT

### Mobile App (.NET MAUI — XAML)
- [ ] Scaffold .NET MAUI project with Shell navigation
- [ ] Install CommunityToolkit.Mvvm and set up MVVM pattern
- [ ] Build Login page (XAML + ViewModel)
- [ ] Build Register page (XAML + ViewModel)
- [ ] Set up HttpClient service for API calls
- [ ] Wire up secure token storage (SecureStorage)
- [ ] Define route structure in AppShell.xaml

### Milestone
> Register and login works from the mobile app with JWT authentication.

---

## Sprint 2 — Profile CRUD

**Goal:** Full user profile persists to PostgreSQL + Blob Storage

### Backend
- [ ] CRUD endpoints for Education
- [ ] CRUD endpoints for Experience
- [ ] CRUD endpoints for Skills
- [ ] CRUD endpoints for Projects
- [ ] Azure Blob Storage integration for profile image upload
- [ ] Request validation using shared library attributes

### Mobile App
- [ ] Education form page (XAML + ViewModel with data binding)
- [ ] Experience form page (XAML + ViewModel with data binding)
- [ ] Skills form page (XAML + ViewModel with data binding)
- [ ] Projects form page (XAML + ViewModel with data binding)
- [ ] Profile image picker and upload
- [ ] Use CollectionView for listing entries per section
- [ ] Local draft saving (Preferences / SQLite) for offline safety
- [ ] Connect all forms to backend API

### Milestone
> User can fill in full profile from mobile app; data persists to PostgreSQL and Blob Storage.

---

## Sprint 3 — AI Resume Generation

**Goal:** Core feature works — paste job description, get tailored resume

### AI Prompt Design
- [ ] Design the structured prompt (profile + job → JSON output)
- [ ] Test prompt in API playground (Claude or OpenAI) before integrating
- [ ] Define expected JSON response shape (summary, bulletPoints, highlightedSkills, etc.)

### Backend
- [ ] Build resume generation endpoint
- [ ] Pull user profile data from DB to construct prompt context
- [ ] Integrate Claude API or OpenAI API call
- [ ] Parse structured JSON response from AI
- [ ] Save generated resume to Resumes table in DB
- [ ] Handle errors, timeouts, and retries gracefully

### Mobile App
- [ ] "New Resume" page — form for job title, job description, company info
- [ ] Loading state UI during AI generation (ActivityIndicator + messaging)
- [ ] Resume preview page (render summary, bullets, skills using XAML layouts)
- [ ] Saved resumes list page (CollectionView bound to ViewModel)

### Milestone
> Paste a job description → generate a tailored resume → see the result on the preview screen.

---

## Sprint 4 — PDF Export, Editing & Polish

**Goal:** Full MVP flow — profile → generate → edit → export PDF

### Backend
- [ ] PDF generation using QuestPDF (one clean professional template)
- [ ] Store generated PDF in Azure Blob Storage
- [ ] Return SAS URL (short expiry) for secure PDF access
- [ ] Edit resume endpoint (update summary/bullets, re-export)
- [ ] Delete resume endpoint

### Mobile App
- [ ] "Export as PDF" button — triggers generation, opens/downloads file
- [ ] Edit page for tweaking generated resume text before export
- [ ] Use WebView to render resume preview as HTML (easier than pure XAML layout)
- [ ] Cache recent resumes on-device (SQLite)
- [ ] UI polish pass — consistent styling, error messages, empty states
- [ ] Verify navigation flow is smooth across all screens

### Milestone
> Full MVP flow works: Profile → Generate → Preview → Edit → Export PDF.

---

## Sprint 5 — Deploy, Test & Present

**Goal:** Deployed on Azure, demo-ready

### Deployment
- [ ] Write multi-stage Dockerfile for backend
- [ ] Provision Azure App Service
- [ ] Provision Azure Database for PostgreSQL
- [ ] Set up Azure Blob Storage container (profile-images, resumes)
- [ ] Configure environment variables and connection strings in Azure
- [ ] Set up GitHub Actions CI/CD (build → push Docker image → deploy)
- [ ] Point mobile app to production API URL

### Testing & Finishing
- [ ] End-to-end test: register → profile → generate → edit → export
- [ ] Test on different device sizes / emulators
- [ ] Fix bugs and edge cases
- [ ] Write README (setup instructions, architecture diagram, how to run locally)
- [ ] Prepare demo / presentation

### Milestone
> App is deployed on Azure, fully functional, and demo-ready.

---

## Nice-to-Haves (If Time Permits)
- [ ] Google social login (ASP.NET Core Identity + WebAuthenticator)
- [ ] Azure Cache for Redis (cache profile data on backend)
- [ ] Azure Key Vault for secrets management
- [ ] Multiple PDF templates
- [ ] Resume version history