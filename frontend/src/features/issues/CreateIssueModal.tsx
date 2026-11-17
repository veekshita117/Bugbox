import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { Modal } from '../../components/common/Modal';
import { IssuePriority, IssueStatus, IssueType } from '../../api/types';
import { useToast } from '../../context/ToastContext';
import { Loader2 } from 'lucide-react';

interface CreateIssueModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  defaultSprintId?: string;
}

export const CreateIssueModal: React.FC<CreateIssueModalProps> = ({
  isOpen,
  onClose,
  projectId,
  defaultSprintId,
}) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<IssueType>('Task');
  const [priority, setPriority] = useState<IssuePriority>('Medium');
  const [status, setStatus] = useState<IssueStatus>('ToDo');
  const [assigneeId, setAssigneeId] = useState<string>('');
  const [sprintId, setSprintId] = useState<string>(defaultSprintId || '');
  const [selectedLabels, setSelectedLabels] = useState<string[]>([]);

  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

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

  const createMutation = useMutation({
    mutationFn: (data: any) => api.createIssue(projectId, data),
    onSuccess: (newIssue) => {
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      showSuccess(`Issue ${newIssue.issueKey} created!`);
      onClose();
      // Reset
      setTitle('');
      setDescription('');
      setSelectedLabels([]);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to create issue');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    createMutation.mutate({
      title: title.trim(),
      description: description.trim() || undefined,
      type,
      priority,
      status,
      assigneeId: assigneeId || undefined,
      sprintId: sprintId || undefined,
      labelIds: selectedLabels.length > 0 ? selectedLabels : undefined,
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create Issue" maxWidth="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1" htmlFor="issue-title">
            Title *
          </label>
          <input
            id="issue-title"
            type="text"
            required
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className="w-full px-3.5 py-2 text-sm rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            placeholder="What needs to be done?"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Issue Type</label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as IssueType)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="Task">Task</option>
              <option value="Bug">Bug</option>
              <option value="Feature">Feature</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Priority</label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value as IssuePriority)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Status</label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as IssueStatus)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="ToDo">To Do</option>
              <option value="InProgress">In Progress</option>
              <option value="InReview">In Review</option>
              <option value="Done">Done</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Assignee</label>
            <select
              value={assigneeId}
              onChange={(e) => setAssigneeId(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="">Unassigned</option>
              {members?.map((m) => (
                <option key={m.userId} value={m.userId}>
                  {m.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">Sprint</label>
          <select
            value={sprintId}
            onChange={(e) => setSprintId(e.target.value)}
            className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
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

        {labels && labels.length > 0 && (
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">Labels</label>
            <div className="flex flex-wrap gap-1.5">
              {labels.map((l) => {
                const selected = selectedLabels.includes(l.id);
                return (
                  <button
                    key={l.id}
                    type="button"
                    onClick={() => {
                      setSelectedLabels((prev) =>
                        selected ? prev.filter((id) => id !== l.id) : [...prev, l.id]
                      );
                    }}
                    className={`px-2.5 py-1 text-xs rounded-lg border transition ${
                      selected
                        ? 'border-transparent font-semibold shadow-sm'
                        : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                    }`}
                    style={
                      selected
                        ? {
                            backgroundColor: `${l.color}25`,
                            color: l.color,
                            borderColor: `${l.color}60`,
                          }
                        : {}
                    }
                  >
                    {l.name}
                  </button>
                );
              })}
            </div>
          </div>
        )}

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1">Description</label>
          <textarea
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Add context, acceptance criteria or steps..."
            className="w-full px-3.5 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
          />
        </div>

        <div className="pt-3 flex items-center justify-end gap-3 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 rounded-xl hover:bg-slate-100 transition"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={!title.trim() || createMutation.isPending}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl shadow-sm transition disabled:opacity-50"
          >
            {createMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
            Create Issue
          </button>
        </div>
      </form>
    </Modal>
  );
};
