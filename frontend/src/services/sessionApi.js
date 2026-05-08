import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
})

const sessionApi = {
  // Sessions
  createSession: (data) => api.post('/sessions', data),
  getSession: (code) => api.get(`/sessions/${code}`),
  getSessionState: (code) => api.get(`/sessions/${code}/state`),
  updateSessionStatus: (code, status) => api.put(`/sessions/${code}/status`, { status }),
  deleteSession: (code) => api.delete(`/sessions/${code}`),

  // Rounds
  startRound: (code) => api.post(`/sessions/${code}/rounds/start`),
  pauseRound: (code) => api.post(`/sessions/${code}/rounds/pause`),
  nextRound: (code) => api.post(`/sessions/${code}/rounds/next`),
  adjustTime: (code, minutes) => api.put(`/sessions/${code}/rounds/time`, { minutes }),

  // Teams
  registerTeam: (code, data) => api.post(`/sessions/${code}/teams`, data),
  getTeams: (code) => api.get(`/sessions/${code}/teams`),
  getTeam: (code, teamId) => api.get(`/sessions/${code}/teams/${teamId}`),
  updateTeam: (code, teamId, data) => api.put(`/sessions/${code}/teams/${teamId}`, data),
  selectTrack: (code, teamId, track) => api.put(`/sessions/${code}/teams/${teamId}/track`, JSON.stringify(track), { headers: { 'Content-Type': 'application/json' } }),
  updateTokens: (code, teamId, tokens) => api.put(`/sessions/${code}/teams/${teamId}/tokens`, JSON.stringify(tokens), { headers: { 'Content-Type': 'application/json' } }),
  deleteTeam: (code, teamId) => api.delete(`/sessions/${code}/teams/${teamId}`),
}

export default sessionApi
