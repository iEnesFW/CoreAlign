import React from 'react';
import { useLocalSearchParams } from 'expo-router';
import { ProjectDetailScreen } from '@/features/project/ui/ProjectDetailScreen';

const ProjectViewerRoute: React.FC = () => {
  const { id } = useLocalSearchParams<{ id: string }>();
  if (!id) return null;
  return <ProjectDetailScreen projectId={id} />;
};

export default ProjectViewerRoute;
