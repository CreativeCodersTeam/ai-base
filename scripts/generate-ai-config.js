#!/usr/bin/env node

import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Parse command line arguments
function parseArgs() {
  const args = process.argv.slice(2);
  const config = {
    languages: [],
    aiSystems: [],
    outputDir: process.cwd(),
    clearAiConfigs: false,
    createAgentsMd: false,
  };

  for (const arg of args) {
    if (arg.startsWith('--languages=')) {
      config.languages = arg.substring('--languages='.length).split(',').map(l => l.trim().toLowerCase());
    } else if (arg.startsWith('--ai-systems=')) {
      config.aiSystems = arg.substring('--ai-systems='.length).split(',').map(s => s.trim().toLowerCase());
    } else if (arg.startsWith('--output-dir=')) {
      config.outputDir = arg.substring('--output-dir='.length);
    } else if (arg === '--clear') {
      config.clearAiConfigs = true;
    }
  }

  return config;
}

function getAiConfigPaths(outputDir) {
  return {
    copilot: {
      root: path.join(outputDir, '.github'),
      copilotFile: path.join(outputDir, '.github', 'copilot-instructions.md'),
      skills: path.join(outputDir, '.github', 'skills'),
      agents: path.join(outputDir, '.github', 'agents'),
      instructions: path.join(outputDir, '.github', 'instructions')
    },
    junie: {
      root: path.join(outputDir, '.junie'),
      guidelinesFile: path.join(outputDir, '.junie', 'guidelines.md'),
      agentsFile: path.join(outputDir, 'AGENTS.md')
    },
    claude: {
      root: path.join(outputDir, '.claude'),
      claudeFile: path.join(outputDir, 'CLAUDE.md'),
      skills: path.join(outputDir, '.claude', 'skills'),
      agents: path.join(outputDir, '.claude', 'agents')
    }
  };
}

// Read all Markdown files from a directory
async function readMarkdownFiles(dir) {
  const files = [];
  try {
    const entries = await fs.readdir(dir, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        const subFiles = await readMarkdownFiles(fullPath);
        files.push(...subFiles);
      } else if (entry.name.endsWith('.md')) {
        const content = await fs.readFile(fullPath, 'utf-8');
        files.push({ path: fullPath, content, name: entry.name });
      }
    }
  } catch (error) {
    console.error('Error reading markdown files:', error);
  }

  return files;
}

// Combine instructions for AI systems that don't support language-specific files
async function combineInstructions(languages, baseDir) {
  const sections = [];

  // Add general instructions first
  const generalDir = path.join(baseDir, 'src/instructions/general');
  const generalFiles = await readMarkdownFiles(generalDir);

  if (generalFiles.length > 0) {
    sections.push('# General Instructions\n');
    for (const file of generalFiles) {
      sections.push(file.content, '\n---\n');
    }
  }

  // Add language-specific instructions
  for (const lang of languages) {
    const langDir = path.join(baseDir, 'src/instructions', lang);
    const langFiles = await readMarkdownFiles(langDir);

    if (langFiles.length > 0) {
      sections.push(`# ${lang.toUpperCase()} Specific Instructions\n`, `**Scope: ${lang} projects only**\n\n`);

      for (const file of langFiles) {
        sections.push(file.content, '\n---\n');
      }
    }
  }

  return sections.join('\n');
}

// Generate GitHub Copilot configuration
async function generateCopilotConfig(languages, baseDir, outputDir, copilotPaths) {
  console.log('Generating GitHub Copilot configuration...');

  const githubDir = path.join(outputDir, '.github');
  await fs.mkdir(githubDir, { recursive: true });

  await createAllSkills(baseDir, copilotPaths.skills, languages);
  await createAllAgents(baseDir, copilotPaths.agents, languages);

  // Copilot doesn't support language-specific instruction files.
  // So we combine all instructions into one file
  await createAllInstructions(baseDir, copilotPaths.instructions, languages, copilotPaths.copilotFile);

  console.log(`✓ Created ${copilotPaths.copilotFile}`);
  console.log(`✓ Created skills in ${copilotPaths.skills}`);
  console.log(`✓ Created agents in ${copilotPaths.agents}`);
}

// Generate Junie configuration
async function generateJunieConfig(languages, baseDir, outputDir, juniePaths, createAgentsMd) {
  console.log('Generating Junie configuration...');

  await fs.mkdir(juniePaths.root, { recursive: true });

  const agentsContent = await combineInstructions(languages, baseDir);
  const instructionsFile = createAgentsMd ? juniePaths.agentsFile : juniePaths.guidelinesFile;
  await fs.writeFile(instructionsFile, agentsContent);

  console.log(`✓ Created ${instructionsFile}`);
}

// Generate Claude Code configuration
async function generateClaudeConfig(languages, baseDir, outputDir, claudePaths) {
  console.log('Generating Claude Code configuration...');

  await createAllSkills(baseDir, claudePaths.skills, languages);

  await createAllAgents(baseDir, claudePaths.agents, languages);

  // Create CLAUDE.md with instructions
  const combinedInstructions = await combineInstructions(languages, baseDir);
  const claudeFile = path.join(outputDir, 'CLAUDE.md');
  await fs.writeFile(claudeFile, combinedInstructions);

  console.log(`✓ Created ${claudeFile}`);
  console.log(`✓ Created skills in ${claudePaths.skills}`);
  console.log(`✓ Created agents in ${claudePaths.agents}`);
}

async function createAllSkills(baseDir, skillsDir, languages) {
  await fs.mkdir(skillsDir, { recursive: true });

  for (const lang of languages.concat("general")) {
    const langSkillsDir = path.join(baseDir, 'src/skills', lang);
    await copySkills(langSkillsDir, skillsDir);
  }
}

