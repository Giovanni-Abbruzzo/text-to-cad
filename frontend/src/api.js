export const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:8000";

export async function processInstruction(text) { 
  const r = await fetch(`${API_BASE}/process_instruction`, {
    method: "POST",
    headers: {"Content-Type":"application/json"},
    body: JSON.stringify({ instruction: text })
  });
  if (!r.ok) throw new Error(`API error ${r.status}`);
  return r.json();
}
