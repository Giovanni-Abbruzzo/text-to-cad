import { useState } from 'react'

function App() {
  const [instruction, setInstruction] = useState('Add 4 holes on the top face')
  const [response, setResponse] = useState(null)
  const [loading, setLoading] = useState(false)

  const handleSend = async () => {
    setLoading(true)
    setResponse(null)

    // Debug: Log the environment variable
    console.log('VITE_API_BASE:', import.meta.env.VITE_API_BASE)
    const apiBase = import.meta.env.VITE_API_BASE || 'http://localhost:8000'
    console.log('Using API base:', apiBase)

    try {
      const res = await fetch(`${apiBase}/process_instruction`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ instruction }),
      })

      if (!res.ok) {
        throw new Error(`HTTP error! status: ${res.status}`)
      }

      const data = await res.json()
      setResponse(data)
    } catch (error) {
      console.error('Error:', error)
      alert(`Failed to process instruction: ${error.message}`)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      <h1>Text-to-CAD</h1>
      
      <div style={{ marginBottom: '20px' }}>
        <label htmlFor="instruction" style={{ display: 'block', marginBottom: '8px' }}>
          Instruction:
        </label>
        <input
          id="instruction"
          type="text"
          value={instruction}
          onChange={(e) => setInstruction(e.target.value)}
          style={{
            width: '100%',
            padding: '8px',
            fontSize: '16px',
            border: '1px solid #ccc',
            borderRadius: '4px',
          }}
        />
      </div>

      <button
        onClick={handleSend}
        disabled={loading || !instruction.trim()}
        style={{
          padding: '10px 20px',
          fontSize: '16px',
          backgroundColor: loading ? '#ccc' : '#007bff',
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          cursor: loading ? 'not-allowed' : 'pointer',
          marginBottom: '20px',
        }}
      >
        {loading ? 'Processing...' : 'Send'}
      </button>

      {response && (
        <div>
          <h3>Response:</h3>
          <pre
            style={{
              backgroundColor: '#f5f5f5',
              padding: '15px',
              borderRadius: '4px',
              overflow: 'auto',
              border: '1px solid #ddd',
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
