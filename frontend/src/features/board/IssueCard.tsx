import React from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { IssueDto } from '../../api/types';
import { PriorityBadge, TypeBadge } from '../../components/common/Badge';
import { Avatar } from '../../components/common/Avatar';
import { MessageSquare } from 'lucide-react';

interface IssueCardProps {
  issue: IssueDto;
  onClick: (issue: IssueDto) => void;
  isDragOverlay?: boolean;
}

export const IssueCard: React.FC<IssueCardProps> = ({
  issue,
  onClick,
  isDragOverlay = false,
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: issue.id,
    data: { issue },
    disabled: isDragOverlay,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      onClick={() => onClick(issue)}
      className={`group bg-white p-4 rounded-2xl border transition-all select-none cursor-pointer ${
        isDragging
          ? 'opacity-40 border-brand-300 shadow-none'
          : 'border-slate-200/80 shadow-sm hover:shadow-md hover:border-slate-300 active:cursor-grabbing'
      } ${isDragOverlay ? 'shadow-2xl border-brand-500 rotate-1 scale-105' : ''}`}
    >
      <div className="flex items-center justify-between gap-2 mb-2">
        <div className="flex items-center gap-1.5">
          <TypeBadge type={issue.type} />
          <span className="font-mono text-xs font-semibold text-slate-500">
            {issue.issueKey}
          </span>
        </div>
        <PriorityBadge priority={issue.priority} />
      </div>

      <h4 className="text-sm font-semibold text-slate-800 line-clamp-2 mb-3 leading-snug group-hover:text-brand-600 transition">
        {issue.title}
      </h4>

      {issue.labels && issue.labels.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mb-3">
          {issue.labels.map((l) => (
            <span
              key={l.id}
              className="inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium"
              style={{
                backgroundColor: `${l.color}15`,
                color: l.color,
                borderColor: `${l.color}30`,
              }}
            >
              {l.name}
            </span>
          ))}
        </div>
      )}

      <div className="pt-2 border-t border-slate-100 flex items-center justify-between">
        <div className="flex items-center gap-2">
          {issue.commentsCount > 0 && (
            <span className="inline-flex items-center gap-1 text-xs text-slate-400">
              <MessageSquare className="w-3.5 h-3.5" />
              {issue.commentsCount}
            </span>
          )}
        </div>

        {issue.assignee ? (
          <Avatar name={issue.assignee.name} size="sm" />
        ) : (
          <div
            className="w-6 h-6 rounded-full border border-dashed border-slate-300 flex items-center justify-center text-[10px] text-slate-400"
            title="Unassigned"
          >
            ?
          </div>
        )}
      </div>
    </div>
  );
};
