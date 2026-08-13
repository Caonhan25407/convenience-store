const API_BASE_URL = 'http://localhost:5000'

export async function getHealth() {
  const response = await fetch(`${API_BASE_URL}/api/health`)

  if (!response.ok) {
    throw new Error('Không thể kết nối backend')
  }

  return response.json()
}