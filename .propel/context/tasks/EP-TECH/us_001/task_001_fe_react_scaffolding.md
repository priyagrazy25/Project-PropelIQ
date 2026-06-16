---
post_title: "TASK_001 - React TypeScript SPA Scaffolding"
author1: "AI Senior Developer"
post_slug: "task-001-fe-react-scaffolding"
microsoft_alias: "N/A"
featured_image: "N/A"
categories: "Healthcare, Development Tasks"
tags: "EP-TECH, US_001, frontend, React, TypeScript, Vercel, Redux Toolkit"
ai_note: "Generated with AI assistance from user story US_001"
summary: "Scaffold the React 18.x TypeScript SPA with Redux Toolkit, Vercel deployment, feature-based folder structure mirroring backend modules."
post_date: "2026-04-16"
---

# Task - TASK_001_FE_REACT_SCAFFOLDING

## Requirement Reference
- User Story: us_001
- Story Location: .propel/context/tasks/EP-TECH/us_001/us_001.md
- Acceptance Criteria:
    - AC-1: React 18.x TypeScript project with strict config and ESLint/Prettier
    - AC-2: Feature-based folder structure mirroring backend modules (Scheduling, Clinical, Identity, Notification)
    - AC-3: Vercel auto-deploy with HTTPS and CDN on push to main
    - AC-4: Dev server with HMR on localhost:3000
    - AC-5: Redux Toolkit centralized store with typed slices
- Edge Cases:
    - Vercel free tier bandwidth exceeded → maintenance-mode static page
    - Inconsistent Node.js versions → enforce via .nvmrc and engines field

## Design References (Frontend Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **UI Impact** | No |
| **Figma URL** | N/A |
| **Wireframe Status** | N/A |
| **Wireframe Type** | N/A |
| **Wireframe Path/URL** | N/A |
| **Screen Spec** | N/A |
| **UXR Requirements** | N/A |
| **Design Tokens** | N/A |

## Applicable Technology Stack
| Layer | Technology | Version |
|-------|------------|---------|
| Frontend | React with TypeScript | 18.x |
| State Management | Redux Toolkit | 2.x |
| Hosting | Vercel (free tier) | N/A |
| Real-Time Client | @microsoft/signalr | 8.x |
| Linting | ESLint + Prettier | Latest |

## AI References (AI Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **AI Impact** | No |
| **AIR Requirements** | N/A |
| **AI Pattern** | N/A |
| **Prompt Template Path** | N/A |
| **Guardrails Config** | N/A |
| **Model Provider** | N/A |

## Mobile References (Mobile Tasks Only)
| Reference Type | Value |
|----------------|-------|
| **Mobile Impact** | No |
| **Platform Target** | N/A |
| **Min OS Version** | N/A |
| **Mobile Framework** | N/A |

## Task Overview
Bootstrap the React 18.x TypeScript single-page application with a production-ready project structure, Redux Toolkit state management, and Vercel deployment pipeline. The folder structure mirrors the backend's modular monolith boundaries (Scheduling, Clinical, Identity, Notification) to maintain consistent domain separation across the full stack.

## Dependent Tasks
- None (first task in the project)

## Impacted Components
- NEW: React SPA project root
- NEW: Redux store with typed slices (auth, scheduling, clinical, notification)
- NEW: Vercel deployment configuration
- NEW: ESLint + Prettier configuration

## Implementation Plan
1. Initialize React project using Vite with TypeScript template (`npm create vite@latest`)
2. Configure strict TypeScript settings in tsconfig.json (strict: true, noImplicitAny, strictNullChecks)
3. Set up ESLint with @typescript-eslint plugin and Prettier integration
4. Create feature-based folder structure: `src/features/{scheduling,clinical,identity,notification}/`
5. Install and configure Redux Toolkit with createSlice for each feature domain
6. Configure Vercel project with vercel.json (rewrites for SPA routing)
7. Add .nvmrc file pinning Node.js LTS version and engines field in package.json
8. Verify HMR works on localhost:3000 and Vercel deployment succeeds

## Current Project State
```
frontend/
  .nvmrc
  .prettierrc
  eslint.config.js
  index.html
  package.json
  tsconfig.json
  tsconfig.app.json
  tsconfig.node.json
  vercel.json
  vite.config.ts
  src/
    App.tsx
    main.tsx
    app/
      hooks.ts
      store.ts
    features/
      clinical/clinicalSlice.ts
      identity/identitySlice.ts
      notification/notificationSlice.ts
      scheduling/schedulingSlice.ts
```

## Expected Changes
| Action | File Path | Description |
|--------|-----------|-------------|
| CREATE | frontend/package.json | Project manifest with React 18.x, Redux Toolkit, TypeScript dependencies |
| CREATE | frontend/tsconfig.json | Strict TypeScript configuration |
| CREATE | frontend/.eslintrc.cjs | ESLint configuration with TypeScript and Prettier plugins |
| CREATE | frontend/.prettierrc | Prettier formatting rules |
| CREATE | frontend/.nvmrc | Node.js LTS version pin |
| CREATE | frontend/vercel.json | Vercel deployment configuration with SPA rewrites |
| CREATE | frontend/src/app/store.ts | Redux Toolkit store configuration with typed hooks |
| CREATE | frontend/src/features/identity/identitySlice.ts | Identity/auth state slice |
| CREATE | frontend/src/features/scheduling/schedulingSlice.ts | Scheduling state slice |
| CREATE | frontend/src/features/clinical/clinicalSlice.ts | Clinical state slice |
| CREATE | frontend/src/features/notification/notificationSlice.ts | Notification state slice |
| CREATE | frontend/src/App.tsx | Root application component with router shell |
| CREATE | frontend/src/main.tsx | Application entry point with Provider wrappers |

## External References
- React 18 Documentation: https://react.dev/
- Redux Toolkit Quick Start: https://redux-toolkit.js.org/tutorials/quick-start
- Vite Guide: https://vitejs.dev/guide/
- Vercel Deployment Docs: https://vercel.com/docs/deployments/overview

## Build Commands
- `npm install` — Install dependencies
- `npm run dev` — Start development server on localhost:3000
- `npm run build` — Production build
- `npm run lint` — Run ESLint checks

## Implementation Validation Strategy
- [x] `npm run build` completes without errors
- [x] `npm run lint` passes with zero warnings
- [ ] Dev server starts on localhost:3000 with HMR active
- [ ] Redux DevTools shows all 4 feature slices initialized
- [ ] Vercel deployment succeeds with HTTPS enabled

## Implementation Checklist
- [x] Initialize Vite + React 18.x + TypeScript project
- [x] Configure strict TypeScript compiler options
- [x] Set up ESLint with @typescript-eslint and Prettier
- [x] Create feature-based folder structure (scheduling, clinical, identity, notification)
- [x] Install Redux Toolkit and configure centralized store with typed hooks
- [x] Create initial state slices for each feature domain
- [x] Configure Vercel deployment with SPA routing rewrites
- [x] Add .nvmrc and package.json engines field for Node.js version enforcement
