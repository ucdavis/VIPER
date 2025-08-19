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

// Project root directory path (for stylelint config)
const projectRoot = path.join(__dirname, '..');

// Sanitize all file paths and filter out null results (missing files)
const files = rawFiles
  .map(filePath => sanitizeFilePath(filePath, projectRoot, ['.css', '.vue']))
  .filter(file => file !== null);

// Function to analyze Stylelint output and separate accessibility vs style issues
function analyzeStylelintOutput(stdout, stderr) {
  const accessibilityErrors = [];
  const accessibilityWarnings = [];
  const styleIssues = [];
  const output = stdout + stderr;

  // Parse JSON output from Stylelint
  let stylelintResults = [];
  let jsonOutput = '';

  jsonOutput = stdout.trim();

  try {
    if (jsonOutput.trim()) {
      stylelintResults = JSON.parse(jsonOutput);
    }
  } catch (error) {
    console.warn('Failed to parse Stylelint JSON output, analyzing text output');
    return analyzeTextOutput(output);
  }

  // Process each file's results
  stylelintResults.forEach(fileResult => {
    if (fileResult.warnings) {
      fileResult.warnings.forEach(warning => {
        const issue = {
          file: fileResult.source,
          line: warning.line,
          col: warning.column,
          level: warning.severity === 'error' ? 'error' : 'warning',
          message: warning.text,
          rule: warning.rule || 'unknown'
        };

        // Categorize issues using shared rule definitions
        const category = categorizeRule(issue.rule);
        
        if (category === 'critical-accessibility') {
          accessibilityErrors.push(issue);
        } else if (category === 'accessibility-warning') {
          accessibilityWarnings.push(issue);
        } else {
          styleIssues.push(issue);
        }
      });
    }
  });

  return { accessibilityErrors, accessibilityWarnings, styleIssues, output };
}

// Fallback function to analyze text output when JSON parsing fails
function analyzeTextOutput(output) {
  const accessibilityErrors = [];
  const accessibilityWarnings = [];
  const styleIssues = [];
  const lines = output.split('\n');

  lines.forEach(line => {
    // Parse stylelint text output format: file:line:col ✖ message [rule]
    const match = line.match(/(.+?):(\d+):(\d+)\s+✖\s+(.+?)\s+\[(.+?)\]/);
    if (match) {
      const [, file, lineNum, col, message, rule] = match;
      const issue = {
        file,
        line: parseInt(lineNum),
        col: parseInt(col),
        level: 'error',
        message,
        rule
      };

      // Use shared categorization
      const category = categorizeRule(issue.rule);
      
      if (category === 'critical-accessibility') {
        accessibilityErrors.push(issue);
      } else if (category === 'accessibility-warning') {
        accessibilityWarnings.push(issue);
      } else {
        styleIssues.push(issue);
      }
    }
  });

  return { accessibilityErrors, accessibilityWarnings, styleIssues, output };
}

try {
  // Create summary reporter for CSS linting
  const reporter = createSummaryReporter('CSS');

  // Run Stylelint using shared command runner
  const stylelintArgs = [
    ...(fixFlag ? ['--fix'] : []),
    '--formatter', 'json',
    '--allow-empty-input',
    ...files
  ];

  console.log(`🎨 Running Stylelint accessibility and style checks on ${files.length} CSS/Vue files...`);
  const stylelintResult = runCommand('stylelint', stylelintArgs, 'Stylelint', projectRoot);

  // Check for fatal errors
  if (stylelintResult.status !== 0 && stylelintResult.status !== 2) {
    console.error('\n❌ Stylelint command failed:');
    if (stylelintResult.stdout) console.error(stylelintResult.stdout);
    if (stylelintResult.stderr) console.error(stylelintResult.stderr);
    console.error('\n🛑 COMMIT BLOCKED - Stylelint execution failed');
    process.exit(1);
  }

  // Warn about configuration errors (status 2)
  if (stylelintResult.status === 2) {
    console.warn('\n⚠️  STYLELINT CONFIGURATION WARNING: Status 2 indicates potential config issues');
    console.warn('📋 Consider reviewing stylelint.config.mjs if unexpected behavior occurs');
  }

  // Filter out deprecation warnings
  const cleanStderr = stylelintResult.stderr ?
    stylelintResult.stderr
      .split('\n')
      .filter(line => !line.includes('DeprecationWarning'))
      .join('\n')
      .trim() : '';

  // Analyze output
  const { accessibilityErrors, accessibilityWarnings, styleIssues } = analyzeStylelintOutput(
    stylelintResult.stdout,
    cleanStderr
  );

  // Display results with clear separation
  if (accessibilityErrors.length > 0) {
    console.log(`\n🚨 WCAG 2.1 AA VIOLATIONS (${accessibilityErrors.length}) - FEDERAL COMPLIANCE REQUIRED:`);
    accessibilityErrors.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  // Only show warnings when blocking on warnings is enabled
  const blockOnWarnings = shouldBlockOnWarnings();
  
  if (blockOnWarnings && accessibilityWarnings.length > 0) {
    console.log(`\n🔶 ACCESSIBILITY WARNINGS (${accessibilityWarnings.length}) - WCAG 2.1 AA SUPPORTIVE:`);
    accessibilityWarnings.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (blockOnWarnings && styleIssues.length > 0) {
    console.log(`\n⚠️  STYLE ISSUES (${styleIssues.length}) - FORMATTING & CODE STYLE:`);
    styleIssues.forEach(issue => {
      console.log(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`);
    });
  }

  if (accessibilityErrors.length === 0 && accessibilityWarnings.length === 0 && styleIssues.length === 0) {
    console.log('✅ No CSS/Stylelint issues found in staged files');
  } else if (!blockOnWarnings && accessibilityErrors.length === 0) {
    console.log('✅ No critical CSS violations found');
  }

  // Use shared summary reporter - adjust counts based on what we're showing
  const totalIssues = accessibilityErrors.length + (blockOnWarnings ? (accessibilityWarnings.length + styleIssues.length) : 0);
  const criticalCount = accessibilityErrors.length;
  const warningCount = blockOnWarnings ? (accessibilityWarnings.length + styleIssues.length) : 0;
  
  if (totalIssues > 0 || blockOnWarnings) {
    reporter.logSummary(totalIssues, criticalCount, warningCount);
  }

  // Handle federal compliance requirements
  if (accessibilityErrors.length > 0) {
    console.log('\n🛑 COMMIT BLOCKED - FEDERAL ACCESSIBILITY COMPLIANCE REQUIRED');
    console.log('🏛️  WCAG 2.1 AA violations MUST be fixed for federal compliance.');
    console.log('📋 Review violations above and use accessible code patterns.');
    process.exit(1);
  }
  
  // Use shared reporter for standard warning handling
  reporter.handleCommitDecision(false, warningCount > 0);

} catch (error) {
  console.error('Unexpected error:', error);
  process.exit(1);
}
