import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { Modal } from '../../components/common/Modal';
import { useToast } from '../../context/ToastContext';
import { Loader2 } from 'lucide-react';

interface CreateProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: (projectId: string) => void;
}

export const CreateProjectModal: React.FC<CreateProjectModalProps> = ({
  isOpen,
  onClose,
  onCreated,
}) => {
  const [name, setName] = useState('');
  const [key, setKey] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<{ name?: string; key?: string }>({});

  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const createMutation = useMutation({
    mutationFn: api.createProject,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showSuccess(`Project "${data.name}" created!`);
      onClose();
      onCreated(data.id);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to create project');
    },
  });

  const handleNameChange = (val: string) => {
    setName(val);
    if (!key || key.length < 4) {
      // Auto-suggest key from name
      const suggested = val
        .replace(/[^a-zA-Z0-9]/g, '')
        .slice(0, 4)
        .toUpperCase();
      setKey(suggested);
    }
  };

  const validate = () => {
    const errs: { name?: string; key?: string } = {};
    if (!name.trim()) errs.name = 'Project name is required';
    if (!key.trim()) {
      errs.key = 'Project key is required';
    } else if (key.length < 2 || key.length > 10) {
      errs.key = 'Key must be 2-10 characters';
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    createMutation.mutate({
      name: name.trim(),
      key: key.trim().toUpperCase(),
      description: description.trim() || undefined,
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Create new project">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-1" htmlFor="proj-name">
            Project Name
          </label>
          <input
            id="proj-name"
            type="text"
            value={name}
            onChange={(e) => handleNameChange(e.target.value)}
            className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            placeholder="Hackathon 2026 AI Bot"
          />
          {errors.name && <p className="mt-1 text-xs text-rose-600 font-medium">{errors.name}</p>}
        </div>

        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-1" htmlFor="proj-key">
            Project Key (Prefix for issues, e.g. BOT-1)
          </label>
          <input
            id="proj-key"
            type="text"
            value={key}
            maxLength={10}
            onChange={(e) => setKey(e.target.value.toUpperCase())}
            className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm uppercase tracking-wider font-mono focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            placeholder="BOT"
          />
          {errors.key && <p className="mt-1 text-xs text-rose-600 font-medium">{errors.key}</p>}
        </div>

        <div>
          <label className="block text-sm font-semibold text-slate-700 mb-1" htmlFor="proj-desc">
            Description (Optional)
          </label>
          <textarea
            id="proj-desc"
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500"
            placeholder="Autonomous student agent tracker"
          />
        </div>

        <div className="pt-3 flex items-center justify-end gap-3 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-600 hover:text-slate-800 rounded-xl hover:bg-slate-100 transition"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="inline-flex items-center gap-2 px-5 py-2 text-sm font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl shadow-sm transition disabled:opacity-60"
          >
            {createMutation.isPending && <Loader2 className="w-4 h-4 animate-spin" />}
            Create project
          </button>
        </div>
      </form>
    </Modal>
  );
};
