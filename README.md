# 🎓 SGS-Togo — Système de Gestion Scolaire du Togo

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Based on](https://img.shields.io/badge/Based%20on-Fullstack%20Hero-blueviolet)](https://fullstackhero.net)

> **Une plateforme numérique moderne, multi-tenant et open source pour la gestion scolaire au Togo 🇹🇬**

---

## 🌍 Contexte

Le système éducatif togolais fait face à des défis majeurs : gestion manuelle des inscriptions, suivi papier des notes et absences, manque de transparence dans les paiements scolaires, et absence d'outils numériques adaptés au contexte local.

**SGS-Togo** est une solution informatique conçue pour répondre à ces problématiques en offrant une plateforme centralisée, accessible et adaptée aux réalités togolaises (support Mobile Money, fonctionnement en zones à faible connectivité, interface multilingue).

---

## 🎯 Objectifs

- **Digitaliser** la gestion des écoles togolaises (publiques et privées)
- **Centraliser** les données scolaires (élèves, enseignants, notes, absences, paiements)
- **Automatiser** la génération des bulletins, rapports et statistiques
- **Faciliter** la communication entre écoles, enseignants et parents
- **Intégrer** les solutions de paiement locales (T-Money, Flooz)
- **Offrir** une architecture multi-tenant (1 école = 1 tenant)

---

## 👥 Utilisateurs cibles

| Rôle | Fonctionnalités |
|------|----------------|
| 🏫 **Administrateur** | Gestion globale, configuration système, statistiques nationales |
| 👨‍🏫 **Directeur d'école** | Supervision de l'école, validation, rapports |
| 👩‍🏫 **Enseignant** | Saisie des notes, gestion des absences, emploi du temps |
| 👨‍👩‍👧 **Parent** | Suivi des résultats, paiements, notifications SMS |
| 🎓 **Élève** | Consultation des notes, emploi du temps, bulletins |

---

## 📦 Modules

### Modules hérités de Fullstack Hero
- 🔐 **Identity** — Authentification, autorisation, gestion des rôles (JWT)
- 🏢 **Multitenancy** — Isolation des données par école (Finbuckle)
- 📋 **Auditing** — Traçabilité complète des actions

### Modules métier SGS-Togo
- 🏫 **SchoolManagement** — Écoles, classes, matières, années scolaires
- 🎓 **StudentManagement** — Élèves, inscriptions, parents, transferts
- 👩‍🏫 **TeacherManagement** — Enseignants, affectations, qualifications
- 📊 **GradeManagement** — Notes, moyennes, bulletins PDF, classements
- 📅 **AttendanceManagement** — Présences, absences, notifications parents
- 💰 **PaymentManagement** — Frais de scolarité, Mobile Money (T-Money/Flooz), reçus
- 📆 **ScheduleManagement** — Emplois du temps, créneaux horaires

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      API Gateway                         │
│                   ASP.NET Core Web API                   │
├─────────────────────────────────────────────────────────┤
│                    Modules métier                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │  School   │ │ Student  │ │ Teacher  │ │  Grade   │  │
│  │Management│ │Management│ │Management│ │Management│  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐               │
│  │Attendance│ │ Payment  │ │ Schedule │               │
│  │Management│ │Management│ │Management│               │
│  └──────────┘ └──────────┘ └──────────┘               │
├─────────────────────────────────────────────────────────┤
│                  Building Blocks (FSH)                   │
│  Core │ Persistence │ Web │ Jobs │ Eventing │ Shared    │
├─────────────────────────────────────────────────────────┤
│               Infrastructure                             │
│  PostgreSQL │ Redis │ Hangfire │ Serilog │ Docker        │
└─────────────────────────────────────────────────────────┘
```

---

## 🛠️ Stack technique

| Composant | Technologie |
|-----------|------------|
| Framework | .NET 10 / ASP.NET Core |
| Architecture | Clean Architecture / Modular Monolith |
| Base de données | PostgreSQL |
| ORM | Entity Framework Core |
| Authentification | JWT + ASP.NET Identity |
| Multi-tenancy | Finbuckle |
| Background Jobs | Hangfire |
| Génération PDF | QuestPDF |
| Notifications SMS | Twilio |
| Paiement Mobile | T-Money / Flooz API |
| Logging | Serilog + OpenTelemetry |
| Cache | Redis |
| Conteneurisation | Docker + Docker Compose |
| CI/CD | GitHub Actions |
| Documentation API | Swagger / OpenAPI |

---

## 📊 Niveaux scolaires supportés

Le système supporte le parcours scolaire togolais complet :

| Cycle | Niveaux |
|-------|---------|
| 🟢 Primaire | CP1, CP2, CE1, CE2, CM1, CM2 |
| 🔵 Collège | 6ème, 5ème, 4ème, 3ème |
| 🟣 Lycée | 2nde, 1ère, Terminale |

---

## 🚀 Démarrage rapide

### Prérequis
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 16+](https://www.postgresql.org/) (ou via Docker)

### Installation

```bash
# 1. Cloner le projet
git clone https://github.com/jeandocker19/sgs-togo.git
cd sgs-togo

# 2. Démarrer les services (PostgreSQL, Redis) via Docker
docker-compose up -d

# 3. Restaurer les dépendances
dotnet restore src/FSH.Framework.slnx

# 4. Lancer l'application via Aspire
dotnet run --project src/Playground/FSH.Playground.AppHost

# 5. Ou lancer l'API seule
dotnet run --project src/Playground/Playground.Api
```

### Accès
- 🌐 **API Swagger** : `https://localhost:5285`
- 📖 **Documentation API** : endpoints sous `/api/v1/...`

---

## 📂 Structure du projet

```
sgs-togo/
├── src/
│   ├── BuildingBlocks/        ← 🧱 Fondations (Core, Persistence, Web, Jobs, Cache...)
│   ├── Modules/
│   │   ├── Auditing/          ← 📋 Audit (hérité FSH)
│   │   ├── Identity/          ← 🔐 Identité (hérité FSH)
│   │   ├── Multitenancy/      ← 🏢 Multi-tenant (hérité FSH)
│   │   ├── SchoolManagement/  ← 🏫 Gestion des écoles [NOUVEAU]
│   │   ├── StudentManagement/ ← 🎓 Gestion des élèves [NOUVEAU]
│   │   ├── TeacherManagement/ ← 👩‍🏫 Gestion des enseignants [NOUVEAU]
│   │   ├── GradeManagement/   ← 📊 Notes et bulletins [NOUVEAU]
│   │   ├── AttendanceManagement/ ← 📅 Absences/Présences [NOUVEAU]
│   │   ├── PaymentManagement/ ← 💰 Paiements Mobile Money [NOUVEAU]
│   │   └── ScheduleManagement/← 📆 Emplois du temps [NOUVEAU]
│   ├── Playground/            ← 🎮 API de référence + Blazor UI
│   ├── Tests/                 ← 🧪 Tests unitaires et d'architecture
│   └── Tools/                 ← 🛠️ Outils utilitaires
├── terraform/                 ← ☁️ Infrastructure as Code
├── docker-compose.yml         ← 🐳 Conteneurisation
└── README.md                  ← 📖 Ce fichier
```

---

## 🤝 Contribution

Les contributions sont les bienvenues ! Consultez les [issues](https://github.com/jeandocker19/sgs-togo/issues) pour voir les tâches disponibles.

1. Forkez le projet
2. Créez votre branche (`git checkout -b feature/mon-module`)
3. Committez vos changements (`git commit -m 'feat: ajout module X'`)
4. Poussez sur la branche (`git push origin feature/mon-module`)
5. Ouvrez une Pull Request

---

## 📄 Licence

Ce projet est sous licence [MIT](LICENSE) — basé sur [Fullstack Hero .NET Starter Kit](https://github.com/fullstackhero/dotnet-starter-kit).

---

## 🙏 Remerciements

- [Fullstack Hero](https://fullstackhero.net) pour le starter kit .NET
- La communauté open source togolaise 🇹🇬
- Tous les contributeurs du projet

---

<p align="center">
  Fait avec ❤️ pour l'éducation au Togo 🇹🇬
</p>
