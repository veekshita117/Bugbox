import React from 'react';
import { IssuePriority, IssueStatus, IssueType, ProjectRole } from '../../api/types';

export const PriorityBadge: React.FC<{ priority: IssuePriority }> = ({ priority }) => {
  const styles = {
    Low: 'bg-slate-100 text-slate-700 border-slate-200',
    Medium: 'bg-amber-50 text-amber-700 border-amber-200',
    High: 'bg-rose-50 text-rose-700 border-rose-200',
  }[priority] || 'bg-slate-100 text-slate-700 border-slate-200';

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium border ${styles}`}>
      {priority}
    </span>
  );
};

export const TypeBadge: React.FC<{ type: IssueType }> = ({ type }) => {
  const styles = {
    Bug: 'bg-rose-100 text-rose-700',
    Feature: 'bg-emerald-100 text-emerald-700',
    Task: 'bg-sky-100 text-sky-700',
  }[type] || 'bg-slate-100 text-slate-700';

  return (
    <span className={`inline-flex items-center px-1.5 py-0.5 rounded text-[11px] font-semibold tracking-wide uppercase ${styles}`}>
      {type}
    </span>
  );
};

export const StatusBadge: React.FC<{ status: IssueStatus }> = ({ status }) => {
  const labels: Record<IssueStatus, string> = {
    ToDo: 'To Do',
    InProgress: 'In Progress',
    InReview: 'In Review',
    Done: 'Done',
  };

  const styles: Record<IssueStatus, string> = {
    ToDo: 'bg-slate-100 text-slate-700',
    InProgress: 'bg-blue-100 text-blue-700',
    InReview: 'bg-purple-100 text-purple-700',
    Done: 'bg-emerald-100 text-emerald-700',
  };

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${styles[status]}`}>
      {labels[status]}
    </span>
  );
};

export const RoleBadge: React.FC<{ role: ProjectRole }> = ({ role }) => {
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${
        role === 'Admin'
          ? 'bg-brand-50 text-brand-700 border border-brand-200'
          : 'bg-slate-100 text-slate-600 border border-slate-200'
      }`}
    >
      {role}
    </span>
  );
};
