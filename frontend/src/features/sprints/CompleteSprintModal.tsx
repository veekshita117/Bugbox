import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { SprintDto } from '../../api/types';
import { Modal } from '../../components/common/Modal';
import { useToast } from '../../context/ToastContext';
import { Loader2, CheckCircle2 } from 'lucide-react';

interface CompleteSprintModalProps {
  sprint: SprintDto;
  otherSprints: SprintDto[];
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
}

export const CompleteSprintModal: React.FC<CompleteSprintModalProps> = ({
  sprint,
  otherSprints,
  isOpen,
  onClose,
  projectId,
}) => {
  const [targetSprintId, setTargetSprintId] = useState<string>('');
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const unfinishedCount = sprint.totalIssuesCount - sprint.completedIssuesCount;

  const completeMutation = useMutation({
    mutationFn: () =>
      api.completeSprint(sprint.id, {
        targetSprintId: targetSprintId || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sprints', projectId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      showSuccess(`Sprint "${sprint.name}" completed!`);
      onClose();
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to complete sprint');
    },
  });

  const availableTargets = otherSprints.filter(
    (s) => s.id !== sprint.id && s.status !== 'Completed'
  );

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Complete Sprint">
      <div className="space-y-4">
        <div className="p-4 rounded-2xl bg-indigo-50/50 border border-indigo-100 flex items-start gap-3">
          <CheckCircle2 className="w-5 h-5 text-indigo-600 shrink-0 mt-0.5" />
          <div className="text-xs text-indigo-900 leading-relaxed">
            <span className="font-bold">{sprint.completedIssuesCount} issues</span> completed in this sprint.
            {unfinishedCount > 0 ? (
              <span className="block mt-1 font-semibold text-amber-700">
                {unfinishedCount} open issues remain incomplete.
              </span>
            ) : (
              <span className="block mt-1 text-emerald-700 font-semibold">
                All issues were successfully completed!
              </span>
            )}
          </div>
        </div>

        {unfinishedCount > 0 && (
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Move incomplete issues to:
            </label>
            <select
              value={targetSprintId}
              onChange={(e) => setTargetSprintId(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="">Project Backlog</option>
              {availableTargets.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.status})
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="pt-3 flex items-center justify-end gap-3 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-xs font-medium text-slate-600 hover:text-slate-800 rounded-xl hover:bg-slate-100 transition"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={() => completeMutation.mutate()}
            disabled={completeMutation.isPending}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-xl shadow-sm transition disabled:opacity-50"
          >
            {completeMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
            Confirm & Complete
          </button>
        </div>
      </div>
    </Modal>
  );
};
