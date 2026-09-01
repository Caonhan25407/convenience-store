const API_URL = '/api/health'

export async function getHealth() {
  const response = await fetch(API_URL)

  if (!response.ok) {
    throw new Error('Không thể kết nối backend')
  }

  return response.json()
}
