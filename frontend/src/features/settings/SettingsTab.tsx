import React, { useState } from 'react';
import { useOutletContext, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { ProjectDto } from '../../api/types';
import { Avatar } from '../../components/common/Avatar';
import { RoleBadge } from '../../components/common/Badge';
import { useToast } from '../../context/ToastContext';
import {
  Users,
  Tag,
  Plus,
  Trash2,
  Edit2,
  UserPlus,
  Loader2,
  Check,
} from 'lucide-react';
import { Modal } from '../../components/common/Modal';

export const SettingsTab: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { project } = useOutletContext<{ project: ProjectDto }>();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const isAdmin = project.currentUserRole === 'Admin';

  // Members query
  const { data: members, isLoading: membersLoading } = useQuery({
    queryKey: ['members', projectId],
    queryFn: () => api.getMembers(projectId!),
    enabled: !!projectId,
  });

  // Labels query
  const { data: labels, isLoading: labelsLoading } = useQuery({
    queryKey: ['labels', projectId],
    queryFn: () => api.getLabels(projectId!),
    enabled: !!projectId,
  });

  // Add Member State
  const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);
  const [memberEmail, setMemberEmail] = useState('');
  const [memberRole, setMemberRole] = useState<'Member' | 'Admin'>('Member');

  // Add / Edit Label State
  const [isLabelModalOpen, setIsLabelModalOpen] = useState(false);
  const [editingLabelId, setEditingLabelId] = useState<string | null>(null);
  const [labelName, setLabelName] = useState('');
  const [labelColor, setLabelColor] = useState('#6366F1');

  // Edit Project State
  const [projName, setProjName] = useState(project.name);
  const [projDesc, setProjDesc] = useState(project.description || '');

  // Add Member Mutation
  const addMemberMutation = useMutation({
    mutationFn: (data: { email: string; role: string }) => api.addMember(projectId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['members', projectId] });
      showSuccess('Member added to project');
      setIsAddMemberOpen(false);
      setMemberEmail('');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to add member');
    },
  });

  // Remove Member Mutation
  const removeMemberMutation = useMutation({
    mutationFn: (userId: string) => api.removeMember(projectId!, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['members', projectId] });
      showSuccess('Member removed');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to remove member');
    },
  });

  // Label Mutations
  const createLabelMutation = useMutation({
    mutationFn: (data: { name: string; color: string }) => api.createLabel(projectId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels', projectId] });
      showSuccess('Label created');
      setIsLabelModalOpen(false);
      setLabelName('');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to create label');
    },
  });

  const updateLabelMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; color: string } }) =>
      api.updateLabel(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels', projectId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      showSuccess('Label updated');
      setIsLabelModalOpen(false);
      setEditingLabelId(null);
      setLabelName('');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to update label');
    },
  });

  const deleteLabelMutation = useMutation({
    mutationFn: (id: string) => api.deleteLabel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['labels', projectId] });
      queryClient.invalidateQueries({ queryKey: ['issues', projectId] });
      showSuccess('Label deleted');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to delete label');
    },
  });

  // Update Project Mutation
  const updateProjectMutation = useMutation({
    mutationFn: (data: { name: string; description?: string }) =>
      api.updateProject(projectId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['project', projectId] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showSuccess('Project settings saved');
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to update project');
    },
  });

  const handleLabelSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!labelName.trim()) return;

    if (editingLabelId) {
      updateLabelMutation.mutate({
        id: editingLabelId,
        data: { name: labelName.trim(), color: labelColor },
      });
    } else {
      createLabelMutation.mutate({
        name: labelName.trim(),
        color: labelColor,
      });
    }
  };

  const handleProjectSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!projName.trim()) return;
    updateProjectMutation.mutate({
      name: projName.trim(),
      description: projDesc.trim() || undefined,
    });
  };

  const colorPalette = [
    '#6366F1', // Indigo
    '#EC4899', // Pink
    '#F59E0B', // Amber
    '#10B981', // Emerald
    '#3B82F6', // Blue
    '#8B5CF6', // Purple
    '#EF4444', // Red
    '#64748B', // Slate
  ];

  return (
    <div className="flex-1 flex flex-col gap-8 w-full max-w-4xl mx-auto">
      {/* General Settings */}
      <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm space-y-4">
        <h3 className="text-base font-bold text-slate-900">Project Details</h3>
        <form onSubmit={handleProjectSubmit} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Project Name
              </label>
              <input
                type="text"
                disabled={!isAdmin}
                value={projName}
                onChange={(e) => setProjName(e.target.value)}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 disabled:bg-slate-50 disabled:text-slate-500"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Project Key
              </label>
              <input
                type="text"
                disabled
                value={project.key}
                className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-slate-50 font-mono text-slate-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Description
            </label>
            <textarea
              rows={2}
              disabled={!isAdmin}
              value={projDesc}
              onChange={(e) => setProjDesc(e.target.value)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100 disabled:bg-slate-50 disabled:text-slate-500"
            />
          </div>

          {isAdmin && (
            <div className="flex justify-end">
              <button
                type="submit"
                disabled={updateProjectMutation.isPending}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl transition shadow-sm disabled:opacity-50"
              >
                {updateProjectMutation.isPending && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                Save Details
              </button>
            </div>
          )}
        </form>
      </div>

      {/* Team Members Section */}
      <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Users className="w-5 h-5 text-brand-600" />
            <h3 className="text-base font-bold text-slate-900">
              Team Members ({members?.length || 0})
            </h3>
          </div>

          {isAdmin && (
            <button
              onClick={() => setIsAddMemberOpen(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-brand-600 bg-brand-50 hover:bg-brand-100 rounded-xl transition"
            >
              <UserPlus className="w-3.5 h-3.5" />
              Add Member
            </button>
          )}
        </div>

        {membersLoading ? (
          <div className="text-xs text-slate-400">Loading members...</div>
        ) : (
          <div className="divide-y divide-slate-100">
            {members?.map((member) => (
              <div
                key={member.userId}
                className="py-3 flex items-center justify-between gap-4"
              >
                <div className="flex items-center gap-3">
                  <Avatar name={member.name} size="md" />
                  <div>
                    <h5 className="text-sm font-semibold text-slate-900">{member.name}</h5>
                    <p className="text-xs text-slate-400">{member.email}</p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <RoleBadge role={member.role} />
                  {isAdmin && member.userId !== project.ownerId && (
                    <button
                      onClick={() => {
                        if (window.confirm(`Remove ${member.name} from project?`)) {
                          removeMemberMutation.mutate(member.userId);
                        }
                      }}
                      className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
                      title="Remove member"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Labels Section */}
      <div className="bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Tag className="w-5 h-5 text-brand-600" />
            <h3 className="text-base font-bold text-slate-900">Labels</h3>
          </div>

          <button
            onClick={() => {
              setEditingLabelId(null);
              setLabelName('');
              setLabelColor('#6366F1');
              setIsLabelModalOpen(true);
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-brand-600 bg-brand-50 hover:bg-brand-100 rounded-xl transition"
          >
            <Plus className="w-3.5 h-3.5" />
            New Label
          </button>
        </div>

        {labelsLoading ? (
          <div className="text-xs text-slate-400">Loading labels...</div>
        ) : !labels || labels.length === 0 ? (
          <p className="text-xs text-slate-400 italic">No labels created for this project yet.</p>
        ) : (
          <div className="flex flex-wrap gap-2.5 pt-2">
            {labels.map((label) => (
              <div
                key={label.id}
                className="inline-flex items-center gap-2 px-3 py-1.5 rounded-xl border text-xs font-semibold shadow-xs"
                style={{
                  backgroundColor: `${label.color}15`,
                  color: label.color,
                  borderColor: `${label.color}40`,
                }}
              >
                <span>{label.name}</span>
                <div className="flex items-center gap-1 pl-1 border-l border-current/20">
                  <button
                    onClick={() => {
                      setEditingLabelId(label.id);
                      setLabelName(label.name);
                      setLabelColor(label.color);
                      setIsLabelModalOpen(true);
                    }}
                    className="p-0.5 hover:opacity-75"
                    title="Edit label"
                  >
                    <Edit2 className="w-3 h-3" />
                  </button>
                  <button
                    onClick={() => {
                      if (window.confirm(`Delete label "${label.name}"?`)) {
                        deleteLabelMutation.mutate(label.id);
                      }
                    }}
                    className="p-0.5 hover:opacity-75 text-rose-600"
                    title="Delete label"
                  >
                    <Trash2 className="w-3 h-3" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Add Member Modal */}
      <Modal
        isOpen={isAddMemberOpen}
        onClose={() => setIsAddMemberOpen(false)}
        title="Add Team Member"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!memberEmail.trim()) return;
            addMemberMutation.mutate({ email: memberEmail.trim(), role: memberRole });
          }}
          className="space-y-4"
        >
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Member Email
            </label>
            <input
              type="email"
              required
              value={memberEmail}
              onChange={(e) => setMemberEmail(e.target.value)}
              placeholder="teammate@university.edu"
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Role</label>
            <select
              value={memberRole}
              onChange={(e) => setMemberRole(e.target.value as any)}
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-brand-100"
            >
              <option value="Member">Member</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          <div className="pt-3 flex justify-end gap-2 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsAddMemberOpen(false)}
              className="px-3 py-1.5 text-xs text-slate-600 hover:bg-slate-100 rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!memberEmail.trim() || addMemberMutation.isPending}
              className="px-4 py-1.5 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-lg shadow-sm disabled:opacity-50"
            >
              {addMemberMutation.isPending ? 'Adding...' : 'Add Member'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Label Modal */}
      <Modal
        isOpen={isLabelModalOpen}
        onClose={() => setIsLabelModalOpen(false)}
        title={editingLabelId ? 'Edit Label' : 'Create Label'}
      >
        <form onSubmit={handleLabelSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Label Name</label>
            <input
              type="text"
              required
              value={labelName}
              onChange={(e) => setLabelName(e.target.value)}
              placeholder="frontend, backend, high-priority..."
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-brand-100"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-2">Color</label>
            <div className="flex flex-wrap gap-2">
              {colorPalette.map((color) => (
                <button
                  key={color}
                  type="button"
                  onClick={() => setLabelColor(color)}
                  className="w-7 h-7 rounded-full flex items-center justify-center transition transform hover:scale-110 shadow-xs"
                  style={{ backgroundColor: color }}
                >
                  {labelColor === color && <Check className="w-4 h-4 text-white stroke-[3]" />}
                </button>
              ))}
            </div>
          </div>

          <div className="pt-3 flex justify-end gap-2 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsLabelModalOpen(false)}
              className="px-3 py-1.5 text-xs text-slate-600 hover:bg-slate-100 rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!labelName.trim()}
              className="px-4 py-1.5 text-xs font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-lg shadow-sm disabled:opacity-50"
            >
              Save Label
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
