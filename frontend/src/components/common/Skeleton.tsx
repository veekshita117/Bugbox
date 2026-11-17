import React from 'react';

export const Skeleton: React.FC<{ className?: string }> = ({ className = '' }) => {
  return <div className={`animate-pulse bg-slate-200/80 rounded-md ${className}`} />;
};

export const CardSkeleton: React.FC = () => {
  return (
    <div className="bg-white p-5 rounded-2xl border border-slate-100 shadow-sm space-y-3">
      <div className="flex items-center justify-between">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-5 w-14 rounded-full" />
      </div>
      <Skeleton className="h-5 w-3/4" />
      <Skeleton className="h-4 w-full" />
      <div className="pt-2 flex justify-between items-center">
        <Skeleton className="h-6 w-16" />
        <Skeleton className="h-7 w-7 rounded-full" />
      </div>
    </div>
  );
};
