import { useState } from 'react'
import { processInstruction } from './api.js'

function App() {
  const [instruction, setInstruction] = useState('')
  const [response, setResponse] = useState(null)
  const [loading, setLoading] = useState(false)

  const handleSend = async () => {
    setLoading(true)
    setResponse(null)

    try {
      const data = await processInstruction(instruction)
      setResponse(data)
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
        <div>
          <h3 style={{ 
            fontSize: '1.25rem',
            marginBottom: '16px',
            color: '#495057',
            fontWeight: '500'
          }}>
            Response:
          </h3>
          <pre
            style={{
              backgroundColor: '#000000',
              padding: '20px',
              borderRadius: '8px',
              overflow: 'auto',
              border: '1px solid #e9ecef',
              fontSize: '14px',
              lineHeight: '1.4',
              fontFamily: 'Monaco, Consolas, "Courier New", monospace',
              maxHeight: '400px'
            }}
          >
            {JSON.stringify(response, null, 2)}
          </pre>
        </div>
      )}
    </div>
  )
}

export default App
