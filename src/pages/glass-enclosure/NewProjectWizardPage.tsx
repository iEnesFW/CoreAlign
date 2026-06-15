import { useNavigate } from 'react-router-dom';
import { NewProjectWizard } from '@/features/glass-enclosure/wizard/ui/NewProjectWizard';

export function NewProjectWizardPage() {
  const navigate = useNavigate();
  return (
    <NewProjectWizard isOpen onClose={() => navigate('/dashboard/glass-enclosure/projects')} />
  );
}

export default NewProjectWizardPage;
