const { execSync, spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Windows-only script for finding Viper.exe process by port

// Function to parse .env.local file
function loadEnvLocal() {
    const envPath = path.join(__dirname, '..', '.env.local');
    const env = {};
    
    if (fs.existsSync(envPath)) {
        const envContent = fs.readFileSync(envPath, 'utf8');
        const lines = envContent.split(/\r?\n/);
        
        for (const line of lines) {
            const trimmed = line.trim();
            if (trimmed && !trimmed.startsWith('#') && trimmed.includes('=')) {
                const [key, ...valueParts] = trimmed.split('=');
                env[key.trim()] = valueParts.join('=').trim();
            }
        }
    }
    
    return env;
}

// Get the HTTPS port from .env.local or use default
function getHttpsPort() {
    const env = loadEnvLocal();
    const port = env.ASPNETCORE_HTTPS_PORT || '7157'; // Default port
    // Basic validation: only digits allowed
    if (!/^\d+$/.test(port)) {
        throw new Error('Invalid ASPNETCORE_HTTPS_PORT value');
    }
    return port;
}

// Find process ID listening on the specified port
function findProcessByPort(port) {
    try {
        // Use Windows netstat to find the process listening on the port
        const netstat = spawnSync('netstat', ['-ano'], { encoding: 'utf8' });
        const output = netstat.stdout || '';
        const lines = output.split(/\r?\n/).filter(line => line.includes(`:${port}`) && line.trim());
        
        for (const line of lines) {
            // Look for LISTENING state
            if (line.includes('LISTENING')) {
                // Extract PID (last column)
                const parts = line.trim().split(/\s+/);
                const pid = parts[parts.length - 1];
                
                // Validate PID is numeric before using in shell command
                if (!/^\d+$/.test(pid)) {
                    continue; // Skip invalid PIDs
                }
                
                // Verify it's a Viper process using Windows tasklist
                try {
                    const tasklist = spawnSync('tasklist', [], { encoding: 'utf8' });
                    const tasklistOutput = tasklist.stdout || '';
                    if (tasklistOutput.includes('Viper.exe') && tasklistOutput.includes(pid)) {
                        return pid;
                    }
                } catch (error) {
                    // Continue searching if this PID check fails
                }
            }
        }
    } catch (error) {
        // If command fails, return null
    }
    
    return null;
}

// Main function
function main() {
    const port = getHttpsPort();
    const pid = findProcessByPort(port);
    
    if (pid) {
        // Ensure .vscode directory exists
        const vscDir = path.join(__dirname, '..', '.vscode');
        if (!fs.existsSync(vscDir)) fs.mkdirSync(vscDir, { recursive: true });

        // Write PID to file for VS Code to read
        const pidFile = path.join(vscDir, 'viper-pid.txt');
        fs.writeFileSync(pidFile, pid.toString());
        console.log(`Found Viper.exe process with PID: ${pid}`);
        process.exit(0);
    } else {
        console.error(`No Viper.exe process found listening on port ${port}`);
        process.exit(1);
    }
}

main();