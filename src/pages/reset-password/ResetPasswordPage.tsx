import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { ResetPasswordForm } from '@/features/auth/ui/ResetPasswordForm';

export const ResetPasswordPage = () => {
    return (
        <AuthLayout>
            <ResetPasswordForm />
        </AuthLayout>
    );
};
