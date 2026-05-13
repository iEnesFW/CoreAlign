import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { RegisterForm } from '@/features/auth/ui/RegisterForm/RegisterForm';

export const RegisterPage = () => {
  return (
    <AuthLayout>
      <RegisterForm />
    </AuthLayout>
  );
};
