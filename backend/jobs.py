"""
Async job runner module for simulating long-running CAD operations.

This module provides a simple in-memory job tracking system using asyncio
for simulating CAD work. It's designed to be easily replaceable with real
CAD operations while maintaining the same API.
"""

import asyncio
import uuid
from datetime import datetime
from typing import Dict, Optional
import logging

# Configure logging
logger = logging.getLogger(__name__)

# Module-level job storage
# Structure: {"job_id": {"id": str, "status": str, "progress": int, "error": str|None, "created_at": datetime, "updated_at": datetime, "meta": dict|None}}
JOBS: Dict[str, Dict] = {}

# Optional lock for thread safety when updating JOBS
_jobs_lock = asyncio.Lock()

# Job status constants
STATUS_QUEUED = "queued"
STATUS_RUNNING = "running"
STATUS_SUCCEEDED = "succeeded"
STATUS_FAILED = "failed"

# Progress simulation settings
PROGRESS_STEPS = 20  # Number of progress increments (0 to 100 in steps of 5)
STEP_DELAY = 0.15    # Seconds to wait between progress updates


def start_job(meta: Optional[dict] = None) -> str:
    """
    Start a new async job and return its ID.
    
    Args:
        meta: Optional metadata dictionary to associate with the job
        
    Returns:
        str: The unique job ID (UUID)
    """
    job_id = str(uuid.uuid4())
    now = datetime.now()
    
    # Initialize job record
    job_record = {
        "id": job_id,
        "status": STATUS_QUEUED,
        "progress": 0,
        "error": None,
        "created_at": now,
        "updated_at": now,
        "meta": meta or {}
    }
    
    # Store job record
    JOBS[job_id] = job_record
    
    # Start the async job task
    asyncio.create_task(_run_job(job_id, meta))
    
    logger.info(f"Started job {job_id} with meta: {meta}")
    return job_id


async def _run_job(job_id: str, meta: Optional[dict] = None) -> None:
    """
    Internal function to simulate running a CAD job.
    
    This function simulates work by incrementing progress from 0 to 100
    in steps with small delays. In a real implementation, this would be
    replaced with actual CAD operations.
    
    Args:
        job_id: The unique job identifier
        meta: Optional metadata for the job
    """
    try:
        # Update status to running
        async with _jobs_lock:
            if job_id in JOBS:
                JOBS[job_id]["status"] = STATUS_RUNNING
                JOBS[job_id]["updated_at"] = datetime.now()
        
        logger.info(f"Job {job_id} started running")
        
        # Simulate work with progress updates
        progress_increment = 100 // PROGRESS_STEPS
        
        for step in range(PROGRESS_STEPS + 1):
            # Calculate current progress (0-100)
            current_progress = min(step * progress_increment, 100)
            
            # Update progress
            async with _jobs_lock:
                if job_id in JOBS:
                    JOBS[job_id]["progress"] = current_progress
                    JOBS[job_id]["updated_at"] = datetime.now()
            
            logger.debug(f"Job {job_id} progress: {current_progress}%")
            
            # Don't sleep on the last iteration
            if step < PROGRESS_STEPS:
                await asyncio.sleep(STEP_DELAY)
        
        # Mark job as succeeded
        async with _jobs_lock:
            if job_id in JOBS:
                JOBS[job_id]["status"] = STATUS_SUCCEEDED
                JOBS[job_id]["progress"] = 100
                JOBS[job_id]["updated_at"] = datetime.now()
        
        logger.info(f"Job {job_id} completed successfully")
        
    except Exception as e:
        # Handle any errors during job execution
        error_message = str(e)
        logger.error(f"Job {job_id} failed with error: {error_message}")
        
        async with _jobs_lock:
            if job_id in JOBS:
                JOBS[job_id]["status"] = STATUS_FAILED
                JOBS[job_id]["error"] = error_message
                JOBS[job_id]["updated_at"] = datetime.now()


def get_job(job_id: str) -> Optional[dict]:
    """
    Get the current state of a job by its ID.
    
    Args:
        job_id: The unique job identifier
        
    Returns:
        dict|None: A copy of the job record, or None if job doesn't exist
    """
    if job_id not in JOBS:
        return None
    
    # Return a copy to prevent external modification
    job_record = JOBS[job_id].copy()
    
    # Convert datetime objects to ISO strings for JSON serialization
    job_record["created_at"] = job_record["created_at"].isoformat()
    job_record["updated_at"] = job_record["updated_at"].isoformat()
    
    return job_record


def get_all_jobs() -> Dict[str, dict]:
    """
    Get all jobs in the system.
    
    Returns:
        Dict[str, dict]: Dictionary mapping job IDs to job records
    """
    result = {}
    for job_id, job_record in JOBS.items():
        # Create a copy and convert datetime objects
        job_copy = job_record.copy()
        job_copy["created_at"] = job_copy["created_at"].isoformat()
        job_copy["updated_at"] = job_copy["updated_at"].isoformat()
        result[job_id] = job_copy
    
    return result


def clear_jobs() -> None:
    """
    Clear all jobs from the in-memory store.
    
    This is useful for testing or cleanup purposes.
    """
    global JOBS
    JOBS.clear()
    logger.info("Cleared all jobs from memory")


# Example usage and testing functions
async def test_job_runner():
    """
    Test function to demonstrate the job runner functionality.
    """
    print("Testing job runner...")
    
    # Start a test job
    job_id = start_job({"test": True, "description": "Test CAD operation"})
    print(f"Started job: {job_id}")
    
    # Monitor job progress
    while True:
        job_status = get_job(job_id)
        if job_status:
            print(f"Job {job_id}: {job_status['status']} - {job_status['progress']}%")
            
            if job_status["status"] in [STATUS_SUCCEEDED, STATUS_FAILED]:
                if job_status["status"] == STATUS_FAILED:
                    print(f"Job failed with error: {job_status['error']}")
                break
        
        await asyncio.sleep(0.5)
    
    print("Test completed!")


if __name__ == "__main__":
    # Run test if module is executed directly
    asyncio.run(test_job_runner())
