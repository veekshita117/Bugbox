import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { IssuePriority, IssueStatus, IssueType, ProjectRole } from '../../api/types';
import { Modal } from '../../components/common/Modal';
import { PriorityBadge, StatusBadge, TypeBadge } from '../../components/common/Badge';
import { Avatar } from '../../components/common/Avatar';
import { CommentsList } from './CommentsList';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { Trash2, Loader2, Calendar, Tag } from 'lucide-react';

interface IssueDetailModalProps {
  issueId: string;
  projectId: string;
  isOpen: boolean;
  onClose: () => void;
  userRole?: ProjectRole;
}

export const IssueDetailModal: React.FC<IssueDetailModalProps> = ({
  issueId,
  projectId,
  isOpen,
  onClose,
  userRole,
}) => {
  const { user } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();

  const { data: issue, isLoading } = useQuery({
    queryKey: ['issue', issueId],
    queryFn: () => api.getIssue(issueId),
    enabled: isOpen && !!issueId,
  });

  const { data: members } = useQuery({
    queryKey: ['members', projectId],
    queryFn: () => api.getMembers(projectId),
    enabled: isOpen && !!projectId,
  });

  const { data: sprints } = useQuery({
    queryKey: ['sprints', projectId],
    queryFn: () => api.getProjectSprints(projectId),
    enabled: isOpen && !!projectId,
  });

  const { data: labels } = useQuery({
    queryKey: ['labels', projectId],
    queryFn: () => api.getLabels(projectId),
    enabled: isOpen && !!projectId,
  });

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  useEffect(() => {
    if (issue) {
      setTitle(issue.title);
      setDescription(issue.description || '');
    }
  }, [issue]);

  // Update Mutation
  const updateMutation = useMutation({
    mutationFn: (updated: any) =>
      api.updateIssue(issueId, {
        title: updated.title ?? issue!.title,
        description: updated.description ?? issue!.description,
        type: updated.type ?? issue!.type,
        priority: updated.priority ?? issue!.priority,
        status: updated.status ?? issue!.status,
        assigneeId: updated.assigneeId !== undefined ? updated.assigneeId : issue!.assignee?.id,
        sprintId: updated.sprintId !== undefined ? updated.sprintId : issue!.sprintId,
        labelIds: updated.labelIds ?? issue!.labels.map((l) => l.id),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['issue', issueId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      showSuccess('Issue updated');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to update issue');
    },
  });

  // Delete Mutation
  const deleteMutation = useMutation({
    mutationFn: () => api.deleteIssue(issueId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      showSuccess('Issue deleted');
      onClose();
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to delete issue');
    },
  });

  if (!isOpen) return null;

  const canDelete =
    userRole === 'Admin' || (issue && user && issue.reporter.id === user.id);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={issue ? `${issue.issueKey}` : 'Issue Details'}
      maxWidth="2xl"
    >
      {isLoading || !issue ? (
        <div className="py-8 text-center text-slate-400 text-sm">Loading issue details...</div>
      ) : (
        <div className="space-y-6">
          {/* Header Title */}
          <div>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              onBlur={() => {
                if (title.trim() && title !== issue.title) {
                  updateMutation.mutate({ title: title.trim() });
                }
              }}
              className="w-full text-lg font-bold text-slate-900 px-3 py-1.5 rounded-xl border border-transparent hover:border-slate-200 focus:border-brand-500 focus:bg-white focus:outline-none transition"
            />
          </div>

          {/* Properties Grid */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 p-4 rounded-2xl bg-slate-50 border border-slate-100 text-xs">
            {/* Status */}
            <div>
              <span className="block text-slate-400 font-semibold mb-1">Status</span>
              <select
                value={issue.status}
                onChange={(e) =>
                  updateMutation.mutate({ status: e.target.value as IssueStatus })
                }
                className="w-full bg-white px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-800 font-medium focus:outline-none focus:ring-1 focus:ring-brand-500"
              >
                <option value="ToDo">To Do</option>
                <option value="InProgress">In Progress</option>
                <option value="InReview">In Review</option>
                <option value="Done">Done</option>
              </select>
            </div>

            {/* Priority */}
            <div>
              <span className="block text-slate-400 font-semibold mb-1">Priority</span>
              <select
                value={issue.priority}
                onChange={(e) =>
                  updateMutation.mutate({ priority: e.target.value as IssuePriority })
                }
                className="w-full bg-white px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-800 font-medium focus:outline-none focus:ring-1 focus:ring-brand-500"
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
              </select>
            </div>

            {/* Type */}
            <div>
              <span className="block text-slate-400 font-semibold mb-1">Type</span>
              <select
                value={issue.type}
                onChange={(e) =>
                  updateMutation.mutate({ type: e.target.value as IssueType })
                }
                className="w-full bg-white px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-800 font-medium focus:outline-none focus:ring-1 focus:ring-brand-500"
              >
                <option value="Bug">Bug</option>
                <option value="Feature">Feature</option>
                <option value="Task">Task</option>
              </select>
            </div>

            {/* Assignee */}
            <div>
              <span className="block text-slate-400 font-semibold mb-1">Assignee</span>
              <select
                value={issue.assignee?.id || ''}
                onChange={(e) =>
                  updateMutation.mutate({
                    assigneeId: e.target.value ? e.target.value : null,
                  })
                }
                className="w-full bg-white px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-800 font-medium focus:outline-none focus:ring-1 focus:ring-brand-500 truncate"
              >
                <option value="">Unassigned</option>
                {members?.map((m) => (
                  <option key={m.userId} value={m.userId}>
                    {m.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Sprint */}
            <div className="col-span-2">
              <span className="block text-slate-400 font-semibold mb-1">Sprint</span>
              <select
                value={issue.sprintId || ''}
                onChange={(e) =>
                  updateMutation.mutate({
                    sprintId: e.target.value ? e.target.value : null,
                  })
                }
                className="w-full bg-white px-2.5 py-1.5 rounded-lg border border-slate-200 text-slate-800 font-medium focus:outline-none focus:ring-1 focus:ring-brand-500"
              >
                <option value="">Backlog (No Sprint)</option>
                {sprints
                  ?.filter((s) => s.status !== 'Completed')
                  .map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.status})
                    </option>
                  ))}
              </select>
            </div>

            {/* Reporter */}
            <div className="col-span-2 flex items-center gap-2 pt-2">
              <Avatar name={issue.reporter.name} size="sm" />
              <div>
                <span className="block text-[10px] text-slate-400">Reporter</span>
                <span className="font-semibold text-slate-700">{issue.reporter.name}</span>
              </div>
            </div>
          </div>

          {/* Labels Toggle */}
          <div>
            <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-600 mb-2">
              <Tag className="w-3.5 h-3.5 text-slate-400" />
              Labels
            </div>
            <div className="flex flex-wrap gap-1.5">
              {labels?.map((label) => {
                const isSelected = issue.labels.some((l) => l.id === label.id);
                return (
                  <button
                    key={label.id}
                    type="button"
                    onClick={() => {
                      const newIds = isSelected
                        ? issue.labels.filter((l) => l.id !== label.id).map((l) => l.id)
                        : [...issue.labels.map((l) => l.id), label.id];
                      updateMutation.mutate({ labelIds: newIds });
                    }}
                    className={`px-2.5 py-1 rounded-lg text-xs font-medium border transition ${
                      isSelected
                        ? 'border-transparent shadow-sm'
                        : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                    }`}
                    style={
                      isSelected
                        ? {
                            backgroundColor: `${label.color}25`,
                            color: label.color,
                            borderColor: `${label.color}50`,
                          }
                        : {}
                    }
                  >
                    {label.name}
                  </button>
                );
              })}
              {(!labels || labels.length === 0) && (
                <span className="text-xs text-slate-400 italic">No labels created in project.</span>
              )}
            </div>
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1.5">
              Description
            </label>
            <textarea
              rows={4}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              onBlur={() => {
                if (description !== (issue.description || '')) {
                  updateMutation.mutate({ description });
                }
              }}
              placeholder="Add detailed steps to reproduce, acceptance criteria, or technical notes..."
              className="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            />
          </div>

          {/* Comments Section */}
          <CommentsList
            issueId={issue.id}
            projectId={projectId}
            userRole={userRole}
          />

          {/* Footer Actions */}
          <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
            {canDelete ? (
              <button
                type="button"
                onClick={() => {
                  if (window.confirm('Are you sure you want to delete this issue?')) {
                    deleteMutation.mutate();
                  }
                }}
                disabled={deleteMutation.isPending}
                className="inline-flex items-center gap-1.5 text-xs text-rose-600 hover:text-rose-800 font-semibold p-1 transition"
              >
                <Trash2 className="w-3.5 h-3.5" />
                Delete issue
              </button>
            ) : <div />}

            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition"
            >
              Done
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
};
