#!/usr/bin/env node

const { execFileSync } = require('child_process');
const path = require('path');
const { 
  createSummaryReporter,
  shouldBlockOnWarnings
} = require('./lib/lint-staged-common');
const { 
  isDotNetSecurityRule 
} = require('./lib/critical-rules');

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

        // Use shared security rule categorization
        const isSecurityRule = isDotNetSecurityRule(ruleId);
        
        // Security rules should always be treated as errors (blocking)
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

    // Show warnings (code quality issues) - only when blocking on warnings is enabled
    const blockOnWarnings = shouldBlockOnWarnings();
    
    if (blockOnWarnings && sonarWarnings.length > 0) {
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
  // Create shared summary reporter
  const reporter = createSummaryReporter('SonarAnalyzer');

  let totalSecurityErrors = 0;
  let totalWarnings = 0;

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

  // Use shared summary reporting
  const totalIssues = totalSecurityErrors + totalWarnings;
  reporter.logSummary(totalIssues, totalSecurityErrors, totalWarnings);

  // Handle security errors first  
  if (totalSecurityErrors > 0) {
    console.error('\n🛑 COMMIT BLOCKED due to SECURITY ERRORS.');
    console.error('🔒 Security errors MUST be fixed before committing.');
    process.exit(1);
  }

  // Use shared reporter for warning handling
  reporter.handleCommitDecision(false, totalWarnings > 0);

  // If we get here with no issues, show success
  if (totalIssues === 0) {
    console.log('\n✅ All staged files pass SonarAnalyzer checks');
  }

} catch (error) {
  console.error('SonarAnalyzer check failed:', error.message);
  process.exit(1);
}