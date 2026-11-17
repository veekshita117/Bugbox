export type IssueStatus = 'ToDo' | 'InProgress' | 'InReview' | 'Done';
export type IssuePriority = 'Low' | 'Medium' | 'High';
export type IssueType = 'Bug' | 'Feature' | 'Task';
export type ProjectRole = 'Admin' | 'Member';
export type SprintStatus = 'Planned' | 'Active' | 'Completed';

export interface UserDto {
  id: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: UserDto;
}

export interface ProjectDto {
  id: string;
  name: string;
  key: string;
  description?: string;
  ownerId: string;
  createdAt: string;
  currentUserRole?: ProjectRole;
}

export interface ProjectMemberDto {
  userId: string;
  name: string;
  email: string;
  role: ProjectRole;
}

export interface LabelDto {
  id: string;
  projectId: string;
  name: string;
  color: string;
}

export interface CommentDto {
  id: string;
  issueId: string;
  author: UserDto;
  body: string;
  createdAt: string;
}

export interface IssueDto {
  id: string;
  projectId: string;
  projectKey: string;
  issueKey: string;
  number: number;
  title: string;
  description?: string;
  type: IssueType;
  priority: IssuePriority;
  status: IssueStatus;
  boardOrder: number;
  assignee?: UserDto;
  reporter: UserDto;
  sprintId?: string;
  labels: LabelDto[];
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
  commentsCount: number;
}

export interface SprintDto {
  id: string;
  projectId: string;
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  status: SprintStatus;
  totalIssuesCount: number;
  completedIssuesCount: number;
}

export interface BurndownPointDto {
  date: string;
  remaining: number;
  ideal: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateProjectRequest {
  name: string;
  key: string;
  description?: string;
}

export interface CreateIssueRequest {
  title: string;
  description?: string;
  type: IssueType;
  priority: IssuePriority;
  status?: IssueStatus;
  assigneeId?: string;
  sprintId?: string;
  labelIds?: string[];
}

export interface UpdateIssueRequest {
  title: string;
  description?: string;
  type: IssueType;
  priority: IssuePriority;
  status: IssueStatus;
  assigneeId?: string;
  sprintId?: string;
  labelIds?: string[];
}

export interface MoveIssueRequest {
  status: IssueStatus;
  boardOrder: number;
}

export interface CreateSprintRequest {
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
}

export interface CompleteSprintRequest {
  targetSprintId?: string;
}
