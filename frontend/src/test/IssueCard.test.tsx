import React from 'react';
import { render, screen } from '@testing-library/react';
import { IssueCard } from '../features/board/IssueCard';
import { IssueDto } from '../api/types';
import { describe, it, expect, vi } from 'vitest';

// Mock dnd-kit sortable hook
vi.mock('@dnd-kit/sortable', () => ({
  useSortable: () => ({
    attributes: {},
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  }),
}));

describe('IssueCard Component', () => {
  const mockIssue: IssueDto = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    projectId: '123e4567-e89b-12d3-a456-426614174001',
    projectKey: 'BUG',
    issueKey: 'BUG-42',
    number: 42,
    title: 'Fix responsive navigation bar overflow',
    type: 'Bug',
    priority: 'High',
    status: 'InProgress',
    boardOrder: 1,
    assignee: {
      id: 'user-1',
      name: 'Alan Turing',
      email: 'alan@enigma.org',
      createdAt: '2026-01-01T00:00:00Z',
    },
    reporter: {
      id: 'user-2',
      name: 'Grace Hopper',
      email: 'grace@navy.mil',
      createdAt: '2026-01-01T00:00:00Z',
    },
    labels: [
      { id: 'lbl-1', projectId: 'prj-1', name: 'UI', color: '#6366F1' },
      { id: 'lbl-2', projectId: 'prj-1', name: 'urgent', color: '#EF4444' },
    ],
    createdAt: '2026-11-01T12:00:00Z',
    updatedAt: '2026-11-01T12:00:00Z',
    commentsCount: 3,
  };

  it('renders issue key, title, priority, and labels', () => {
    const handleClick = vi.fn();
    render(<IssueCard issue={mockIssue} onClick={handleClick} />);

    expect(screen.getByText('BUG-42')).toBeInTheDocument();
    expect(screen.getByText('Fix responsive navigation bar overflow')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
    expect(screen.getByText('UI')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument(); // comments count
  });

  it('renders unassigned indicator when no assignee exists', () => {
    const unassignedIssue = { ...mockIssue, assignee: undefined };
    render(<IssueCard issue={unassignedIssue} onClick={vi.fn()} />);

    expect(screen.getByTitle('Unassigned')).toBeInTheDocument();
  });
});
