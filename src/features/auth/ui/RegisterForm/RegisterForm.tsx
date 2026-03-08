import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import styles from './RegisterForm.module.css';
import { Mail, Lock, User, UserCheck } from 'lucide-react';
import { useRegister } from '../../hooks/useAuth';
import { Link } from 'react-router-dom';
import type { RegisterRequest } from '../../model/auth.types';
import type { AxiosError } from 'axios';
import type { ApiResponse } from '../../model/auth.types';

export const RegisterForm = () => {
    const { t } = useTranslation();
    const [formData, setFormData] = useState<RegisterRequest>({ username: '', email: '', password: '', firstName: '', lastName: '' });
    const [confirmPassword, setConfirmPassword] = useState('');
    const [serverError, setServerError] = useState<string | null>(null);
    const [isRegistered, setIsRegistered] = useState(false);
    const registerMutation = useRegister();
    const { executeRecaptcha } = useGoogleReCaptcha();

    const handleChange = useCallback((field: keyof RegisterRequest) => (e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData((prev) => ({ ...prev, [field]: e.target.value }));
    }, []);

    const handleConfirmPasswordChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setConfirmPassword(e.target.value);
    }, []);

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        setServerError(null);

        if (formData.password !== confirmPassword) {
            setServerError(t('auth.register.errors.passwordMismatch'));
            return;
        }

        const captchaToken = await executeRecaptcha?.('register');

        const enrichedData: RegisterRequest = {
            ...formData,
            captchaToken: captchaToken ?? undefined,
        };

        registerMutation.mutate(enrichedData, {
            onSuccess: (response) => {
                if (response.isSuccess) {
                    setIsRegistered(true);
                } else {
                    setServerError(response.errors[0] || t('auth.register.errors.registerFailed'));
                }
            },
            onError: (error: Error) => {
                const axiosError = error as AxiosError<ApiResponse<unknown>>;
                const message = axiosError.response?.data?.errors?.[0] || t('auth.common.unexpectedError');
                setServerError(message);
            },
        });
    }, [formData, confirmPassword, registerMutation, t, executeRecaptcha]);

    if (isRegistered) {
        return (
            <div className={styles.form}>
                <div className={styles.header}>
                    <div className={styles.logoWrapper}>
                        <Logo size={42} />
                    </div>
                    <div className={styles.successMessage}>
                        <UserCheck size={48} strokeWidth={1.5} />
                        <h2>{t('auth.register.success.title')}</h2>
                        <p>{t('auth.register.success.message')}</p>
                        <Link to="/login" className={styles.link}>{t('auth.register.success.backToLogin')}</Link>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <form className={styles.form} onSubmit={handleSubmit}>
            <div className={styles.header}>
                <div className={styles.logoWrapper}>
                    <Logo size={42} />
                </div>
                <p className={styles.subtitle}>{t('auth.register.subtitle')}</p>
            </div>

            {serverError && <div className={styles.errorBanner}>{serverError}</div>}

            <div className={styles.fields}>
                <div className={styles.row}>
                    <Input
                        label={t('auth.register.firstNameLabel')}
                        placeholder={t('auth.register.firstNamePlaceholder')}
                        type="text"
                        value={formData.firstName || ''}
                        onChange={handleChange('firstName')}
                    />
                    <Input
                        label={t('auth.register.lastNameLabel')}
                        placeholder={t('auth.register.lastNamePlaceholder')}
                        type="text"
                        value={formData.lastName || ''}
                        onChange={handleChange('lastName')}
                    />
                </div>

                <Input
                    label={t('auth.register.usernameLabel')}
                    placeholder={t('auth.register.usernamePlaceholder')}
                    type="text"
                    leftIcon={<User size={18} />}
                    value={formData.username}
                    onChange={handleChange('username')}
                />

                <Input
                    label={t('auth.register.emailLabel')}
                    placeholder={t('auth.register.emailPlaceholder')}
                    type="email"
                    leftIcon={<Mail size={18} />}
                    value={formData.email}
                    onChange={handleChange('email')}
                />

                <Input
                    label={t('auth.register.passwordLabel')}
                    placeholder={t('auth.register.passwordPlaceholder')}
                    type="password"
                    leftIcon={<Lock size={18} />}
                    value={formData.password}
                    onChange={handleChange('password')}
                />

                <Input
                    label={t('auth.register.confirmPasswordLabel')}
                    placeholder={t('auth.register.confirmPasswordPlaceholder')}
                    type="password"
                    leftIcon={<Lock size={18} />}
                    value={confirmPassword}
                    onChange={handleConfirmPasswordChange}
                />
            </div>

            <div className={styles.actions}>
                <Button type="submit" isLoading={registerMutation.isPending} className={styles.submitButton}>
                    {t('auth.register.submitButton')}
                </Button>
            </div>

            <div className={styles.footer}>
                {t('auth.register.haveAccountText')} <Link to="/login" className={styles.link}>{t('auth.register.loginLinkText')}</Link>
            </div>
        </form>
    );
};
