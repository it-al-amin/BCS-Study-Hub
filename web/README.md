# BCS Study Hub frontend

The React/TypeScript application uses Vite, Tailwind CSS, React Router, and reusable UI components. It currently provides the responsive study shell and the initial dashboard, syllabus, study-stage, resource, planner, progress, and admin routes.

The syllabus explorer, syllabus administration, authentication, and topic-progress screens are connected to the typed `/api/v1` client. Vite proxies `/api` requests to the ASP.NET Core development server at `http://localhost:5217`. The access token stays in browser memory and is cleared on expiry/reload. Other modules may still show isolated preview data; actions that would imply persistence remain disabled.

## Development

From this directory:

```powershell
npm install
npm run dev
npm run build
npm run lint
```

Routes are declared in `src/app/App.tsx`, shared layouts and navigation in `src/components/layout/`, API services in `src/services/`, and reusable interface elements in `src/components/ui/`.
