import { useState, useEffect } from 'react'
import { processInstruction, fetchCommands } from './api.js'

function App() {
  const [instruction, setInstruction] = useState('')
  const [response, setResponse] = useState(null)
  const [loading, setLoading] = useState(false)
  const [history, setHistory] = useState([])
  const [useAI, setUseAI] = useState(false)

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

  const handleSend = async () => {
    setLoading(true)
    setResponse(null)

    try {
      const data = await processInstruction(instruction, useAI)
      setResponse(data)
      
      // Re-fetch history after successful instruction processing
      try {
        const historyData = await fetchCommands(20)
        setHistory(historyData)
      } catch (historyError) {
        console.error('Failed to refresh history:', historyError)
      }
    } catch (error) {
      console.error('Error:', error)
      alert(`Failed to process instruction: ${error.message}`)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ 
      padding: '40px 20px', 
      maxWidth: '800px', 
      margin: '0 auto',
      fontFamily: 'system-ui, -apple-system, sans-serif',
      lineHeight: '1.6'
    }}>
      <div style={{ textAlign: 'center', marginBottom: '40px' }}>
        <h1 style={{ 
          fontSize: '2.5rem', 
          margin: '0 0 16px 0',
          color: '#2c3e50',
          fontWeight: '600'
        }}>
          Text-to-CAD
        </h1>
        <p style={{ 
          fontSize: '1.1rem',
          color: '#6c757d',
          margin: '0',
          maxWidth: '600px',
          marginLeft: 'auto',
          marginRight: 'auto'
        }}>
          Type a natural-language CAD instruction; the API returns structured JSON
        </p>
      </div>
      
      <div style={{ marginBottom: '24px' }}>
        <label htmlFor="instruction" style={{ 
          display: 'block', 
          marginBottom: '12px',
          fontSize: '1rem',
          fontWeight: '500',
          color: '#495057'
        }}>
          Instruction:
        </label>
        <input
          id="instruction"
          type="text"
          value={instruction}
          onChange={(e) => setInstruction(e.target.value)}
          placeholder="Extrude a 5mm tall cylinder with 10mm diameter" // type hint
          style={{
            width: '100%',
            padding: '12px 16px',
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
      <div style={{ marginBottom: '24px' }}>
        <label style={{
          display: 'flex',
          alignItems: 'center',
          fontSize: '1rem',
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
              width: '18px',
              height: '18px',
              cursor: 'pointer'
            }}
          />
          Use AI
          <span style={{
            marginLeft: '8px',
            fontSize: '0.875rem',
            color: '#6c757d',
            fontWeight: 'normal'
          }}>
            (requires OpenAI API key)
          </span>
        </label>
      </div>

      <button
        onClick={handleSend}
        disabled={loading || !instruction.trim()}
        style={{
          padding: '12px 24px',
          fontSize: '16px',
          fontWeight: '500',
          backgroundColor: loading ? '#6c757d' : '#007bff',
          color: 'white',
          border: 'none',
          borderRadius: '8px',
          cursor: loading ? 'not-allowed' : 'pointer',
          marginBottom: '32px',
          fontFamily: 'inherit',
          transition: 'background-color 0.2s ease',
          minWidth: '120px'
        }}
        onMouseOver={(e) => {
          if (!loading && instruction.trim()) {
            e.target.style.backgroundColor = '#0056b3'
          }
        }}
        onMouseOut={(e) => {
          if (!loading && instruction.trim()) {
            e.target.style.backgroundColor = '#007bff'
          }
        }}
      >
        {loading ? 'Processing...' : 'Send'}
      </button>

      {response && (
        <div style={{ marginBottom: '40px' }}>
          <h3 style={{ 
            fontSize: '1.25rem',
            marginBottom: '16px',
            color: '#495057',
            fontWeight: '500'
          }}>
            Response:
          </h3>
          
          {/* Source Display */}
          <div style={{ marginBottom: '16px' }}>
            <span style={{
              fontSize: '1rem',
              fontWeight: '500',
              color: '#495057'
            }}>
              Source: 
            </span>
            <span style={{
              backgroundColor: response.source === 'ai' ? '#d4edda' : '#fff3cd',
              color: response.source === 'ai' ? '#155724' : '#856404',
              padding: '4px 12px',
              borderRadius: '16px',
              fontSize: '0.875rem',
              fontWeight: '500',
              marginLeft: '8px'
            }}>
              {response.source === 'ai' ? '🤖 AI' : '📋 Rule-based'}
            </span>
          </div>

          {/* Parsed Parameters Table */}
          <div style={{ marginBottom: '20px' }}>
            <h4 style={{
              fontSize: '1rem',
              marginBottom: '12px',
              color: '#495057',
              fontWeight: '500'
            }}>
              Parsed Parameters:
            </h4>
            <div style={{
              backgroundColor: '#f8f9fa',
              border: '1px solid #e9ecef',
              borderRadius: '8px',
              overflow: 'hidden'
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

          {/* Raw JSON (collapsible) */}
          <details style={{ marginTop: '16px' }}>
            <summary style={{
              cursor: 'pointer',
              fontSize: '0.875rem',
              color: '#6c757d',
              fontWeight: '500',
              marginBottom: '8px'
            }}>
              View Raw JSON
            </summary>
            <pre
              style={{
                backgroundColor: '#000000',
                color: '#ffffff',
                padding: '16px',
                borderRadius: '6px',
                overflow: 'auto',
                border: '1px solid #e9ecef',
                fontSize: '12px',
                lineHeight: '1.4',
                fontFamily: 'Monaco, Consolas, "Courier New", monospace',
                maxHeight: '300px',
                marginTop: '8px'
              }}
            >
              {JSON.stringify(response, null, 2)}
            </pre>
          </details>
        </div>
      )}

      {/* History Section */}
      <div>
        <h3 style={{ 
          fontSize: '1.25rem',
          marginBottom: '16px',
          color: '#495057',
          fontWeight: '500'
        }}>
          Command History:
        </h3>
        {history.length === 0 ? (
          <p style={{ 
            color: '#6c757d',
            fontStyle: 'italic',
            margin: '0'
          }}>
            No commands yet. Send your first instruction above!
          </p>
        ) : (
          <div style={{
            backgroundColor: '#f8f9fa',
            border: '1px solid #e9ecef',
            borderRadius: '8px',
            padding: '16px',
            maxHeight: '400px',
            overflowY: 'auto'
          }}>
            {history.map((command) => (
              <div
                key={command.id}
                style={{
                  padding: '12px',
                  marginBottom: '8px',
                  backgroundColor: 'white',
                  border: '1px solid #e9ecef',
                  borderRadius: '6px',
                  fontSize: '14px',
                  lineHeight: '1.4'
                }}
              >
                <div style={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  marginBottom: '8px',
                  flexWrap: 'wrap',
                  gap: '8px'
                }}>
                  <span style={{ 
                    fontWeight: 'bold',
                    color: '#007bff',
                    fontSize: '12px'
                  }}>
                    #{command.id}
                  </span>
                  <span style={{ 
                    backgroundColor: '#e7f3ff',
                    color: '#0056b3',
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '12px',
                    fontWeight: '500'
                  }}>
                    {command.action}
                  </span>
                  <span style={{ 
                    color: '#6c757d',
                    fontSize: '12px',
                    marginLeft: 'auto'
                  }}>
                    {new Date(command.created_at).toLocaleString()}
                  </span>
                </div>
                <div style={{ 
                  color: '#495057',
                  fontWeight: '500'
                }}>
                  {command.prompt}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default App
