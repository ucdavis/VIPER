#!/usr/bin/env node

const path = require('path');
const { 
  parseArguments, 
  sanitizeFilePath, 
  runCommand, 
  createSummaryReporter,
  shouldBlockOnWarnings
} = require('./lib/lint-staged-common');
const { 
  categorizeRule 
} = require('./lib/critical-rules');

// Parse command line arguments
const { fixFlag, rawFiles } = parseArguments();

if (rawFiles.length === 0) {
  process.exit(0);
}

// Base directories for different contexts
const projectRoot = path.join(__dirname, '..');
const vueAppDir = path.join(__dirname, '..', 'VueApp');

// Sanitize file paths - now all ESLint runs from project root
const files = rawFiles
  .map(filePath => sanitizeFilePath(filePath, projectRoot, ['.vue', '.js', '.ts', '.jsx', '.tsx', '.mjs']))
  .filter(file => file !== null);

// Function to analyze ESLint output and separate security vs quality issues
function analyzeESLintOutput(stdout, stderr) {
  const securityErrors = [];
  const qualityIssues = [];
  
  // Parse JSON output from ESLint
  let eslintResults = [];
  try {
    if (stdout.trim()) {
      eslintResults = JSON.parse(stdout);
    }
  } catch (error) {
    // Fallback to stderr if stdout parsing fails
    try {
      if (stderr.trim()) {
        eslintResults = JSON.parse(stderr);
      }
    } catch (fallbackError) {
      console.warn('Failed to parse ESLint JSON output, continuing with empty results');
      return { securityErrors, qualityIssues, output: stdout + stderr };
    }
  }
  
  // Process each file's results
  eslintResults.forEach(fileResult => {
    fileResult.messages.forEach(message => {
      const issue = {
        file: fileResult.filePath,
        line: message.line,
        col: message.column,
        level: message.severity === 2 ? 'error' : 'warning',
        message: message.message,
        rule: message.ruleId || 'unknown'
      };
      
      // Use shared rule categorization
      const category = categorizeRule(issue.rule);
      
      if (category === 'critical-security') {
        securityErrors.push(issue);
      } else {
        qualityIssues.push(issue);
      }
    });
  });
  
  return { securityErrors, qualityIssues, output: stdout + stderr };
}

