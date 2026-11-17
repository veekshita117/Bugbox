import React, { useState } from 'react';
import { useOutletContext, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { IssueDto, ProjectDto, IssueType, IssuePriority } from '../../api/types';
import { Plus, ArrowRight, Layers, Loader2 } from 'lucide-react';
import { PriorityBadge, TypeBadge } from '../../components/common/Badge';
import { Avatar } from '../../components/common/Avatar';
import { EmptyState } from '../../components/common/EmptyState';
import { IssueDetailModal } from '../issues/IssueDetailModal';
import { useToast } from '../../context/ToastContext';

export const BacklogTab: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { project } = useOutletContext<{ project: ProjectDto }>();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const [quickTitle, setQuickTitle] = useState('');
  const [selectedIssue, setSelectedIssue] = useState<IssueDto | null>(null);
  const [targetSprintMap, setTargetSprintMap] = useState<Record<string, string>>({});

  // Backlog issues: all issues in project where sprintId is not set
  const { data: issuesData, isLoading: issuesLoading } = useQuery({
    queryKey: ['issues', projectId, 'backlog'],
    queryFn: () =>
      api.getProjectIssues(projectId!, {
        pageSize: 100,
      }),
    enabled: !!projectId,
  });

  const { data: sprints } = useQuery({
    queryKey: ['sprints', projectId],
    queryFn: () => api.getProjectSprints(projectId!),
    enabled: !!projectId,
  });

  const activeOrPlannedSprints = sprints?.filter((s) => s.status !== 'Completed') || [];

  // Filter unscheduled
  const backlogIssues = (issuesData?.items || []).filter((i) => !i.sprintId);

  // Quick Inline Create Mutation
  const createMutation = useMutation({
    mutationFn: (title: string) =>
      api.createIssue(projectId!, {
        title,
        type: 'Task' as IssueType,
        priority: 'Medium' as IssuePriority,
      }),
    onSuccess: (newIssue) => {
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      setQuickTitle('');
      showSuccess(`Created ${newIssue.issueKey}`);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to create issue');
    },
  });

  // Assign to Sprint Mutation
  const assignMutation = useMutation({
    mutationFn: ({ sprintId, issueId }: { sprintId: string; issueId: string }) =>
      api.addIssuesToSprint(sprintId, [issueId]),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      showSuccess('Moved to sprint');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to move to sprint');
    },
  });

  const handleQuickCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!quickTitle.trim()) return;
    createMutation.mutate(quickTitle.trim());
  };

  return (
    <div className="flex-1 flex flex-col gap-6 w-full max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Project Backlog</h2>
          <p className="text-xs text-slate-500 mt-0.5">
            Unscheduled tasks waiting to be planned into a sprint ({backlogIssues.length} issues)
          </p>
        </div>
      </div>

      {/* Inline Quick Create Card */}
      <form
        onSubmit={handleQuickCreate}
        className="flex items-center gap-3 bg-white p-3 rounded-2xl border border-slate-200 shadow-sm focus-within:ring-2 focus-within:ring-brand-100 focus-within:border-brand-500 transition"
      >
        <Plus className="w-5 h-5 text-slate-400 shrink-0 ml-1" />
        <input
          type="text"
          value={quickTitle}
          onChange={(e) => setQuickTitle(e.target.value)}
          placeholder="Create issue inline (press Enter to quickly save)..."
          className="flex-1 text-sm bg-transparent border-none focus:outline-none placeholder:text-slate-400"
        />
        <button
          type="submit"
          disabled={!quickTitle.trim() || createMutation.isPending}
          className="px-4 py-1.5 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl transition disabled:opacity-50 flex items-center gap-1"
        >
          {createMutation.isPending ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : 'Add'}
        </button>
      </form>

      {/* Backlog List */}
      {issuesLoading ? (
        <div className="space-y-3">
          <div className="h-16 bg-white rounded-2xl animate-pulse border border-slate-100" />
          <div className="h-16 bg-white rounded-2xl animate-pulse border border-slate-100" />
          <div className="h-16 bg-white rounded-2xl animate-pulse border border-slate-100" />
        </div>
      ) : backlogIssues.length === 0 ? (
        <EmptyState
          icon={Layers}
          title="Backlog is empty"
          description="Everything is scheduled in sprints, or no backlog items have been created yet."
        />
      ) : (
        <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm overflow-hidden divide-y divide-slate-100">
          {backlogIssues.map((issue) => (
            <div
              key={issue.id}
              className="p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:bg-slate-50/80 transition group"
            >
              <div
                onClick={() => setSelectedIssue(issue)}
                className="flex items-center gap-3 cursor-pointer flex-1 min-w-0"
              >
                <TypeBadge type={issue.type} />
                <span className="font-mono text-xs font-bold text-slate-500 shrink-0">
                  {issue.issueKey}
                </span>
                <span className="text-sm font-semibold text-slate-800 hover:text-brand-600 transition truncate">
                  {issue.title}
                </span>

                {issue.labels?.map((l) => (
                  <span
                    key={l.id}
                    className="hidden md:inline-flex px-2 py-0.5 rounded text-[11px] font-medium"
                    style={{
                      backgroundColor: `${l.color}15`,
                      color: l.color,
                    }}
                  >
                    {l.name}
                  </span>
                ))}
              </div>

              {/* Actions & Meta */}
              <div className="flex items-center gap-3 self-end sm:self-auto shrink-0">
                <PriorityBadge priority={issue.priority} />

                {issue.assignee ? (
                  <Avatar name={issue.assignee.name} size="sm" />
                ) : (
                  <div
                    className="w-6 h-6 rounded-full border border-dashed border-slate-300 flex items-center justify-center text-[10px] text-slate-400"
                    title="Unassigned"
                  >
                    ?
                  </div>
                )}

                {/* Move to Sprint Dropdown */}
                {activeOrPlannedSprints.length > 0 && (
                  <div className="flex items-center gap-1.5 pl-3 border-l border-slate-100">
                    <select
                      value={targetSprintMap[issue.id] || ''}
                      onChange={(e) =>
                        setTargetSprintMap((prev) => ({
                          ...prev,
                          [issue.id]: e.target.value,
                        }))
                      }
                      className="px-2 py-1 text-xs rounded-lg border border-slate-200 bg-slate-50 text-slate-700 focus:outline-none"
                    >
                      <option value="">Move to sprint...</option>
                      {activeOrPlannedSprints.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.name} ({s.status})
                        </option>
                      ))}
                    </select>

                    <button
                      type="button"
                      disabled={!targetSprintMap[issue.id] || assignMutation.isPending}
                      onClick={() => {
                        const sId = targetSprintMap[issue.id];
                        if (sId) {
                          assignMutation.mutate({ sprintId: sId, issueId: issue.id });
                        }
                      }}
                      className="p-1 rounded-lg text-slate-500 hover:text-brand-600 hover:bg-brand-50 transition disabled:opacity-30"
                      title="Move issue to selected sprint"
                    >
                      <ArrowRight className="w-4 h-4" />
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedIssue && (
        <IssueDetailModal
          issueId={selectedIssue.id}
          projectId={project.id}
          isOpen={!!selectedIssue}
          onClose={() => setSelectedIssue(null)}
          userRole={project.currentUserRole}
        />
      )}
    </div>
  );
};
