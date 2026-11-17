import React from 'react';
import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { IssueDto, IssueStatus } from '../../api/types';
import { IssueCard } from './IssueCard';

interface KanbanColumnProps {
  id: IssueStatus;
  title: string;
  issues: IssueDto[];
  onIssueClick: (issue: IssueDto) => void;
}

export const KanbanColumn: React.FC<KanbanColumnProps> = ({
  id,
  title,
  issues,
  onIssueClick,
}) => {
  const { setNodeRef, isOver } = useDroppable({
    id,
  });

  const columnColors: Record<IssueStatus, { header: string; countBg: string }> = {
    ToDo: { header: 'text-slate-700', countBg: 'bg-slate-200/70 text-slate-700' },
    InProgress: { header: 'text-blue-700', countBg: 'bg-blue-100 text-blue-700' },
    InReview: { header: 'text-purple-700', countBg: 'bg-purple-100 text-purple-700' },
    Done: { header: 'text-emerald-700', countBg: 'bg-emerald-100 text-emerald-700' },
  };

  return (
    <div
      ref={setNodeRef}
      className={`flex-1 min-w-[280px] max-w-xs flex flex-col rounded-3xl bg-slate-100/70 p-3.5 border transition-colors duration-150 ${
        isOver ? 'border-brand-400 bg-brand-50/30' : 'border-transparent'
      }`}
    >
      <div className="flex items-center justify-between px-2 py-1.5 mb-3">
        <h3 className={`text-sm font-bold tracking-tight uppercase ${columnColors[id].header}`}>
          {title}
        </h3>
        <span
          className={`px-2 py-0.5 rounded-full text-xs font-semibold ${columnColors[id].countBg}`}
        >
          {issues.length}
        </span>
      </div>

      <div className="flex-1 flex flex-col gap-3 overflow-y-auto max-h-[calc(100vh-280px)] pr-1">
        <SortableContext items={issues.map((i) => i.id)} strategy={verticalListSortingStrategy}>
          {issues.map((issue) => (
            <IssueCard key={issue.id} issue={issue} onClick={onIssueClick} />
          ))}
        </SortableContext>

        {issues.length === 0 && (
          <div className="flex-1 min-h-[120px] flex items-center justify-center border-2 border-dashed border-slate-200 rounded-2xl text-xs text-slate-400 font-medium">
            No issues
          </div>
        )}
      </div>
    </div>
  );
};
