import React from 'react';

interface SkeletonProps {
  className?: string;
  rows?: number;
}

export const Skeleton: React.FC<{ className?: string }> = ({ className = 'h-4 w-full' }) => (
  <div className={`bg-mist/40 animate-pulse rounded-btn ${className}`} />
);

export const TableSkeleton: React.FC<SkeletonProps> = ({ rows = 5 }) => (
  <div className="space-y-3 p-4">
    {Array.from({ length: rows }).map((_, i) => (
      <div key={i} className="flex items-center gap-4">
        <Skeleton className="h-4 w-12" />
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-4 w-28" />
        <Skeleton className="h-4 w-20" />
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-4 w-16" />
      </div>
    ))}
  </div>
);
