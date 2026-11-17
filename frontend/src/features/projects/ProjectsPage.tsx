import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { FolderKanban, Plus, Clock, Users, Sparkles, Loader2 } from 'lucide-react';
import { RoleBadge } from '../../components/common/Badge';
import { CardSkeleton } from '../../components/common/Skeleton';
import { EmptyState } from '../../components/common/EmptyState';
import { CreateProjectModal } from './CreateProjectModal';
import { useToast } from '../../context/ToastContext';

export const ProjectsPage: React.FC = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  const { data: projects, isLoading, isError } = useQuery({
    queryKey: ['projects'],
    queryFn: api.getProjects,
  });

  const demoMutation = useMutation({
    mutationFn: api.seedDemo,
    onSuccess: (project) => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showSuccess(`Demo project "${project.name}" generated!`);
      navigate(`/projects/${project.id}/board`);
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to seed demo project');
    },
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">Projects</h1>
          <p className="text-sm text-slate-500 mt-1">
            Manage your student and hackathon issue trackers
          </p>
        </div>

        <div className="flex items-center gap-3 self-start sm:self-auto">
          <button
            onClick={() => demoMutation.mutate()}
            disabled={demoMutation.isPending}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl font-semibold text-sm text-brand-700 bg-brand-50 hover:bg-brand-100 border border-brand-200 transition disabled:opacity-50"
            title="Generate a realistic sample project with users, sprints, issues, and burndown chart"
          >
            {demoMutation.isPending ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Sparkles className="w-4 h-4 text-brand-600" />
            )}
            Try Demo Project
          </button>

          <button
            onClick={() => setIsModalOpen(true)}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl font-semibold text-sm text-white bg-brand-600 hover:bg-brand-700 shadow-md shadow-brand-500/20 transition"
          >
            <Plus className="w-4 h-4" />
            New Project
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <CardSkeleton />
          <CardSkeleton />
          <CardSkeleton />
        </div>
      ) : isError ? (
        <div className="p-6 bg-rose-50 border border-rose-200 rounded-2xl text-rose-800 text-sm">
          Failed to load projects. Please try refreshing.
        </div>
      ) : !projects || projects.length === 0 ? (
        <div className="flex flex-col items-center">
          <EmptyState
            icon={FolderKanban}
            title="No projects found"
            description="Create your first project or launch a pre-populated demo project with active sprints, issues, and burndown chart."
            actionLabel="Create Project"
            onAction={() => setIsModalOpen(true)}
          />
          <button
            onClick={() => demoMutation.mutate()}
            disabled={demoMutation.isPending}
            className="mt-4 inline-flex items-center gap-2 px-5 py-2.5 text-sm font-semibold text-brand-700 bg-brand-50 hover:bg-brand-100 border border-brand-200 rounded-xl transition"
          >
            {demoMutation.isPending ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4 text-brand-600" />}
            Or load a ready-to-explore Demo Project
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {projects.map((project) => (
            <div
              key={project.id}
              onClick={() => navigate(`/projects/${project.id}/board`)}
              className="group bg-white p-6 rounded-3xl border border-slate-200/80 shadow-sm hover:shadow-xl hover:border-brand-300 transition duration-200 cursor-pointer flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center justify-between gap-2 mb-3">
                  <span className="font-mono text-xs font-bold text-brand-600 bg-brand-50 px-2.5 py-1 rounded-lg border border-brand-100 uppercase tracking-wider">
                    {project.key}
                  </span>
                  {project.currentUserRole && <RoleBadge role={project.currentUserRole} />}
                </div>

                <h3 className="text-lg font-bold text-slate-900 group-hover:text-brand-600 transition line-clamp-1 mb-2">
                  {project.name}
                </h3>

                <p className="text-sm text-slate-500 line-clamp-2 mb-6">
                  {project.description || 'No description provided.'}
                </p>
              </div>

              <div className="pt-4 border-t border-slate-100 flex items-center justify-between text-xs text-slate-400">
                <span className="flex items-center gap-1.5">
                  <Clock className="w-3.5 h-3.5" />
                  {new Date(project.createdAt).toLocaleDateString()}
                </span>
                <span className="flex items-center gap-1 font-medium text-slate-600">
                  <Users className="w-3.5 h-3.5 text-slate-400" />
                  View workspace &rarr;
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      <CreateProjectModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onCreated={(id) => navigate(`/projects/${id}/board`)}
      />
    </div>
  );
};
