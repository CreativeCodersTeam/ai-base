#!/usr/bin/env node

import fs from 'fs/promises';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Parse command line arguments
function parseArgs() {
  const args = process.argv.slice(2);
  const config = {
    languages: [],
    aiSystems: [],
    outputDir: process.cwd(),
    clearAiConfigs: false
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
  const aiConfigPaths = {
    copilot: {
      root: path.join(outputDir, '.github'),
      copilotFile: path.join(outputDir, '.github', 'copilot-instructions.md'),
      skills: path.join(outputDir, '.github', 'skills'),
      agents: path.join(outputDir, '.github', 'agents'),
      instructions: path.join(outputDir, '.github', 'instructions')
    },
    junie: {
      root: path.join(outputDir, '.junie'),
      agentsFile: path.join(outputDir, 'AGENTS.md')
    },
    claude: {
      root: path.join(outputDir, '.claude'),
      claudeFile: path.join(outputDir, 'CLAUDE.md'),
      skills: path.join(outputDir, '.claude', 'skills'),
      agents: path.join(outputDir, '.claude', 'agents')
    }
  };

  return aiConfigPaths;
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
    // Directory doesn't exist, return empty array
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
async function generateCopilotConfig(languages, baseDir, outputDir) {
  console.log('Generating GitHub Copilot configuration...');

  const githubDir = path.join(outputDir, '.github');
  await fs.mkdir(githubDir, { recursive: true });

  const skillsDir = path.join(githubDir, 'skills');
  await fs.mkdir(skillsDir, { recursive: true });

  // Copy general skills
  const generalSkillsDir = path.join(baseDir, 'src/skills/general');
  await copySkills(generalSkillsDir, skillsDir);

  // Copy language-specific skills
  for (const lang of languages) {
    const langSkillsDir = path.join(baseDir, 'src/skills', lang);
    await copySkills(langSkillsDir, skillsDir);
  }

  // Copy agents
  const agentsDir = path.join(githubDir, 'agents');
  await fs.mkdir(agentsDir, { recursive: true });

  // Copy general agents
  const generalAgentsDir = path.join(baseDir, 'src/agents/general');
  await copyAgents(generalAgentsDir, agentsDir);

  // Copy language-specific agents
  for (const lang of languages) {
    const langAgentsDir = path.join(baseDir, 'src/agents', lang);
    await copyAgents(langAgentsDir, agentsDir);
  }

  // Copilot doesn't support language-specific instruction files.
  // So we combine all instructions into one file
  const combinedInstructions = await combineInstructions(languages, baseDir);

  const copilotFile = path.join(githubDir, 'copilot-instructions.md');
  await fs.writeFile(copilotFile, combinedInstructions);

  console.log(`✓ Created ${copilotFile}`);
  console.log(`✓ Created skills in ${skillsDir}`);
  console.log(`✓ Created agents in ${agentsDir}`);
}

// Generate Junie configuration
async function generateJunieConfig(languages, baseDir, outputDir) {
  console.log('Generating Junie configuration...');

  // Junie supports AGENTS.md in root
  const agentsContent = await combineInstructions(languages, baseDir);
  const agentsFile = path.join(outputDir, 'AGENTS.md');
  await fs.writeFile(agentsFile, agentsContent);

  console.log(`✓ Created ${agentsFile}`);

  // Also create .junie/guidelines.md
  const junieDir = path.join(outputDir, '.junie');
  await fs.mkdir(junieDir, { recursive: true });

  const guidelinesFile = path.join(junieDir, 'guidelines.md');
  await fs.writeFile(guidelinesFile, agentsContent);

  console.log(`✓ Created ${guidelinesFile}`);
}

// Generate Claude Code configuration
async function generateClaudeConfig(languages, baseDir, outputDir) {
  console.log('Generating Claude Code configuration...');

  const claudeDir = path.join(outputDir, '.claude');
  await fs.mkdir(claudeDir, { recursive: true });

  // Copy skills
  const skillsDir = path.join(claudeDir, 'skills');
  await fs.mkdir(skillsDir, { recursive: true });

  // Copy general skills
  const generalSkillsDir = path.join(baseDir, 'src/skills/general');
  await copySkills(generalSkillsDir, skillsDir);

  // Copy language-specific skills
  for (const lang of languages) {
    const langSkillsDir = path.join(baseDir, 'src/skills', lang);
    await copySkills(langSkillsDir, skillsDir);
  }

  // Copy agents
  const agentsDir = path.join(claudeDir, 'agents');
  await fs.mkdir(agentsDir, { recursive: true });

  // Copy general agents
  const generalAgentsDir = path.join(baseDir, 'src/agents/general');
  await copyAgents(generalAgentsDir, agentsDir);

  // Copy language-specific agents
  for (const lang of languages) {
    const langAgentsDir = path.join(baseDir, 'src/agents', lang);
    await copyAgents(langAgentsDir, agentsDir);
  }

  // Create CLAUDE.md with instructions
  const combinedInstructions = await combineInstructions(languages, baseDir);
  const claudeFile = path.join(outputDir, 'CLAUDE.md');
  await fs.writeFile(claudeFile, combinedInstructions);

  console.log(`✓ Created ${claudeFile}`);
  console.log(`✓ Created skills in ${skillsDir}`);
  console.log(`✓ Created agents in ${agentsDir}`);
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

async function clearAiConfigs(outputDir, aiSystems) {
  console.log('Clearing existing AI configurations...');

  const aiConfigPaths = getAiConfigPaths(outputDir);

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

  if (config.clearAiConfigs) {
    await clearAiConfigs(config.outputDir, config.aiSystems);
  }

  return;

  console.log('AI Configuration Generator');
  console.log('==========================');
  console.log(`Languages: ${config.languages.join(', ')}`);
  console.log(`AI Systems: ${config.aiSystems.join(', ')}`);
  console.log(`Output Directory: ${config.outputDir}\n`);

  const baseDir = path.resolve(__dirname, '..');

  for (const aiSystem of config.aiSystems) {
    switch (aiSystem.toLowerCase()) {
      case 'copilot':
        await generateCopilotConfig(config.languages, baseDir, config.outputDir);
        break;
      case 'junie':
        await generateJunieConfig(config.languages, baseDir, config.outputDir);
        break;
      case 'claude':
        await generateClaudeConfig(config.languages, baseDir, config.outputDir);
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
