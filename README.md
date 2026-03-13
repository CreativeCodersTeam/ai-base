# AI Base Configuration Repository

Zentrales Repository für die Verwaltung von AI Instructions, Skills und Agents, das von verschiedenen AI-Systemen genutzt werden kann.

## 🎯 Überblick

Dieses Repository dient als zentrale Quelle für:
- **Instructions**: Allgemeine und sprachenspezifische Coding-Richtlinien
- **Skills**: Wiederverwendbare Fähigkeiten für AI-Agents
- **Agents**: Spezialisierte Agent-Definitionen für verschiedene Rollen

## 🏗️ Struktur

```
ai-base/
├── src/
│   ├── instructions/          # Coding-Richtlinien
│   │   ├── general/          # Allgemeine Instructions
│   │   ├── csharp/           # C#-spezifische Instructions
│   │   ├── typescript/       # TypeScript-spezifische Instructions
│   │   ├── angular/          # Angular-spezifische Instructions
│   │   └── java/             # Java-spezifische Instructions
│   ├── skills/               # Wiederverwendbare Skills
│   │   ├── general/          # Allgemeine Skills (z.B. code-review)
│   │   ├── csharp/           # C#-spezifische Skills (z.B. ef-core)
│   │   ├── typescript/       # TypeScript-spezifische Skills (z.B. rxjs)
│   │   ├── angular/          # Angular-spezifische Skills
│   │   └── java/             # Java-spezifische Skills
│   └── agents/               # Agent-Definitionen
│       ├── general/          # Allgemeine Agents (z.B. backend-developer)
│       ├── csharp/           # C#-spezifische Agents
│       ├── typescript/       # TypeScript-spezifische Agents
│       ├── angular/          # Angular-spezifische Agents
│       └── java/             # Java-spezifische Agents
├── scripts/
│   └── generate-ai-config.js # Generator-Script
└── .github/
    └── workflows/
        └── sync-ai-config.yml # Reusable GitHub Action
```

## 🚀 Verwendung

### In anderen Repositories verwenden

Erstelle eine Workflow-Datei in deinem Repository (z.B. `.github/workflows/sync-ai-config.yml`):

```yaml
name: Sync AI Configuration

on:
  workflow_dispatch:
  schedule:
    - cron: '0 0 * * 0'  # Wöchentlich sonntags

jobs:
  sync:
    uses: <DEIN-GITHUB-USERNAME>/ai-base/.github/workflows/sync-ai-config.yml@main
    with:
      languages: 'csharp,typescript'
      ai-systems: 'copilot,claude'
      ai-base-version: 'main'
```

### Lokal testen

```bash
# Installation
npm install

# Generiere Konfiguration
node scripts/generate-ai-config.js \
  --languages=csharp,typescript \
  --ai-systems=copilot,junie,claude \
  --output-dir=./output
```

## 🤖 Unterstützte AI-Systeme

### GitHub Copilot
- **Ausgabe-Dateien**: `.github/copilot-instructions.md`
- **Features**: Kombinierte Instructions (da keine sprachenspezifischen Files unterstützt werden)

### Junie (JetBrains)
- **Ausgabe-Dateien**:
  - `AGENTS.md` (root)
  - `.junie/guidelines.md`
- **Features**: Kombinierte Instructions

### Claude Code
- **Ausgabe-Dateien**:
  - `CLAUDE.md` (Instructions)
  - `.claude/skills/` (Skills mit SKILL.md Files)
- **Features**:
  - Sprachenspezifische Skills
  - Kombinierte Instructions

## 📝 Verfügbare Sprachen

- `csharp` - C# / .NET
- `typescript` - TypeScript
- `angular` - Angular Framework
- `java` - Java

## ⚙️ Parameter

### GitHub Action Inputs

| Parameter | Beschreibung | Erforderlich | Standard |
|-----------|--------------|--------------|----------|
| `languages` | Komma-getrennte Liste von Sprachen | Ja | - |
| `ai-systems` | Komma-getrennte Liste von AI-Systemen | Ja | - |
| `ai-base-version` | Version/Branch von ai-base | Nein | `main` |

### Script Parameter

```bash
--languages=<lang1,lang2>    # Sprachen (komma-getrennt)
--ai-systems=<sys1,sys2>     # AI-Systeme (komma-getrennt)
--output-dir=<path>          # Ausgabe-Verzeichnis (Standard: aktuelles Verzeichnis)
```

## 📦 Hinzufügen neuer Inhalte

### Neue Instruction hinzufügen

1. Erstelle eine `.md` Datei in `src/instructions/<sprache>/`
2. Folge dem Format der existierenden Instructions
3. Bei sprachenspezifischen Instructions: Füge `**Scope: <Sprache> Projects**` hinzu

### Neuen Skill hinzufügen

1. Erstelle ein Verzeichnis in `src/skills/<sprache>/<skill-name>/`
2. Füge eine `SKILL.md` Datei hinzu mit:
   ```markdown
   ---
   name: skill-name
   description: Beschreibung wann der Skill verwendet wird
   ---

   # Skill-Inhalt
   ```

### Neuen Agent hinzufügen

1. Erstelle eine `.md` Datei in `src/agents/<sprache>/`
2. Definiere Rolle, Verantwortlichkeiten, Skills und Guidelines

## 🔄 Wie es funktioniert

1. **Quell-Files**: Instructions, Skills und Agents werden einmal in `src/` definiert
2. **Generator-Script**: `generate-ai-config.js` liest die Source-Files
3. **Transformation**:
   - Für AI-Systeme ohne sprachenspezifische Unterstützung werden Files kombiniert
   - Header werden hinzugefügt mit Sprach-Scope
   - Files werden in AI-System spezifische Formate konvertiert
4. **Deployment**: GitHub Action kopiert generierte Files ins Ziel-Repository

## 🎨 Kombinations-Logik

Für AI-Systeme die keine sprachenspezifischen Instructions unterstützen:

1. **General Instructions** zuerst
   ```markdown
   # General Instructions
   <Inhalt>
   ```

2. **Sprachenspezifische Instructions** danach (für jede Sprache)
   ```markdown
   # <SPRACHE> Specific Instructions
   **Scope: <sprache> projects only**
   <Inhalt>
   ```

## 📄 Lizenz

MIT

## 🤝 Beitragen

Contributions sind willkommen! Bitte erstelle einen Pull Request mit deinen Änderungen.
