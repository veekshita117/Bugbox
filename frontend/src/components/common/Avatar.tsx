import React from 'react';

interface AvatarProps {
  name: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const colorMap = [
  'bg-indigo-100 text-indigo-700',
  'bg-emerald-100 text-emerald-700',
  'bg-amber-100 text-amber-700',
  'bg-sky-100 text-sky-700',
  'bg-rose-100 text-rose-700',
  'bg-purple-100 text-purple-700',
  'bg-teal-100 text-teal-700',
];

export const Avatar: React.FC<AvatarProps> = ({ name, size = 'md', className = '' }) => {
  const initials = name
    .split(' ')
    .map((p) => p[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase() || '?';

  // Deterministic color selection
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const colorClass = colorMap[Math.abs(hash) % colorMap.length];

  const sizeClasses = {
    sm: 'w-6 h-6 text-xs',
    md: 'w-8 h-8 text-xs',
    lg: 'w-10 h-10 text-sm font-semibold',
  }[size];

  return (
    <div
      className={`inline-flex items-center justify-center rounded-full font-medium shrink-0 select-none ${sizeClasses} ${colorClass} ${className}`}
      title={name}
    >
      {initials}
    </div>
  );
};
