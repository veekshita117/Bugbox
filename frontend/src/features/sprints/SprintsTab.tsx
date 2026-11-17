import React, { useState } from 'react';
import { useOutletContext, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { ProjectDto, SprintDto } from '../../api/types';
import {
  Play,
  CheckCircle2,
  Trash2,
  Plus,
  Calendar,
  Layers,
  Repeat,
  Loader2,
} from 'lucide-react';
import { CreateSprintModal } from './CreateSprintModal';
import { CompleteSprintModal } from './CompleteSprintModal';
import { BurndownChart } from './BurndownChart';
import { EmptyState } from '../../components/common/EmptyState';
import { useToast } from '../../context/ToastContext';

export const SprintsTab: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { project } = useOutletContext<{ project: ProjectDto }>();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [completeSprintTarget, setCompleteSprintTarget] = useState<SprintDto | null>(null);

  const { data: sprints, isLoading } = useQuery({
    queryKey: ['sprints', projectId],
    queryFn: () => api.getProjectSprints(projectId!),
    enabled: !!projectId,
  });

  const startMutation = useMutation({
    mutationFn: (sprintId: string) => api.startSprint(sprintId),
    onSuccess: (sprint) => {
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      showSuccess(`Sprint "${sprint.name}" is now active!`);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to start sprint');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (sprintId: string) => api.deleteSprint(sprintId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      showSuccess('Sprint deleted');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to delete sprint');
    },
  });

  const activeSprint = sprints?.find((s) => s.status === 'Active');
  const plannedSprints = sprints?.filter((s) => s.status === 'Planned') || [];
  const completedSprints = sprints?.filter((s) => s.status === 'Completed') || [];

  return (
    <div className="flex-1 flex flex-col gap-8 w-full max-w-5xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Sprint Management</h2>
          <p className="text-xs text-slate-500 mt-0.5">
            Plan cycles, track velocity, and review burndown analytics
          </p>
        </div>

        <button
          onClick={() => setIsCreateOpen(true)}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl shadow-sm transition"
        >
          <Plus className="w-4 h-4" />
          New Sprint
        </button>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          <div className="h-48 bg-white rounded-3xl animate-pulse border border-slate-100" />
          <div className="h-24 bg-white rounded-3xl animate-pulse border border-slate-100" />
        </div>
      ) : !sprints || sprints.length === 0 ? (
        <EmptyState
          icon={Repeat}
          title="No sprints planned yet"
          description="Create your team's first sprint to organize issues into manageable 1-2 week cycles."
          actionLabel="Create Sprint"
          onAction={() => setIsCreateOpen(true)}
        />
      ) : (
        <div className="space-y-8">
          {/* Active Sprint Section */}
          {activeSprint && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400">
                  Active Sprint
                </h3>

                <button
                  onClick={() => setCompleteSprintTarget(activeSprint)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 rounded-xl transition"
                >
                  <CheckCircle2 className="w-4 h-4" />
                  Complete Sprint
                </button>
              </div>

              <div className="bg-white p-6 rounded-3xl border border-brand-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 animate-pulse" />
                    <h4 className="text-lg font-bold text-slate-900">{activeSprint.name}</h4>
                  </div>
                  {activeSprint.goal && (
                    <p className="text-xs text-slate-500 max-w-xl">{activeSprint.goal}</p>
                  )}
                  <div className="flex items-center gap-4 mt-3 text-xs text-slate-400">
                    <span className="flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5" />
                      {activeSprint.startDate ? new Date(activeSprint.startDate).toLocaleDateString() : 'N/A'} &mdash;{' '}
                      {activeSprint.endDate ? new Date(activeSprint.endDate).toLocaleDateString() : 'N/A'}
                    </span>
                    <span className="flex items-center gap-1">
                      <Layers className="w-3.5 h-3.5" />
                      {activeSprint.totalIssuesCount} issues ({activeSprint.completedIssuesCount} completed)
                    </span>
                  </div>
                </div>
              </div>

              {/* Burndown Chart for Active Sprint */}
              <BurndownChart sprintId={activeSprint.id} />
            </div>
          )}

          {/* Planned Sprints */}
          <div className="space-y-3">
            <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400">
              Planned Sprints ({plannedSprints.length})
            </h3>

            {plannedSprints.length === 0 ? (
              <p className="text-xs text-slate-400 italic bg-white p-4 rounded-2xl border border-slate-100">
                No planned sprints. Click "New Sprint" to schedule the next iteration.
              </p>
            ) : (
              <div className="space-y-3">
                {plannedSprints.map((sprint) => (
                  <div
                    key={sprint.id}
                    className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                  >
                    <div>
                      <h4 className="text-sm font-bold text-slate-900">{sprint.name}</h4>
                      {sprint.goal && <p className="text-xs text-slate-500 mt-0.5">{sprint.goal}</p>}
                      <div className="flex items-center gap-3 mt-2 text-xs text-slate-400">
                        <span>{sprint.totalIssuesCount} issues queued</span>
                      </div>
                    </div>

                    <div className="flex items-center gap-2 self-end sm:self-auto">
                      <button
                        onClick={() => startMutation.mutate(sprint.id)}
                        disabled={startMutation.isPending}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl shadow-sm transition disabled:opacity-50"
                      >
                        {startMutation.isPending ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        ) : (
                          <Play className="w-3.5 h-3.5 fill-current" />
                        )}
                        Start Sprint
                      </button>

                      <button
                        onClick={() => {
                          if (window.confirm(`Delete "${sprint.name}"? Issues will return to backlog.`)) {
                            deleteMutation.mutate(sprint.id);
                          }
                        }}
                        disabled={deleteMutation.isPending}
                        className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition"
                        title="Delete planned sprint"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Completed Sprints */}
          {completedSprints.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400">
                Completed Sprints ({completedSprints.length})
              </h3>
              <div className="space-y-3">
                {completedSprints.map((sprint) => (
                  <div
                    key={sprint.id}
                    className="bg-slate-50/70 p-4 rounded-2xl border border-slate-200/70 flex items-center justify-between text-xs"
                  >
                    <div>
                      <span className="font-bold text-slate-800">{sprint.name}</span>
                      <span className="text-slate-400 ml-2">
                        {sprint.completedIssuesCount}/{sprint.totalIssuesCount} completed
                      </span>
                    </div>
                    <span className="text-slate-400 font-medium">Completed</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      <CreateSprintModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        projectId={project.id}
      />

      {completeSprintTarget && (
        <CompleteSprintModal
          sprint={completeSprintTarget}
          otherSprints={sprints || []}
          isOpen={!!completeSprintTarget}
          onClose={() => setCompleteSprintTarget(null)}
          projectId={project.id}
        />
      )}
    </div>
  );
};
