import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { LoginForm } from '@/features/auth/ui/LoginForm/LoginForm';

export const LoginPage = () => {
    return (
        <AuthLayout>
            <LoginForm />
        </AuthLayout>
    );
};
