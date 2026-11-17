import React, { useState } from 'react';
import { useParams, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../api/client';
import {
  LayoutDashboard,
  Layers,
  Repeat,
  Settings,
  Plus,
  ArrowLeft,
} from 'lucide-react';
import { RoleBadge } from '../../components/common/Badge';
import { Skeleton } from '../../components/common/Skeleton';
import { CreateIssueModal } from '../issues/CreateIssueModal';

export const ProjectLayout: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const [isCreateIssueOpen, setIsCreateIssueOpen] = useState(false);

  const { data: project, isLoading, isError } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => api.getProject(projectId!),
    enabled: !!projectId,
  });

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full space-y-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-12 w-full rounded-2xl" />
        <Skeleton className="h-96 w-full rounded-3xl" />
      </div>
    );
  }

  if (isError || !project) {
    return (
      <div className="max-w-xl mx-auto my-12 p-8 bg-rose-50 border border-rose-200 rounded-3xl text-center">
        <h3 className="text-lg font-bold text-rose-900 mb-2">Project not found</h3>
        <p className="text-sm text-rose-700 mb-6">
          You might not have access to this project or it may have been deleted.
        </p>
        <button
          onClick={() => navigate('/projects')}
          className="inline-flex items-center gap-2 px-4 py-2 bg-white text-slate-700 font-medium rounded-xl border border-slate-200 hover:bg-slate-50 text-sm shadow-sm"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Projects
        </button>
      </div>
    );
  }

  const tabs = [
    { name: 'Board', path: 'board', icon: LayoutDashboard },
    { name: 'Backlog', path: 'backlog', icon: Layers },
    { name: 'Sprints', path: 'sprints', icon: Repeat },
    { name: 'Settings', path: 'settings', icon: Settings },
  ];

  return (
    <div className="flex-1 flex flex-col w-full">
      {/* Project Header */}
      <div className="bg-white border-b border-slate-200/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-6 pb-2">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
            <div className="flex items-center gap-3">
              <span className="font-mono text-sm font-bold text-brand-600 bg-brand-50 px-2.5 py-1 rounded-lg border border-brand-100 uppercase tracking-wider">
                {project.key}
              </span>
              <h1 className="text-2xl font-bold text-slate-900 tracking-tight">{project.name}</h1>
              {project.currentUserRole && <RoleBadge role={project.currentUserRole} />}
            </div>

            <button
              onClick={() => setIsCreateIssueOpen(true)}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-brand-600 hover:bg-brand-700 rounded-xl shadow-md shadow-brand-500/20 transition self-start sm:self-auto"
            >
              <Plus className="w-4 h-4" />
              Create Issue
            </button>
          </div>

          {/* Navigation Tabs */}
          <nav className="flex items-center gap-2 overflow-x-auto no-scrollbar">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <NavLink
                  key={tab.path}
                  to={`/projects/${project.id}/${tab.path}`}
                  className={({ isActive }) =>
                    `inline-flex items-center gap-2 px-4 py-2.5 text-sm font-semibold border-b-2 transition whitespace-nowrap ${
                      isActive
                        ? 'border-brand-600 text-brand-600'
                        : 'border-transparent text-slate-500 hover:text-slate-900 hover:border-slate-300'
                    }`
                  }
                >
                  <Icon className="w-4 h-4" />
                  {tab.name}
                </NavLink>
              );
            })}
          </nav>
        </div>
      </div>

      {/* Tab Content */}
      <div className="flex-1 flex flex-col max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 w-full">
        <Outlet context={{ project }} />
      </div>

      <CreateIssueModal
        isOpen={isCreateIssueOpen}
        onClose={() => setIsCreateIssueOpen(false)}
        projectId={project.id}
      />
    </div>
  );
};
