import { useState, useEffect, useRef } from 'react'
import { processInstruction, fetchCommands, startJob, fetchJob } from './api.js'

function App() {
  const [instruction, setInstruction] = useState('')
  const [response, setResponse] = useState(null)
  const [loading, setLoading] = useState(false)
  const [history, setHistory] = useState([])
  const [useAI, setUseAI] = useState(false)
  
  // Job management state
  const [currentJob, setCurrentJob] = useState(null) // { job_id, status, progress, error }
  const [jobPolling, setJobPolling] = useState(false)
  const pollingIntervalRef = useRef(null)

  // Fetch command history on component mount
  useEffect(() => {
    const loadHistory = async () => {
      try {
        const historyData = await fetchCommands(20)
        setHistory(historyData)
      } catch (error) {
        console.error('Failed to load history:', error)
      }
    }
    loadHistory()
  }, [])
  
  // Cleanup polling interval on component unmount
  useEffect(() => {
    return () => {
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current)
      }
    }
  }, [])

  const handleSend = async () => {
    setLoading(true)
    setResponse(null)
    setCurrentJob(null)
    
    // Clear any existing polling
    if (pollingIntervalRef.current) {
      clearInterval(pollingIntervalRef.current)
    }

    try {
      const data = await processInstruction(instruction, useAI)
      setResponse(data)
      
      // Start a job after successful instruction processing
      try {
        console.log('Starting job for processed instruction...')
        const jobData = await startJob() // No command_id association for now
        setCurrentJob(jobData)
        setJobPolling(true)
        
        // Start polling for job status
        startJobPolling(jobData.job_id)
        
      } catch (jobError) {
        console.error('Failed to start job:', jobError)
        // Don't fail the whole process if job creation fails
      }
      
    } catch (error) {
      console.error('Error:', error)
      alert(`Failed to process instruction: ${error.message}`)
    } finally {
      setLoading(false)
    }
  }
  
  const startJobPolling = (jobId) => {
    console.log(`Starting polling for job: ${jobId}`)
    
    pollingIntervalRef.current = setInterval(async () => {
      try {
        const jobStatus = await fetchJob(jobId)
        setCurrentJob(jobStatus)
        
        console.log(`Job ${jobId}: ${jobStatus.status} - ${jobStatus.progress}%`)
        
        // Check if job is complete
        if (jobStatus.status === 'succeeded' || jobStatus.status === 'failed') {
          console.log(`Job ${jobId} completed with status: ${jobStatus.status}`)
          setJobPolling(false)
          clearInterval(pollingIntervalRef.current)
          pollingIntervalRef.current = null
          
          // Log completion details
          if (jobStatus.status === 'failed') {
            console.error(`Job ${jobId} failed with error: ${jobStatus.error}`)
          } else {
            console.log(`Job ${jobId} completed successfully`)
          }
          
          // Re-fetch history after job completion
          try {
            const historyData = await fetchCommands(20)
            setHistory(historyData)
            console.log('History refreshed after job completion')
          } catch (historyError) {
            console.error('Failed to refresh history after job completion:', historyError)
          }
        }
        
      } catch (error) {
        console.error('Error polling job status:', error)
        // Stop polling on error
        setJobPolling(false)
        clearInterval(pollingIntervalRef.current)
        pollingIntervalRef.current = null
      }
    }, 600) // Poll every 600ms
  }

  return (
    <div style={{ 
      padding: '20px', 
      margin: '0',
      fontFamily: 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
      lineHeight: '1.6',
      backgroundColor: '#f8f9fa',
      minHeight: '100vh',
      width: '100vw',
      boxSizing: 'border-box'
    }}>
      <div style={{
        maxWidth: '1200px',
        margin: '0 auto',
        padding: '0 20px'
      }}>
      {/* Page Header */}
      <div style={{
        textAlign: 'center',
        marginBottom: '32px',
        backgroundColor: 'white',
        padding: '24px',
        borderRadius: '12px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        border: '1px solid #e9ecef'
      }}>
        <h1 style={{ 
          margin: '0 0 8px 0',
          color: '#212529',
          fontSize: '2.25rem',
          fontWeight: '700',
          letterSpacing: '-0.025em'
        }}>
          Text-to-CAD Converter
        </h1>
        <p style={{
          margin: '0',
          color: '#6c757d',
          fontSize: '1.1rem',
          fontWeight: '400'
        }}>
          Convert natural language instructions into structured CAD commands
        </p>
      </div>

      {/* Instruction Section */}
      <div style={{
        backgroundColor: 'white',
        padding: '24px',
        borderRadius: '12px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        border: '1px solid #e9ecef',
        marginBottom: '20px'
      }}>
        <h2 style={{
          margin: '0 0 16px 0',
          color: '#212529',
          fontSize: '1.5rem',
          fontWeight: '600'
        }}>
          Instruction
        </h2>
        
        <div style={{ marginBottom: '16px' }}>
          <input
            id="instruction"
            type="text"
            value={instruction}
            onChange={(e) => setInstruction(e.target.value)}
            placeholder="e.g., Create a 5mm tall cylinder with 10mm diameter"
            style={{
              width: '100%',
              padding: '14px 16px',
              fontSize: '16px',
              border: '2px solid #e9ecef',
              borderRadius: '8px',
              outline: 'none',
              transition: 'border-color 0.2s ease',
              fontFamily: 'inherit',
              boxSizing: 'border-box'
            }}
            onFocus={(e) => e.target.style.borderColor = '#007bff'}
            onBlur={(e) => e.target.style.borderColor = '#e9ecef'}
          />
        </div>

        {/* Use AI Toggle */}
        <div style={{ marginBottom: '16px' }}>
          <label style={{
            display: 'flex',
            alignItems: 'center',
            fontSize: '0.95rem',
            fontWeight: '500',
            color: '#495057',
            cursor: 'pointer'
          }}>
            <input
              type="checkbox"
              checked={useAI}
              onChange={(e) => setUseAI(e.target.checked)}
              style={{
                marginRight: '8px',
                width: '16px',
                height: '16px',
                cursor: 'pointer'
              }}
            />
            Use AI Enhancement
            <span style={{
              marginLeft: '8px',
              fontSize: '0.8rem',
              color: '#6c757d',
              fontWeight: 'normal'
            }}>
              (requires OpenAI API key)
            </span>
          </label>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <button
            onClick={handleSend}
            disabled={loading || !instruction.trim() || (currentJob && currentJob.status === 'running')}
            style={{
              padding: '14px 28px',
              fontSize: '16px',
              fontWeight: '600',
              backgroundColor: (loading || (currentJob && currentJob.status === 'running')) ? '#6c757d' : '#007bff',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              cursor: (loading || (currentJob && currentJob.status === 'running')) ? 'not-allowed' : 'pointer',
              fontFamily: 'inherit',
              transition: 'all 0.2s ease',
              minWidth: '140px',
              boxShadow: '0 2px 4px rgba(0,123,255,0.3)'
            }}
            onMouseOver={(e) => {
              if (!loading && instruction.trim() && !(currentJob && currentJob.status === 'running')) {
                e.target.style.backgroundColor = '#0056b3'
                e.target.style.transform = 'translateY(-1px)'
              }
            }}
            onMouseOut={(e) => {
              if (!loading && instruction.trim() && !(currentJob && currentJob.status === 'running')) {
                e.target.style.backgroundColor = '#007bff'
                e.target.style.transform = 'translateY(0)'
              }
            }}
          >
            {loading ? 'Processing...' : 
             (currentJob && currentJob.status === 'running') ? 'Job Running...' : 
             'Send'}
          </button>
        </div>
      </div>

      {/* Job Status Section */}
      {currentJob && (
        <div style={{
          backgroundColor: 'white',
          padding: '24px',
          borderRadius: '12px',
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
          border: '1px solid #e9ecef',
          marginBottom: '20px'
        }}>
          <div style={{
            display: 'flex',
            alignItems: 'center',
            marginBottom: '16px',
            gap: '12px'
          }}>
            <h2 style={{
              fontSize: '1.5rem',
              margin: '0',
              color: '#212529',
              fontWeight: '600'
            }}>
              Job Status
            </h2>
            
            {/* Status Badge */}
            <span style={{
              padding: '4px 12px',
              borderRadius: '16px',
              fontSize: '0.8rem',
              fontWeight: '600',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              backgroundColor: 
                currentJob.status === 'succeeded' ? '#d4edda' :
                currentJob.status === 'failed' ? '#f8d7da' :
                currentJob.status === 'running' ? '#d1ecf1' : '#e2e3e5',
              color:
                currentJob.status === 'succeeded' ? '#155724' :
                currentJob.status === 'failed' ? '#721c24' :
                currentJob.status === 'running' ? '#0c5460' : '#495057',
              border: `1px solid ${
                currentJob.status === 'succeeded' ? '#c3e6cb' :
                currentJob.status === 'failed' ? '#f5c6cb' :
                currentJob.status === 'running' ? '#bee5eb' : '#d6d8db'
              }`
            }}>
              {currentJob.status === 'succeeded' ? 'Succeeded' :
               currentJob.status === 'failed' ? 'Failed' :
               currentJob.status === 'running' ? 'Running' : 'Queued'}
            </span>
          </div>
          
          <div style={{ marginBottom: '12px' }}>
            <div style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '8px'
            }}>
              <span style={{
                fontSize: '0.9rem',
                color: '#495057',
                fontWeight: '500'
              }}>
                Status: <span style={{
                  color: currentJob.status === 'succeeded' ? '#28a745' : 
                        currentJob.status === 'failed' ? '#dc3545' :
                        currentJob.status === 'running' ? '#007bff' : '#6c757d'
                }}>
                  {currentJob.status}
                </span>
              </span>
              <span style={{
                fontSize: '0.9rem',
                color: '#495057',
                fontWeight: '500'
              }}>
                {currentJob.progress}%
              </span>
            </div>
            
            {/* Progress Bar */}
            <div style={{
              width: '100%',
              height: '8px',
              backgroundColor: '#e9ecef',
              borderRadius: '4px',
              overflow: 'hidden'
            }}>
              <div style={{
                width: `${currentJob.progress}%`,
                height: '100%',
                backgroundColor: currentJob.status === 'failed' ? '#dc3545' : '#007bff',
                transition: 'width 0.3s ease',
                borderRadius: '4px'
              }} />
            </div>
          </div>
          
          {/* Job ID and timestamps */}
          <div style={{
            fontSize: '0.8rem',
            color: '#6c757d',
            display: 'flex',
            flexDirection: 'column',
            gap: '4px'
          }}>
            <div>Job ID: {currentJob.job_id}</div>
            {currentJob.created_at && (
              <div>Started: {new Date(currentJob.created_at).toLocaleString()}</div>
            )}
            {currentJob.error && (
              <div style={{ color: '#dc3545', fontWeight: '500' }}>
                Error: {currentJob.error}
              </div>
            )}
            {jobPolling && (
              <div style={{ color: '#007bff', fontWeight: '500' }}>
                Polling for updates...
              </div>
            )}
            
            {/* Error Message Display */}
            {currentJob.status === 'failed' && currentJob.error && (
              <div style={{
                marginTop: '12px',
                padding: '12px',
                backgroundColor: '#f8d7da',
                border: '1px solid #f5c6cb',
                borderRadius: '6px',
                color: '#721c24'
              }}>
                <div style={{
                  fontWeight: '600',
                  marginBottom: '4px',
                  fontSize: '0.9rem'
                }}>
                  Job Failed
                </div>
                <div style={{
                  fontSize: '0.85rem',
                  fontFamily: 'Monaco, Consolas, monospace',
                  wordBreak: 'break-word'
                }}>
                  {currentJob.error}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Result Section */}
      {response && (
        <div style={{
          backgroundColor: 'white',
          padding: '24px',
          borderRadius: '12px',
          boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
          border: '1px solid #e9ecef',
          marginBottom: '20px'
        }}>
          <h2 style={{ 
            margin: '0 0 16px 0',
            color: '#212529',
            fontSize: '1.5rem',
            fontWeight: '600'
          }}>
            Result
          </h2>
          
          {/* Source Display */}
          <div style={{ marginBottom: '20px' }}>
            <div style={{
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}>
              <span style={{
                fontSize: '0.95rem',
                fontWeight: '500',
                color: '#495057'
              }}>
                Processing Method:
              </span>
              <span style={{
                backgroundColor: response.source === 'ai' ? '#d4edda' : '#fff3cd',
                color: response.source === 'ai' ? '#155724' : '#856404',
                padding: '6px 12px',
                borderRadius: '20px',
                fontSize: '0.85rem',
                fontWeight: '600',
                border: `1px solid ${response.source === 'ai' ? '#c3e6cb' : '#ffeaa7'}`
              }}>
                {response.source === 'ai' ? 'AI Enhanced' : 'Rule-based'}
              </span>
            </div>
            <div style={{
              marginTop: '8px',
              fontSize: '0.9rem',
              color: '#6c757d'
            }}>
              Schema Version: {response.schema_version || 'N/A'}
            </div>
          </div>

          {/* Plan Section */}
          {response.plan && response.plan.length > 0 && (
            <div style={{ marginBottom: '20px' }}>
              <h3 style={{
                fontSize: '1.1rem',
                marginBottom: '12px',
                color: '#495057',
                fontWeight: '600'
              }}>
                Plan
              </h3>
              <ol style={{
                margin: 0,
                paddingLeft: '20px',
                color: '#495057',
                backgroundColor: '#f8f9fa',
                border: '1px solid #e9ecef',
                borderRadius: '10px',
                padding: '12px 16px',
                boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
              }}>
                {response.plan.map((step, index) => (
                  <li key={`${index}-${step}`} style={{ marginBottom: '6px' }}>
                    {step}
                  </li>
                ))}
              </ol>
            </div>
          )}

          {/* Parsed Parameters Table */}
          <div style={{ marginBottom: '20px' }}>
            <h3 style={{
              fontSize: '1.1rem',
              marginBottom: '12px',
              color: '#495057',
              fontWeight: '600'
            }}>
              Parsed Parameters
            </h3>
            <div style={{
              backgroundColor: '#f8f9fa',
              border: '1px solid #e9ecef',
              borderRadius: '10px',
              overflow: 'hidden',
              boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
            }}>
              <table style={{
                width: '100%',
                borderCollapse: 'collapse',
                fontSize: '14px'
              }}>
                <tbody>
                  <tr style={{ backgroundColor: '#e9ecef' }}>
                    <td style={{
                      padding: '12px 16px',
                      fontWeight: '600',
                      color: '#495057',
                      borderBottom: '1px solid #dee2e6'
                    }}>
                      Action
                    </td>
                    <td style={{
                      padding: '12px 16px',
                      color: '#495057',
                      borderBottom: '1px solid #dee2e6'
                    }}>
                      {response.parsed_parameters?.action || 'N/A'}
                    </td>
                  </tr>
                  {response.parsed_parameters?.parameters && Object.entries(response.parsed_parameters.parameters).map(([key, value]) => (
                    <tr key={key}>
                      <td style={{
                        padding: '12px 16px',
                        fontWeight: '500',
                        color: '#6c757d',
                        borderBottom: '1px solid #dee2e6',
                        textTransform: 'capitalize'
                      }}>
                        {key.replace('_', ' ')}
                      </td>
                      <td style={{
                        padding: '12px 16px',
                        color: '#495057',
                        borderBottom: '1px solid #dee2e6',
                        fontFamily: value && typeof value === 'object' ? 'Monaco, Consolas, monospace' : 'inherit'
                      }}>
                        {value === null ? (
                          <span style={{ color: '#6c757d', fontStyle: 'italic' }}>null</span>
                        ) : typeof value === 'object' ? (
                          JSON.stringify(value, null, 2)
                        ) : (
                          String(value)
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Operations Section */}
          {Array.isArray(response.operations) && response.operations.length > 1 && (
            <div style={{ marginBottom: '20px' }}>
              <h3 style={{
                fontSize: '1.1rem',
                marginBottom: '12px',
                color: '#495057',
                fontWeight: '600'
              }}>
                Operations
              </h3>
              <ul style={{
                margin: 0,
                paddingLeft: '20px',
                color: '#495057',
                backgroundColor: '#f8f9fa',
                border: '1px solid #e9ecef',
                borderRadius: '10px',
                padding: '12px 16px',
                boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
              }}>
                {response.operations.map((op, index) => (
                  <li key={`${index}-${op?.action || 'op'}`} style={{ marginBottom: '6px' }}>
                    <strong>{op?.action || 'unknown'}</strong> {op?.parameters?.shape ? `- ${op.parameters.shape}` : ''}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Raw JSON (collapsible) */}
          <details style={{ marginTop: '20px' }}>
            <summary style={{
              cursor: 'pointer',
              fontSize: '0.9rem',
              color: '#6c757d',
              fontWeight: '600',
              marginBottom: '8px',
              padding: '8px 12px',
              backgroundColor: '#f8f9fa',
              borderRadius: '6px',
              border: '1px solid #e9ecef'
            }}>
              View Raw JSON Response
            </summary>
            <pre
              style={{
                backgroundColor: '#1a1a1a',
                color: '#f8f8f2',
                padding: '20px',
                borderRadius: '8px',
                overflow: 'auto',
                border: '1px solid #e9ecef',
                fontSize: '13px',
                lineHeight: '1.5',
                fontFamily: 'Monaco, Consolas, "Courier New", monospace',
                maxHeight: '300px',
                marginTop: '12px',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
              }}
            >
              {JSON.stringify(response, null, 2)}
            </pre>
          </details>
        </div>
      )}

      {/* History Section */}
      <div style={{
        backgroundColor: 'white',
        padding: '24px',
        borderRadius: '12px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        border: '1px solid #e9ecef'
      }}>
        <h2 style={{ 
          margin: '0 0 16px 0',
          color: '#212529',
          fontSize: '1.5rem',
          fontWeight: '600'
        }}>
          History
        </h2>
        {history.length === 0 ? (
          <div style={{
            textAlign: 'center',
            padding: '40px 20px',
            color: '#6c757d'
          }}>
            <div style={{
              fontSize: '3rem',
              marginBottom: '16px',
              opacity: 0.3
            }}>
              No history yet
            </div>
            <p style={{ 
              fontSize: '1.1rem',
              fontStyle: 'italic',
              margin: '0'
            }}>
              No commands yet. Send your first instruction above!
            </p>
          </div>
        ) : (
          <div style={{
            maxHeight: '400px',
            overflowY: 'auto',
            paddingRight: '8px'
          }}>
            {history.map((command) => (
              <div
                key={command.id}
                style={{
                  padding: '16px',
                  marginBottom: '12px',
                  backgroundColor: '#f8f9fa',
                  border: '1px solid #e9ecef',
                  borderRadius: '10px',
                  fontSize: '14px',
                  lineHeight: '1.5',
                  position: 'relative',
                  boxShadow: '0 1px 3px rgba(0,0,0,0.1)'
                }}
              >
                <div style={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  marginBottom: '12px',
                  flexWrap: 'wrap',
                  gap: '8px'
                }}>
                  <span style={{ 
                    fontWeight: '700',
                    color: '#007bff',
                    fontSize: '13px',
                    backgroundColor: 'white',
                    padding: '4px 8px',
                    borderRadius: '6px',
                    border: '1px solid #007bff'
                  }}>
                    #{command.id}
                  </span>
                  <span style={{ 
                    backgroundColor: '#e7f3ff',
                    color: '#0056b3',
                    padding: '4px 12px',
                    borderRadius: '16px',
                    fontSize: '12px',
                    fontWeight: '600',
                    border: '1px solid #bee5eb'
                  }}>
                    {command.action}
                  </span>
                  <span style={{ 
                    color: '#6c757d',
                    fontSize: '11px',
                    marginLeft: 'auto',
                    fontWeight: '500'
                  }}>
                    {new Date(command.created_at).toLocaleString()}
                  </span>
                </div>
                <div style={{ 
                  color: '#495057',
                  fontWeight: '500',
                  backgroundColor: 'white',
                  padding: '12px',
                  borderRadius: '6px',
                  border: '1px solid #e9ecef'
                }}>
                  {command.prompt}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
      </div>
    </div>
  )
}

export default App
