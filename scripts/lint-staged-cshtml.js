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

// Project root directory path
const projectRoot = path.join(__dirname, '..');

// Sanitize all file paths and filter out null results (missing files)
const files = rawFiles
  .map(filePath => sanitizeFilePath(filePath, projectRoot, ['.cshtml']))
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
      
      // Categorize issues using shared rule definitions
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
  // Create summary reporter for CSHTML linting
  const reporter = createSummaryReporter('CSHTML');
  
  // Check if we should block on warnings (for lint:staged vs lint:precommit)
  const blockOnWarnings = shouldBlockOnWarnings();

  if (files.length === 0) {
    console.log('✅ No .cshtml files to check');
    process.exit(0);
  }

  // Run ESLint on .cshtml files using root config
  const eslintArgs = [
    ...(fixFlag ? ['--fix'] : []),
    '--format', 'json',
    ...files
  ];

  console.log(`🔍 Running ESLint security and quality checks on ${files.length} .cshtml files...`);
  
  const eslintResult = runCommand('eslint', eslintArgs, 'ESLint (CSHTML)', projectRoot);
  
  // Check if ESLint command had a fatal error (only if no JSON output produced)  
  if (eslintResult.status !== 0 && eslintResult.status !== 1) {
    // Exit codes 0 = no issues, 1 = linting errors found, >1 = fatal error
    console.error('\n❌ ESLint command failed with exit code:', eslintResult.status);
    if (eslintResult.stdout) console.error('STDOUT:', eslintResult.stdout);
    if (eslintResult.stderr) console.error('STDERR:', eslintResult.stderr);
    console.error('\n🛑 COMMIT BLOCKED - ESLint execution failed');
    process.exit(1);
  }
  
  // Analyze ESLint output for security vs quality issues
  const { securityErrors, qualityIssues } = analyzeESLintOutput(
    eslintResult.stdout, 
    eslintResult.stderr
  );

  // Display results with clear separation
  if (securityErrors.length > 0) {
    console.log(`\n🚨 SECURITY ERRORS (${securityErrors.length}) - BLOCKING COMMIT:`);
    securityErrors.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (blockOnWarnings && qualityIssues.length > 0) {
    console.log(`\n⚠️  CODE QUALITY ISSUES (${qualityIssues.length}):`);
    qualityIssues.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (securityErrors.length === 0 && qualityIssues.length === 0) {
    console.log('✅ No ESLint issues found in staged .cshtml files');
  } else if (!blockOnWarnings && securityErrors.length === 0) {
    console.log('✅ No critical CSHTML violations found');
  }

  // Use shared summary reporter - adjust counts based on what we're showing
  const totalIssues = securityErrors.length + (blockOnWarnings ? qualityIssues.length : 0);
  const criticalCount = securityErrors.length;
  const warningCount = blockOnWarnings ? qualityIssues.length : 0;
  
  if (totalIssues > 0 || blockOnWarnings) {
    reporter.logSummary(totalIssues, criticalCount, warningCount);
  }

  // Use shared reporter for standard handling
  const hasBlockingIssues = securityErrors.length > 0;
  const hasWarnings = qualityIssues.length > 0;
  
  reporter.handleCommitDecision(hasBlockingIssues, hasWarnings, 'SECURITY ERRORS');

} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}