try {
  let hasSecurityErrors = false;
  let hasTypeErrors = false;
  
  // Create summary reporter for Vue/JS/TS linting
  const reporter = createSummaryReporter('Vue/JS/TS');

  // 1. Run ESLint using shared command runner
  // Determine working directory and config based on files being linted
  
  console.log(`🔍 Running ESLint security and quality checks on ${files.length} Vue/JS/TS files...`);
  
  let allSecurityErrors = [];
  let allQualityIssues = [];
  
  // Separate Vue files from other JS/TS files for different linting approaches
  const vueFiles = files.filter(f => {
    const norm = f.replace(/\\/g,'/');
    return norm.startsWith('VueApp/') && (norm.endsWith('.vue') || /\.(t|j)sx?$/.test(norm));
  });
  const otherFiles = files.filter(f => !vueFiles.includes(f));
  
  // Run VueApp ESLint for Vue files (has all Vue/security plugins)
  if (vueFiles.length > 0) {
    console.log(`Checking ${vueFiles.length} Vue/browser files with VueApp ESLint...`);
    const relativeVueFiles = vueFiles.map(f => path.relative(vueAppDir, path.resolve(projectRoot, f)));
    const vueEslintArgs = [
      ...(fixFlag ? ['--fix'] : []),
      '--format', 'json',
      '--no-warn-ignored',
      ...relativeVueFiles
    ];
    
    const vueEslintResult = runCommand('eslint', vueEslintArgs, 'ESLint (Vue)', vueAppDir);
    
    if (vueEslintResult.status > 1) {
      console.error('\n❌ ESLint command failed:');
      if (vueEslintResult.stdout) console.error(vueEslintResult.stdout);
      if (vueEslintResult.stderr) console.error(vueEslintResult.stderr);
      console.error('\n🛑 COMMIT BLOCKED - ESLint execution failed');
      process.exit(1);
    }
    
    const { securityErrors: vueSecurityErrors, qualityIssues: vueQualityIssues } = analyzeESLintOutput(
      vueEslintResult.stdout, 
      vueEslintResult.stderr
    );
    
    allSecurityErrors.push(...vueSecurityErrors);
    allQualityIssues.push(...vueQualityIssues);
  }
  
  // Run unified ESLint for scripts and config files
  if (otherFiles.length > 0) {
    console.log(`Checking ${otherFiles.length} scripts/config files with unified ESLint...`);
    const relativeOtherFiles = otherFiles.map(f => path.relative(vueAppDir, path.resolve(projectRoot, f)));
    const eslintArgs = [
      ...(fixFlag ? ['--fix'] : []),
      '--config', '../eslint.config.mjs',
      '--format', 'json',
      '--no-warn-ignored',
      ...relativeOtherFiles
    ];
    
    const eslintResult = runCommand('eslint', eslintArgs, 'ESLint (scripts)', vueAppDir);
    
    // Check if ESLint command had a fatal error (only if exit code > 1)
    if (eslintResult.status > 1) {
      console.error('\n❌ ESLint command failed:');
      if (eslintResult.stdout) console.error(eslintResult.stdout);
      if (eslintResult.stderr) console.error(eslintResult.stderr);
      console.error('\n🛑 COMMIT BLOCKED - ESLint execution failed');
      process.exit(1);
    }
    
    // Analyze ESLint output for security vs quality issues
    const { securityErrors, qualityIssues } = analyzeESLintOutput(
      eslintResult.stdout, 
      eslintResult.stderr
    );
    
    allSecurityErrors.push(...securityErrors);
    allQualityIssues.push(...qualityIssues);
  }
  
  // Display results with clear separation
  if (allSecurityErrors.length > 0) {
    console.log(`\n🚨 CRITICAL ERRORS (${allSecurityErrors.length}) - MUST FIX:`);
    allSecurityErrors.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
    hasSecurityErrors = true;
  }

  // Only show warnings when blocking on warnings is enabled
  const blockOnWarnings = shouldBlockOnWarnings();
  
  if (blockOnWarnings && allQualityIssues.length > 0) {
    console.log(`\n⚠️  CODE QUALITY WARNINGS (${allQualityIssues.length}):`);
    allQualityIssues.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (allSecurityErrors.length === 0 && allQualityIssues.length === 0) {
    console.log('✅ No ESLint issues found in staged Vue/JS/TS files');
  } else if (!blockOnWarnings && allSecurityErrors.length === 0) {
    console.log('✅ No critical security violations found');
  }

  // 2. Run TypeScript type checking using proper project configurations
  const tsFiles = files.filter(file => /\.(ts|tsx|vue)$/.test(file));

  if (tsFiles.length > 0) {
    // Separate files by their appropriate TypeScript project
    // Any TS/JS files outside of src/ are considered Node.js config files
    const nodeFiles = tsFiles.filter(file => !file.startsWith('src/'));
    const appFiles = tsFiles.filter(file => !nodeFiles.includes(file));

    console.log('\n🔍 Running TypeScript type checking...');
    
    // Check Node.js config files with tsconfig.node.json
    if (nodeFiles.length > 0) {
      console.log(`Checking ${nodeFiles.length} Node.js config files...`);
      const nodeResult = runCommand('tsc', ['--project', 'tsconfig.node.json', '--noEmit'], 'TypeScript Node config checking', vueAppDir);
      if (!nodeResult.success) {
        console.log('\n❌ TypeScript type checking failed:');
        console.log(nodeResult.stdout);
        console.log(nodeResult.stderr);
        hasTypeErrors = true;
      }
    }

    // Check app files with tsconfig.app.json
    if (appFiles.length > 0) {
      console.log(`Checking ${appFiles.length} application files...`);
      const appResult = runCommand('tsc', ['--project', 'tsconfig.app.json', '--noEmit'], 'TypeScript app checking', vueAppDir);
      if (!appResult.success) {
        console.log('\n❌ TypeScript type checking failed:');
        console.log(appResult.stdout);
        console.log(appResult.stderr);
        hasTypeErrors = true;
      }
    }

    if (!hasTypeErrors) {
      console.log('✅ TypeScript type checking passed');
    }
  }

  // Use shared summary reporter - adjust counts based on what we're showing
  const totalIssues = allSecurityErrors.length + (blockOnWarnings ? allQualityIssues.length : 0);
  const criticalCount = allSecurityErrors.length;
  const warningCount = blockOnWarnings ? allQualityIssues.length : 0;
  
  if (totalIssues > 0 || blockOnWarnings) {
    reporter.logSummary(totalIssues, criticalCount, warningCount);
  }

  // Handle commit decision - security and type errors are both critical
  const hasCriticalIssues = hasSecurityErrors || hasTypeErrors;
  
  if (hasCriticalIssues) {
    console.log('\n🛑 COMMIT BLOCKED due to CRITICAL ERRORS.');
    console.log('🔒 Security and type errors MUST be fixed before committing.');
    process.exit(1);
  }
  
  // Use shared reporter for warning handling
  reporter.handleCommitDecision(false, allQualityIssues.length > 0);

} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}