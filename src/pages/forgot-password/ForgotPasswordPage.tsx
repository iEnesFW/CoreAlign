import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { ForgotPasswordForm } from '@/features/auth/ui/ForgotPasswordForm/ForgotPasswordForm';

export const ForgotPasswordPage = () => {
    return (
        <AuthLayout>
            <ForgotPasswordForm />
        </AuthLayout>
    );
};
