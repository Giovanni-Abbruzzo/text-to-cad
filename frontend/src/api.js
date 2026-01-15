export const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:8000";

export async function processInstruction(text, useAI = false) { 
  const r = await fetch(`${API_BASE}/process_instruction`, {
    method: "POST",
    headers: {"Content-Type":"application/json"},
    body: JSON.stringify({ instruction: text, use_ai: useAI })
  });
  if (!r.ok) throw new Error(`API error ${r.status}`);
  return r.json();
}

export async function planInstruction(payload) {
  const r = await fetch(`${API_BASE}/plan`, {
    method: "POST",
    headers: {"Content-Type":"application/json"},
    body: JSON.stringify(payload)
  });
  if (!r.ok) throw new Error(`API error ${r.status}`);
  return r.json();
}

export async function fetchCommands(limit = 20) {
  const r = await fetch(`${API_BASE}/commands?limit=${limit}`);
  if (!r.ok) throw new Error(`API error ${r.status}`);
  return r.json();
}

// Job Management API Functions

/**
 * Start a new job for CAD processing simulation.
 * 
 * @param {number|null} commandId - Optional ID of a saved command to associate with this job
 * @returns {Promise<Object>} Job creation response with job_id, status, and progress
 * @throws {Error} If the API request fails
 * 
 * @example
 * // Start a job without command association
 * const job = await startJob();
 * console.log(job); // { job_id: "uuid", status: "queued", progress: 0 }
 * 
 * // Start a job associated with a saved command
 * const job = await startJob(123);
 */
export async function startJob(commandId = null) {
  const requestBody = commandId !== null ? { command_id: commandId } : {};
  
  const r = await fetch(`${API_BASE}/jobs`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(requestBody)
  });
  
  if (!r.ok) {
    const errorText = await r.text();
    throw new Error(`Failed to start job (${r.status}): ${errorText}`);
  }
  
  return r.json(); // { job_id, status, progress }
}

/**
 * Fetch the current status of a job by its ID.
 * 
 * @param {string} jobId - The unique job identifier
 * @returns {Promise<Object>} Current job state with status, progress, and metadata
 * @throws {Error} If the API request fails or job is not found
 * 
 * @example
 * const jobStatus = await fetchJob("550e8400-e29b-41d4-a716-446655440000");
 * console.log(jobStatus); 
 * // {
 * //   job_id: "550e8400-e29b-41d4-a716-446655440000",
 * //   status: "running",
 * //   progress: 45,
 * //   error: null,
 * //   created_at: "2025-08-13T19:00:00.000000",
 * //   updated_at: "2025-08-13T19:00:15.000000"
 * // }
 */
export async function fetchJob(jobId) {
  if (!jobId) {
    throw new Error("Job ID is required");
  }
  
  const r = await fetch(`${API_BASE}/jobs/${encodeURIComponent(jobId)}`);
  
  if (!r.ok) {
    if (r.status === 404) {
      throw new Error(`Job with ID '${jobId}' not found`);
    }
    const errorText = await r.text();
    throw new Error(`Failed to fetch job status (${r.status}): ${errorText}`);
  }
  
  return r.json(); // { job_id, status, progress, error?, created_at, updated_at }
}

/**
 * Poll a job until it completes (succeeds or fails).
 * 
 * @param {string} jobId - The unique job identifier
 * @param {function} onProgress - Optional callback function called on each progress update
 * @param {number} pollInterval - Polling interval in milliseconds (default: 1000)
 * @param {number} maxPolls - Maximum number of polls before giving up (default: 120)
 * @returns {Promise<Object>} Final job state when completed
 * @throws {Error} If polling fails or times out
 * 
 * @example
 * // Basic polling
 * const finalJob = await pollJobUntilComplete(jobId);
 * 
 * // Polling with progress callback
 * const finalJob = await pollJobUntilComplete(jobId, (job) => {
 *   console.log(`Progress: ${job.progress}%`);
 * });
 */
export async function pollJobUntilComplete(jobId, onProgress = null, pollInterval = 1000, maxPolls = 120) {
  let pollCount = 0;
  
  while (pollCount < maxPolls) {
    try {
      const job = await fetchJob(jobId);
      
      // Call progress callback if provided
      if (onProgress && typeof onProgress === 'function') {
        onProgress(job);
      }
      
      // Check if job is complete
      if (job.status === 'succeeded' || job.status === 'failed') {
        return job;
      }
      
      // Wait before next poll
      await new Promise(resolve => setTimeout(resolve, pollInterval));
      pollCount++;
      
    } catch (error) {
      // If it's a 404, the job might have been cleaned up
      if (error.message.includes('not found')) {
        throw new Error(`Job ${jobId} was not found during polling. It may have been cleaned up.`);
      }
      throw error;
    }
  }
  
  throw new Error(`Job polling timed out after ${maxPolls} attempts (${maxPolls * pollInterval / 1000}s)`);
}
