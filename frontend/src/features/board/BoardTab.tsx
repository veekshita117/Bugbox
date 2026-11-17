import React, { useState, useMemo } from 'react';
import { useOutletContext, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  DndContext,
  DragOverlay,
  closestCorners,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragStartEvent,
  DragEndEvent,
} from '@dnd-kit/core';
import { sortableKeyboardCoordinates } from '@dnd-kit/sortable';
import { api } from '../../api/client';
import { IssueDto, IssueStatus, ProjectDto } from '../../api/types';
import { KanbanColumn } from './KanbanColumn';
import { IssueCard } from './IssueCard';
import { IssueDetailModal } from '../issues/IssueDetailModal';
import { useToast } from '../../context/ToastContext';
import { Filter, Search, X, Sparkles } from 'lucide-react';
import { Skeleton } from '../../components/common/Skeleton';

const COLUMNS: { id: IssueStatus; title: string }[] = [
  { id: 'ToDo', title: 'To Do' },
  { id: 'InProgress', title: 'In Progress' },
  { id: 'InReview', title: 'In Review' },
  { id: 'Done', title: 'Done' },
];

export const BoardTab: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { project } = useOutletContext<{ project: ProjectDto }>();
  const queryClient = useQueryClient();
  const { showError } = useToast();

  const [selectedIssue, setSelectedIssue] = useState<IssueDto | null>(null);
  const [activeIssue, setActiveIssue] = useState<IssueDto | null>(null);

  // Filters state
  const [search, setSearch] = useState('');
  const [assigneeId, setAssigneeId] = useState<string>('');
  const [labelId, setLabelId] = useState<string>('');

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Sprints query
  const { data: sprints } = useQuery({
    queryKey: ['sprints', projectId],
    queryFn: () => api.getProjectSprints(projectId!),
    enabled: !!projectId,
  });

  const activeSprint = useMemo(
    () => sprints?.find((s) => s.status === 'Active'),
    [sprints]
  );

  // Issues query: by default load active sprint issues, or all if no active sprint
  const { data: issuesData, isLoading: issuesLoading } = useQuery({
    queryKey: ['issues', projectId, activeSprint?.id],
    queryFn: () =>
      api.getProjectIssues(projectId!, {
        sprintId: activeSprint?.id,
        pageSize: 100,
      }),
    enabled: !!projectId,
  });

  // Members & Labels for filter dropdowns
  const { data: members } = useQuery({
    queryKey: ['members', projectId],
    queryFn: () => api.getMembers(projectId!),
    enabled: !!projectId,
  });

  const { data: labels } = useQuery({
    queryKey: ['labels', projectId],
    queryFn: () => api.getLabels(projectId!),
    enabled: !!projectId,
  });

  // Optimistic Move Mutation
  const moveMutation = useMutation({
    mutationFn: ({ id, status, boardOrder }: { id: string; status: IssueStatus; boardOrder: number }) =>
      api.moveIssue(id, { status, boardOrder }),
    onMutate: async ({ id, status, boardOrder }) => {
      await queryClient.cancelQueries({ queryKey: ['issues', projectId, activeSprint?.id] });
      const previous = queryClient.getQueryData(['issues', projectId, activeSprint?.id]);

      queryClient.setQueryData(['issues', projectId, activeSprint?.id], (old: any) => {
        if (!old || !old.items) return old;
        const updated = old.items.map((i: IssueDto) =>
          i.id === id ? { ...i, status, boardOrder } : i
        );
        return { ...old, items: updated };
      });

      return { previous };
    },
    onError: (err: any, _, context) => {
      if (context?.previous) {
        queryClient.setQueryData(['issues', projectId, activeSprint?.id], context.previous);
      }
      showError(err.message || 'Failed to update issue position');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
    },
  });

  const issues = issuesData?.items || [];

  // Filtered issues
  const filteredIssues = useMemo(() => {
    return issues.filter((issue) => {
      if (search && !issue.title.toLowerCase().includes(search.toLowerCase()) && !issue.issueKey.toLowerCase().includes(search.toLowerCase())) {
        return false;
      }
      if (assigneeId && issue.assignee?.id !== assigneeId) {
        return false;
      }
      if (labelId && !issue.labels.some((l) => l.id === labelId)) {
        return false;
      }
      return true;
    });
  }, [issues, search, assigneeId, labelId]);

  // Group by status column
  const issuesByColumn = useMemo(() => {
    const map: Record<IssueStatus, IssueDto[]> = {
      ToDo: [],
      InProgress: [],
      InReview: [],
      Done: [],
    };

    filteredIssues.forEach((issue) => {
      if (map[issue.status]) {
        map[issue.status].push(issue);
      }
    });

    Object.keys(map).forEach((key) => {
      map[key as IssueStatus].sort((a, b) => a.boardOrder - b.boardOrder);
    });

    return map;
  }, [filteredIssues]);

  const handleDragStart = (event: DragStartEvent) => {
    const { active } = event;
    const item = issues.find((i) => i.id === active.id);
    if (item) setActiveIssue(item);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveIssue(null);

    if (!over) return;

    const activeId = active.id as string;
    const overId = over.id as string;

    const sourceIssue = issues.find((i) => i.id === activeId);
    if (!sourceIssue) return;

    // Check if dropped directly onto a column container or onto another card
    let targetStatus: IssueStatus | null = null;
    let targetOrder = 0;

    if (COLUMNS.some((c) => c.id === overId)) {
      targetStatus = overId as IssueStatus;
      const colItems = issuesByColumn[targetStatus];
      targetOrder = colItems.length > 0 ? Math.max(...colItems.map((i) => i.boardOrder)) + 1 : 0;
    } else {
      const targetIssue = issues.find((i) => i.id === overId);
      if (targetIssue) {
        targetStatus = targetIssue.status;
        targetOrder = targetIssue.boardOrder;
      }
    }

    if (targetStatus && (sourceIssue.status !== targetStatus || sourceIssue.boardOrder !== targetOrder)) {
      moveMutation.mutate({
        id: activeId,
        status: targetStatus,
        boardOrder: targetOrder,
      });
    }
  };

  return (
    <div className="flex-1 flex flex-col gap-6 w-full">
      {/* Active Sprint Banner */}
      {activeSprint ? (
        <div className="bg-gradient-to-r from-brand-50 to-indigo-50/40 p-4 rounded-2xl border border-brand-100 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 shadow-sm">
          <div>
            <div className="flex items-center gap-2">
              <span className="inline-flex items-center gap-1 text-xs font-bold uppercase tracking-wider text-brand-700 bg-brand-100/70 px-2 py-0.5 rounded-md">
                <Sparkles className="w-3.5 h-3.5" /> Active Sprint
              </span>
              <h2 className="text-base font-bold text-slate-900">{activeSprint.name}</h2>
            </div>
            {activeSprint.goal && (
              <p className="text-xs text-slate-600 mt-1">{activeSprint.goal}</p>
            )}
          </div>

          <div className="text-xs text-slate-500">
            {activeSprint.totalIssuesCount} issues &middot; {activeSprint.completedIssuesCount} completed
          </div>
        </div>
      ) : (
        <div className="bg-slate-50 p-4 rounded-2xl border border-slate-200 text-sm text-slate-600 flex items-center justify-between">
          <span>No active sprint. Showing all project issues.</span>
        </div>
      )}

      {/* Filter Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 bg-white p-3 rounded-2xl border border-slate-200/80 shadow-sm">
        <div className="flex flex-wrap items-center gap-2 flex-1">
          {/* Search */}
          <div className="relative min-w-[200px] flex-1 sm:max-w-xs">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search issues by title or key..."
              className="w-full pl-9 pr-4 py-1.5 text-xs bg-slate-50 rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            />
            {search && (
              <button
                onClick={() => setSearch('')}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </div>

          {/* Assignee Filter */}
          <select
            value={assigneeId}
            onChange={(e) => setAssigneeId(e.target.value)}
            className="px-3 py-1.5 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-none focus:ring-2 focus:ring-brand-100"
          >
            <option value="">All Assignees</option>
            {members?.map((m) => (
              <option key={m.userId} value={m.userId}>
                {m.name}
              </option>
            ))}
          </select>

          {/* Label Filter */}
          <select
            value={labelId}
            onChange={(e) => setLabelId(e.target.value)}
            className="px-3 py-1.5 text-xs rounded-xl border border-slate-200 bg-slate-50 text-slate-700 focus:outline-none focus:ring-2 focus:ring-brand-100"
          >
            <option value="">All Labels</option>
            {labels?.map((l) => (
              <option key={l.id} value={l.id}>
                {l.name}
              </option>
            ))}
          </select>

          {(search || assigneeId || labelId) && (
            <button
              onClick={() => {
                setSearch('');
                setAssigneeId('');
                setLabelId('');
              }}
              className="inline-flex items-center gap-1 text-xs text-brand-600 hover:text-brand-800 font-medium px-2 py-1"
            >
              Reset filters
            </button>
          )}
        </div>
      </div>

      {/* Kanban Board Columns with Drag and Drop */}
      {issuesLoading ? (
        <div className="flex gap-4 overflow-x-auto pb-4">
          {COLUMNS.map((col) => (
            <div key={col.id} className="flex-1 min-w-[280px] max-w-xs space-y-3">
              <Skeleton className="h-8 w-28 rounded-xl" />
              <Skeleton className="h-28 w-full rounded-2xl" />
              <Skeleton className="h-28 w-full rounded-2xl" />
            </div>
          ))}
        </div>
      ) : (
        <DndContext
          sensors={sensors}
          collisionDetection={closestCorners}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
        >
          <div className="flex gap-4 overflow-x-auto pb-6">
            {COLUMNS.map((col) => (
              <KanbanColumn
                key={col.id}
                id={col.id}
                title={col.title}
                issues={issuesByColumn[col.id]}
                onIssueClick={(issue) => setSelectedIssue(issue)}
              />
            ))}
          </div>

          <DragOverlay>
            {activeIssue ? (
              <IssueCard
                issue={activeIssue}
                onClick={() => {}}
                isDragOverlay
              />
            ) : null}
          </DragOverlay>
        </DndContext>
      )}

      {/* Issue Detail Modal */}
      {selectedIssue && (
        <IssueDetailModal
          issueId={selectedIssue.id}
          projectId={project.id}
          isOpen={!!selectedIssue}
          onClose={() => setSelectedIssue(null)}
        />
      )}
    </div>
  );
};
