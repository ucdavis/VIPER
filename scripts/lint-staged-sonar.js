#!/usr/bin/env node

const { execFileSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Platform-specific constants
const IS_WINDOWS = process.platform === 'win32';

// Security-related rule detection
function isSecurityRelatedRule(ruleId) {
  // CA-series rules are Microsoft security analyzers
  if (ruleId.startsWith('CA')) {
    return true;
  }
  
  // SonarAnalyzer security rules (S2xxx-S6xxx range contains many security rules)
  const securityRules = [
    'S2083', // Path traversal
    'S2091', // XPath injection  
    'S2092', // Cookie security
    'S2245', // Insecure random
    'S3649', // SQL injection
    'S4426', // Weak crypto
    'S5131', // CSRF protection
    'S2068', // Hardcoded credentials
    'S2612', // File permissions
    'S4423', // Weak SSL/TLS
    'S4507', // Debugging enabled
    'S5042', // Zip slip
    'S5122', // CORS policy
    'S5131', // CSRF
    'S5144', // Server certificates
    'S5443', // Operating system command injection
    'S5659', // JWT signature verification
    'S5693', // XML validation
    'S5738', // Hash algorithm
    'S5766', // Deserialization
    'S5856'  // Regex injection
  ];
  
  return securityRules.includes(ruleId);
}


// Get staged files from lint-staged
const stagedFiles = process.argv.slice(2);

if (stagedFiles.length === 0) {
  console.log('No staged files to check with SonarAnalyzer');
  process.exit(0);
}

// Convert to relative paths and normalize
const normalizedStagedFiles = stagedFiles.map(f => {
  // Convert absolute path to relative path from project root
  const relativePath = path.relative(process.cwd(), f).replace(/\\/g, '/');
  return relativePath;
});

console.log(`Checking SonarAnalyzer issues for ${normalizedStagedFiles.length} staged files...`);

// Determine which project to build based on staged files
const webFiles = normalizedStagedFiles.filter(f => f.startsWith('web/'));
const testFiles = normalizedStagedFiles.filter(f => f.startsWith('test/'));

// Runs SonarAnalyzer for a project and returns { errors, warnings } counts
function runSonarForProject(projectPath, projectFiles, projectName) {
  if (projectFiles.length === 0) return { errors: 0, warnings: 0 };

  console.log(`Building ${projectName} to run SonarAnalyzer...`);

  try {
    // Run build to trigger SonarAnalyzer
    const result = execFileSync('dotnet', ['build', '--no-restore'], {
      cwd: projectPath,
      encoding: 'utf8',
      timeout: 120000
    });

    // Process didn't throw, but warnings might be in stdout
    return filterSonarWarnings(result, projectFiles, projectName);

  } catch (error) {
    // Build failed or had warnings - check both stdout and stderr
    const fullOutput = (error.stdout || '') + '\n' + (error.stderr || '');
    const issueCount = filterSonarWarnings(fullOutput, projectFiles, projectName);

    // Don't fail on build errors that are just warnings
    if (!error.message.includes('error ') || error.status === 0) {
      console.log(`Build completed with warnings in ${projectName}`);
      return issueCount;
    } else {
      console.error(`Build failed in ${projectName}:`, error.message);
      process.exit(1);
    }
  }
}

// Filters SonarAnalyzer output for staged files and returns { errors, warnings } counts
function filterSonarWarnings(output, stagedFiles, projectName) {
  const lines = output.split('\n');
  const sonarWarnings = [];
  const sonarErrors = [];

  lines.forEach(line => {
    // Look for analyzer issues: path(line,col): warning/error S1234/CA1234: message
    const sonarMatch = line.match(/^(.+?)\((\d+),(\d+)\):\s+(warning|error)\s+((S|CA)\d+):\s+(.+)/);

    if (sonarMatch) {
      const [, filePath, lineNum, col, severity, ruleId, , message] = sonarMatch;

      // Normalize the file path from the warning
      const normalizedWarningPath = path.relative(process.cwd(), filePath).replace(/\\/g, '/');

      // Check if this issue is for one of our staged files using normalized relative paths
      const isForStagedFile = stagedFiles.some(stagedFile => {
        // Normalize both paths for comparison
        const normalizedStagedFile = stagedFile.replace(/\\/g, '/');
        
        // Exact match (most reliable)
        if (normalizedWarningPath === normalizedStagedFile) {
          return true;
        }
        
        // Check if the warning path ends with the staged file path
        // but ensure it's a proper path boundary (not just substring match)
        if (normalizedWarningPath.endsWith('/' + normalizedStagedFile) || 
            normalizedWarningPath.endsWith('\\' + normalizedStagedFile.replace(/\//g, '\\'))) {
          return true;
        }
        
        return false;
      });

      if (isForStagedFile) {
        const issue = {
          file: normalizedWarningPath,
          line: lineNum,
          col: col,
          rule: ruleId,
          message: message.trim(),
          severity: severity
        };

        // Categorize as security vs quality based on rule patterns
        const isSecurityRule = isSecurityRelatedRule(ruleId);
        
        // Security rules should always be treated as errors (blocking)
        // CA-series rules (Microsoft analyzers) and specific S-series security rules
        if (isSecurityRule || severity === 'error') {
          sonarErrors.push(issue);
        } else {
          sonarWarnings.push(issue);
        }
      }
    }
  });

  const totalIssues = sonarErrors.length + sonarWarnings.length;

  // Report all issues found
  if (totalIssues > 0) {
    console.log(`\n🔍 SonarAnalyzer found ${totalIssues} issue(s) in staged ${projectName} files:`);
    
    // Show errors (security issues that block commits)
    if (sonarErrors.length > 0) {
      console.log(`\n🚨 SECURITY ERRORS (${sonarErrors.length}) - BLOCKING COMMIT:`);
      sonarErrors.forEach(issue => {
        console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
      });
    }

    // Show warnings (code quality issues)
    if (sonarWarnings.length > 0) {
      console.log(`\n⚠️  CODE QUALITY WARNINGS (${sonarWarnings.length}):`);
      sonarWarnings.forEach(issue => {
        console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
      });
    }
    
    console.log('');
  } else {
    console.log(`✅ No SonarAnalyzer issues found in staged ${projectName} files`);
  }

  // Return both errors and warnings count for dual-mode support
  return { errors: sonarErrors.length, warnings: sonarWarnings.length };
}

try {
  let totalSecurityErrors = 0;
  let totalWarnings = 0;
  
  // Check if we should block on warnings (for lint:staged vs lint:precommit)
  const blockOnWarnings = process.env.LINT_BLOCK_ON_WARNINGS === 'true';

  // Run for each project that has staged files
  if (webFiles.length > 0) {
    const webResult = runSonarForProject('web', webFiles, 'web');
    totalSecurityErrors += webResult.errors;
    totalWarnings += webResult.warnings;
  }
  if (testFiles.length > 0) {
    const testResult = runSonarForProject('test', testFiles, 'test');
    totalSecurityErrors += testResult.errors;
    totalWarnings += testResult.warnings;
  }

  // Summary for developer visibility
  const totalIssues = totalSecurityErrors + totalWarnings;
  console.log(`\n📊 SonarAnalyzer Summary: ${totalIssues} total issues (${totalSecurityErrors} security errors, ${totalWarnings} warnings)`);

  // Determine what should block the commit
  const shouldBlock = totalSecurityErrors > 0 || (blockOnWarnings && totalWarnings > 0);

  if (shouldBlock) {
    if (totalSecurityErrors > 0) {
      console.error('\n🛑 COMMIT BLOCKED due to SECURITY ERRORS.');
      console.error('🔒 Security errors MUST be fixed before committing.');
    }
    if (blockOnWarnings && totalWarnings > 0 && totalSecurityErrors === 0) {
      console.log('\n⚠️  LINTING STOPPED due to code quality warnings.');
      console.log('💡 These warnings would not block commits in normal mode. Fix warnings above or use lint:precommit to ignore warnings.');
    }
    process.exit(1);
  } else if (totalWarnings > 0) {
    console.log('\n✅ COMMIT ALLOWED - Only warnings detected (non-blocking).');
    console.log('💡 Run `npm run lint:staged` to see and fix all warnings.');
  } else {
    console.log('\n✅ All staged files pass SonarAnalyzer checks');
  }
} catch (error) {
  console.error('SonarAnalyzer check failed:', error.message);
  process.exit(1);
}