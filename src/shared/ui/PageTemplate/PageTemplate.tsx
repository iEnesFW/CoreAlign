import React from 'react';
import { cn } from '@/shared/lib/cn';

interface ListPageTemplateProps {
  header: React.ReactNode;
  toolbar?: React.ReactNode;
  pagination?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

export const ListPageTemplate = ({
  header,
  toolbar,
  pagination,
  children,
  className,
}: ListPageTemplateProps) => (
  <div className={cn('flex flex-col gap-4 p-4 sm:p-6', className)}>
    {header}
    {toolbar}
    {children}
    {pagination}
  </div>
);

interface DetailPageTemplateProps {
  header: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

export const DetailPageTemplate = ({ header, children, className }: DetailPageTemplateProps) => (
  <div className={cn('flex flex-col gap-4 p-4 sm:p-6', className)}>
    {header}
    {children}
  </div>
);
