import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import styles from './LoginForm.module.css';
import { Mail, Lock } from 'lucide-react';
import { useLogin } from '../../hooks/useAuth';
import { useNavigate, Link } from 'react-router-dom';
import type { LoginRequest } from '../../model/auth.types';
import type { AxiosError } from 'axios';
import type { ApiResponse } from '../../model/auth.types';

export const LoginForm = () => {
    const { t } = useTranslation();
    const [formData, setFormData] = useState<LoginRequest>({ email: '', password: '' });
    const [serverError, setServerError] = useState<string | null>(null);
    const navigate = useNavigate();
    const loginMutation = useLogin();

    const handleEmailChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData((prev) => ({ ...prev, email: e.target.value }));
    }, []);

    const handlePasswordChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData((prev) => ({ ...prev, password: e.target.value }));
    }, []);

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        setServerError(null);

        loginMutation.mutate(formData, {
            onSuccess: (response) => {
                if (response.isSuccess) {
                    navigate('/dashboard');
                } else {
                    setServerError(response.errors[0] || t('auth.login.errors.loginFailed'));
                }
            },
            onError: (error: Error) => {
                const axiosError = error as AxiosError<ApiResponse<unknown>>;
                const message = axiosError.response?.data?.errors?.[0] || t('auth.common.unexpectedError');
                setServerError(message);
            },
        });
    }, [formData, loginMutation, navigate, t]);

    return (
        <form className={styles.form} onSubmit={handleSubmit}>
            <div className={styles.header}>
                <div className={styles.logoWrapper}>
                    <Logo size={42} />
                </div>
                <p className={styles.subtitle}>{t('auth.login.subtitle')}</p>
            </div>

            {serverError && <div className={styles.errorBanner}>{serverError}</div>}

            <div className={styles.fields}>
                <Input
                    label={t('auth.login.emailLabel')}
                    placeholder={t('auth.login.emailPlaceholder')}
                    type="email"
                    leftIcon={<Mail size={18} />}
                    value={formData.email}
                    onChange={handleEmailChange}
                />

                <Input
                    label={t('auth.login.passwordLabel')}
                    placeholder={t('auth.login.passwordPlaceholder')}
                    type="password"
                    leftIcon={<Lock size={18} />}
                    value={formData.password}
                    onChange={handlePasswordChange}
                />
            </div>

            <div className={styles.actions}>
                <Link to="/forgot-password" className={styles.forgotPassword}>{t('auth.login.forgotPassword')}</Link>
                <Button type="submit" isLoading={loginMutation.isPending} className={styles.submitButton}>
                    {t('auth.login.submitButton')}
                </Button>
            </div>

            <div className={styles.footer}>
                {t('auth.login.noAccount')} <Link to="/register" className={styles.link}>{t('auth.login.registerLink')}</Link>
            </div>
        </form>
    );
};
