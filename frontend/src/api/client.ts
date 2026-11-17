import {
  AuthResponse,
  CommentDto,
  CreateIssueRequest,
  CreateProjectRequest,
  CreateSprintRequest,
  CompleteSprintRequest,
  IssueDto,
  LabelDto,
  MoveIssueRequest,
  PagedResult,
  ProjectDto,
  ProjectMemberDto,
  SprintDto,
  BurndownPointDto,
  UpdateIssueRequest,
  UserDto,
} from './types';

export interface IssueFilterParams {
  status?: string;
  assigneeId?: string;
  labelId?: string;
  sprintId?: string;
  type?: string;
  priority?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

const API_BASE = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '') + '/api';

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('bugbox_token');
  const headers = new Headers(options.headers || {});

  if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    localStorage.removeItem('bugbox_token');
    localStorage.removeItem('bugbox_user');
    if (window.location.pathname !== '/login') {
      window.location.href = '/login';
    }
    throw new ApiError(401, 'Session expired. Please log in again.');
  }

  if (response.status === 204) {
    return {} as T;
  }

  const contentType = response.headers.get('content-type');
  const isJson = contentType && contentType.includes('application/json');
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    const message = data?.detail || data?.title || response.statusText || 'Request failed';
    const errors = data?.errors;
    throw new ApiError(response.status, message, errors);
  }

  return data as T;
}

export const api = {
  // Auth
  register: (data: { name: string; email: string; password: string }) =>
    request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify(data) }),

  login: (data: { email: string; password: string }) =>
    request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify(data) }),

  getMe: () => request<UserDto>('/auth/me'),

  // Projects
  getProjects: () => request<ProjectDto[]>('/projects'),

  getProject: (id: string) => request<ProjectDto>(`/projects/${id}`),

  createProject: (data: CreateProjectRequest) =>
    request<ProjectDto>('/projects', { method: 'POST', body: JSON.stringify(data) }),

  updateProject: (id: string, data: { name: string; description?: string }) =>
    request<ProjectDto>(`/projects/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteProject: (id: string) =>
    request<void>(`/projects/${id}`, { method: 'DELETE' }),

  getMembers: (projectId: string) =>
    request<ProjectMemberDto[]>(`/projects/${projectId}/members`),

  addMember: (projectId: string, data: { email: string; role: string }) =>
    request<ProjectMemberDto>(`/projects/${projectId}/members`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  removeMember: (projectId: string, userId: string) =>
    request<void>(`/projects/${projectId}/members/${userId}`, { method: 'DELETE' }),

  // Issues
  getProjectIssues: (projectId: string, params: IssueFilterParams = {}) => {
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        query.append(key, String(value));
      }
    });
    const qs = query.toString() ? `?${query.toString()}` : '';
    return request<PagedResult<IssueDto>>(`/projects/${projectId}/issues${qs}`);
  },

  getIssue: (id: string) => request<IssueDto>(`/issues/${id}`),

  createIssue: (projectId: string, data: CreateIssueRequest) =>
    request<IssueDto>(`/projects/${projectId}/issues`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  updateIssue: (id: string, data: UpdateIssueRequest) =>
    request<IssueDto>(`/issues/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteIssue: (id: string) => request<void>(`/issues/${id}`, { method: 'DELETE' }),

  updateIssueStatus: (id: string, status: string) =>
    request<IssueDto>(`/issues/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),

  moveIssue: (id: string, data: MoveIssueRequest) =>
    request<IssueDto>(`/issues/${id}/move`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),

  addLabelToIssue: (issueId: string, labelId: string) =>
    request<IssueDto>(`/issues/${issueId}/labels/${labelId}`, { method: 'POST' }),

  removeLabelFromIssue: (issueId: string, labelId: string) =>
    request<IssueDto>(`/issues/${issueId}/labels/${labelId}`, { method: 'DELETE' }),

  // Sprints
  getProjectSprints: (projectId: string) =>
    request<SprintDto[]>(`/projects/${projectId}/sprints`),

  getSprint: (id: string) => request<SprintDto>(`/sprints/${id}`),

  createSprint: (projectId: string, data: CreateSprintRequest) =>
    request<SprintDto>(`/projects/${projectId}/sprints`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  updateSprint: (id: string, data: CreateSprintRequest) =>
    request<SprintDto>(`/sprints/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteSprint: (id: string) => request<void>(`/sprints/${id}`, { method: 'DELETE' }),

  startSprint: (id: string) => request<SprintDto>(`/sprints/${id}/start`, { method: 'POST' }),

  completeSprint: (id: string, data: CompleteSprintRequest) =>
    request<SprintDto>(`/sprints/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  addIssuesToSprint: (sprintId: string, issueIds: string[]) =>
    request<void>(`/sprints/${sprintId}/issues`, {
      method: 'POST',
      body: JSON.stringify({ issueIds }),
    }),

  removeIssueFromSprint: (sprintId: string, issueId: string) =>
    request<void>(`/sprints/${sprintId}/issues/${issueId}`, { method: 'DELETE' }),

  getBurndown: (sprintId: string) =>
    request<BurndownPointDto[]>(`/sprints/${sprintId}/burndown`),

  // Labels
  getLabels: (projectId: string) =>
    request<LabelDto[]>(`/projects/${projectId}/labels`),

  createLabel: (projectId: string, data: { name: string; color: string }) =>
    request<LabelDto>(`/projects/${projectId}/labels`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  updateLabel: (id: string, data: { name: string; color: string }) =>
    request<LabelDto>(`/labels/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteLabel: (id: string) => request<void>(`/labels/${id}`, { method: 'DELETE' }),

  // Comments
  getComments: (issueId: string) =>
    request<CommentDto[]>(`/issues/${issueId}/comments`),

  addComment: (issueId: string, body: string) =>
    request<CommentDto>(`/issues/${issueId}/comments`, {
      method: 'POST',
      body: JSON.stringify({ body }),
    }),

  deleteComment: (id: string) => request<void>(`/comments/${id}`, { method: 'DELETE' }),

  // Demo
  seedDemo: () => request<ProjectDto>('/demo/seed', { method: 'POST' }),
};