async function createAllAgents(baseDir, agentsDir, languages) {
  await fs.mkdir(agentsDir, { recursive: true });

  for (const lang of languages.concat("general")) {
    const langAgentsDir = path.join(baseDir, 'src/agents', lang);
    await copyAgents(langAgentsDir, agentsDir);
  }
}

async function createAllInstructions(baseDir, instructionsDir, languages, generalInstructionFile) {
  await fs.mkdir(instructionsDir, { recursive: true });

  const generalInstructionsFile = path.join(baseDir, 'src', 'instructions', 'general', 'general.instructions.md');
  await fs.copyFile(generalInstructionsFile, generalInstructionFile);

  for (const lang of languages) {
    const langInstructionsDir = path.join(baseDir, 'src/instructions', lang);
    console.log(`Copying instructions for ${lang}... from ${langInstructionsDir} to ${instructionsDir}`);
    await copyAgents(langInstructionsDir, instructionsDir);
  }
}

// Copy skills maintaining directory structure
async function copySkills(sourceDir, targetDir) {
  try {
    const entries = await fs.readdir(sourceDir, { withFileTypes: true });

    for (const entry of entries) {
      const sourcePath = path.join(sourceDir, entry.name);
      const targetPath = path.join(targetDir, entry.name);

      if (entry.isDirectory()) {
        await fs.mkdir(targetPath, { recursive: true });
        await copyDirectory(sourcePath, targetPath);
      }
    }
  } catch (error) {
    console.error('Error copying skills:', error);
  }
}

// Copy agents maintaining directory structure
async function copyAgents(sourceDir, targetDir) {
  try {
    const entries = await fs.readdir(sourceDir, { withFileTypes: true });

    for (const entry of entries) {
      const sourcePath = path.join(sourceDir, entry.name);
      const targetPath = path.join(targetDir, entry.name);

      if (entry.isDirectory()) {
        await fs.mkdir(targetPath, { recursive: true });
        await copyDirectory(sourcePath, targetPath);
      } else if (entry.name.endsWith('.md')) {
        await fs.copyFile(sourcePath, targetPath);
      }
    }
  } catch (error) {
    console.error('Error copying agents:', error);
  }
}

// Recursively copy directory
async function copyDirectory(source, target) {
  await fs.mkdir(target, { recursive: true });
  const entries = await fs.readdir(source, { withFileTypes: true });

  for (const entry of entries) {
    const sourcePath = path.join(source, entry.name);
    const targetPath = path.join(target, entry.name);

    if (entry.isDirectory()) {
      await copyDirectory(sourcePath, targetPath);
    } else {
      await fs.copyFile(sourcePath, targetPath);
    }
  }
}

async function clearAiConfigs(outputDir, aiSystems, aiConfigPaths) {
  console.log('Clearing existing AI configurations...');

  if (aiSystems.includes('copilot')) {
    console.log('Clearing Copilot configurations...');
    await clearAiConfigFile(aiConfigPaths.copilot.copilotFile);
    await clearAiConfigDirectory(aiConfigPaths.copilot.agents);
    await clearAiConfigDirectory(aiConfigPaths.copilot.skills);
  }

  if (aiSystems.includes('junie')) {
    console.log('Clearing Junie configurations...');
    await clearAiConfigDirectory(aiConfigPaths.junie.root);
    await clearAiConfigFile(aiConfigPaths.junie.agentsFile);
  }

  if (aiSystems.includes('claude')) {
    console.log('Clearing Claude configurations...');
    await clearAiConfigFile(aiConfigPaths.claude.claudeFile);
    await clearAiConfigDirectory(aiConfigPaths.claude.root);
  }
}

async function clearAiConfigDirectory(dir) {
  await fs.rm(dir, { recursive: true, force: true });

  console.log(`✓ Directory cleared: ${dir}`);
}

async function clearAiConfigFile(fileName) {
  await fs.rm(fileName, { force: true });

  console.log(`✓ File cleared: ${fileName}`);
}

// Main function
async function main() {
  const config = parseArgs();

  if (config.languages.length === 0 || config.aiSystems.length === 0) {
    console.error('Usage: node generate-ai-config.js --languages=csharp,typescript --ai-systems=copilot,junie,claude [--output-dir=./output]');
    process.exit(1);
  }

  const aiConfigPaths = getAiConfigPaths(config.outputDir);

  if (config.clearAiConfigs) {
    await clearAiConfigs(config.outputDir, config.aiSystems, aiConfigPaths);
  }

  console.log('AI Configuration Generator');
  console.log('==========================');
  console.log(`Languages: ${config.languages.join(', ')}`);
  console.log(`AI Systems: ${config.aiSystems.join(', ')}`);
  console.log(`Output Directory: ${config.outputDir}\n`);

  const baseDir = path.resolve(__dirname, '..');

  for (const aiSystem of config.aiSystems) {
    switch (aiSystem.toLowerCase()) {
      case 'copilot':
        await generateCopilotConfig(config.languages, baseDir, config.outputDir, aiConfigPaths.copilot);
        break;
      case 'junie':
        await generateJunieConfig(config.languages, baseDir, config.outputDir, aiConfigPaths.junie, config.createAgentsMd);
        break;
      case 'claude':
        await generateClaudeConfig(config.languages, baseDir, config.outputDir, aiConfigPaths.claude);
        break;
      default:
        console.warn(`⚠ Unknown AI system: ${aiSystem}`);
    }
  }

  console.log('\n✓ Configuration generation completed successfully!');
}

main().catch(error => {
  console.error('Error:', error);
  process.exit(1);
});
