# AI Base Configuration Repository

Central repository for managing AI Instructions, Skills, and Agents that can be used by various AI systems.

## 🎯 Overview

This repository serves as a central source for:
- **Instructions**: General and language-specific coding guidelines
- **Skills**: Reusable capabilities for AI agents
- **Agents**: Specialized agent definitions for different roles

## 🏗️ Structure

```
ai-base/
├── src/
│   ├── instructions/         # Coding guidelines (*.instructions.md)
│   │   ├── general/          # General instructions
│   │   ├── csharp/           # C#-specific instructions
│   │   ├── typescript/       # TypeScript-specific instructions
│   │   ├── angular/          # Angular-specific instructions
│   │   ├── java/             # Java-specific instructions
│   │   └── docker/           # Docker-specific instructions
│   ├── skills/               # Reusable skills
│   │   ├── general/          # General skills (e.g., code-review)
│   │   ├── csharp/           # C#-specific skills (e.g., ef-core)
│   │   ├── typescript/       # TypeScript-specific skills (e.g., rxjs)
│   │   ├── angular/          # Angular-specific skills
│   │   └── java/             # Java-specific skills
│   └── agents/               # Agent definitions
│       ├── general/          # General agents (e.g., backend-developer)
│       ├── csharp/           # C#-specific agents
│       ├── typescript/       # TypeScript-specific agents
│       ├── angular/          # Angular-specific agents
│       └── java/             # Java-specific agents
├── tools/
│   ├── DeployAi/             # .NET deployment tool
│   └── deploy-ai-config.sh   # Deployment script (calls DeployAi)
└── .github/
    └── workflows/
        └── sync-ai-config.yml # Reusable GitHub Action
```

## 🚀 Usage

### Using in Other Repositories

Create a workflow file in your repository (e.g., `.github/workflows/sync-ai-config.yml`):

```yaml
name: Sync AI Configuration

on:
  workflow_dispatch:
    inputs:
      create-pull-request:
        description: 'Create a pull request instead of committing directly to the branch'
        required: false
        type: boolean
        default: true
      ai-base-version:
        description: 'Version/branch of ai-base to use'
        required: false
        type: string
        default: 'main'
  schedule:
    - cron: '0 0 * * *' # Every day at midnight

jobs:
  sync-scheduled:
    if: github.event_name == 'schedule'
    uses: CreativeCodersTeam/ai-base/.github/workflows/sync-ai-config.yml@main
    with:
      languages: 'csharp,typescript'
      ai-systems: 'copilot,claude,junie'
      ai-base-version: 'main'
      create-pull-request: true

  sync-manual:
    if: github.event_name == 'workflow_dispatch'
    uses: CreativeCodersTeam/ai-base/.github/workflows/sync-ai-config.yml@main
    with:
      languages: 'csharp,typescript'
      ai-systems: 'copilot,claude,junie'
      ai-base-version: ${{ inputs.ai-base-version }}
      create-pull-request: ${{ inputs.create-pull-request }}
```

### Local Testing

```bash
# Deploy AI configuration using the DeployAi .NET program
sh ./tools/deploy-ai-config.sh \
  --languages=csharp,typescript \
  --ai-systems=copilot,junie,claude \
  --output-dir=./output
```

The `deploy-ai-config.sh` script invokes the `DeployAi` .NET program which generates and deploys the AI configuration files.

## 🤖 Supported AI Systems

### GitHub Copilot
- **Output files**: `.github/copilot-instructions.md`
- **Features**: Combined instructions (as language-specific files are not supported)

### Junie (JetBrains)
- **Output files**:
  - `AGENTS.md` (root)
  - `.junie/guidelines.md`
- **Features**: Combined instructions

### Claude Code
- **Output files**:
  - `CLAUDE.md` (Instructions)
  - `.claude/skills/` (Skills with SKILL.md files)
- **Features**:
  - Language-specific skills
  - Combined instructions

## 📝 Available Languages

- `csharp` - C# / .NET
- `typescript` - TypeScript
- `angular` - Angular Framework
- `java` - Java
- `docker` - Docker

## ⚙️ Parameters

### GitHub Action Inputs

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| `languages` | Comma-separated list of languages | Yes | - |
| `ai-systems` | Comma-separated list of AI systems | Yes | - |
| `ai-base-version` | Version/branch of ai-base | No | `main` |

### Script Parameters

```bash
--languages=<lang1,lang2>    # Languages (comma-separated)
--ai-systems=<sys1,sys2>     # AI systems (comma-separated)
--output-dir=<path>          # Output directory (default: current directory)
```

## 📦 Adding New Content

### Adding a New Instruction

1. Create a file matching the pattern `*.instructions.md` in `src/instructions/<language>/`
   - Example: `coding-standards.instructions.md`, `best-practices.instructions.md`
2. Follow the format of existing instructions
3. For language-specific instructions: Add `**Scope: <Language> Projects**`
4. All instruction files must follow the `*.instructions.md` naming pattern to be recognized by the deployment system

### Adding a New Skill

1. Create a directory in `src/skills/<language>/<skill-name>/`
2. Add a `SKILL.md` file with:
   ```markdown
   ---
   name: skill-name
   description: Description of when the skill should be used
   ---

   # Skill content
   ```

### Adding a New Agent

1. Create a `.md` file in `src/agents/<language>/`
2. Define role, responsibilities, skills, and guidelines

## 🔄 How It Works

1. **Source files**: Instructions, Skills, and Agents are defined once in `src/`
2. **Generator script**: `generate-ai-config.js` reads the source files
3. **Transformation**:
   - For AI systems without language-specific support, files are combined
   - Headers are added with language scope
   - Files are converted to AI system-specific formats
4. **Deployment**: GitHub Action copies generated files to target repository

## 🎨 Combination Logic

For AI systems that don't support language-specific instructions:

1. **General Instructions** first
   ```markdown
   # General Instructions
   <Content>
   ```

2. **Language-specific Instructions** after (for each language)
   ```markdown
   # <LANGUAGE> Specific Instructions
   **Scope: <language> projects only**
   <Content>
   ```

## 📄 License

MIT

## 🤝 Contributing

Contributions are welcome! Please create a pull request with your changes.
