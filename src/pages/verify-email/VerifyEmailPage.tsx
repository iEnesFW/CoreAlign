import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { Loader2, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Logo } from '@/shared/ui/Logo/Logo';
import { apiClient } from '@/shared/api/apiClient';
import styles from '@/features/auth/ui/ResetPasswordForm/ResetPasswordForm.module.css';

export const VerifyEmailPage = () => {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');
    const navigate = useNavigate();

    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [pageErrorMessage, setPageErrorMessage] = useState<string>('');
    const hasVerified = useRef(false);

    useEffect(() => {
        if (!token || hasVerified.current) return;

        const verify = async () => {
            hasVerified.current = true;
            setStatus('loading');
            try {
                const response = await apiClient.post('/auth/verify-email', { token });
                if (response.data?.isSuccess) {
                    setStatus('success');
                    setTimeout(() => navigate('/login'), 4000);
                } else {
                    setStatus('error');
                    setPageErrorMessage(response.data?.message || 'Doğrulama başarısız.');
                }
            } catch (catchErr: unknown) {
                console.error("Direct fetch failed:", catchErr);
                setStatus('error');
                if (axios.isAxiosError(catchErr)) {
                    setPageErrorMessage(
                        catchErr.response?.data?.errors?.[0] ||
                        catchErr.response?.data?.message ||
                        catchErr.message ||
                        'Token geçersiz veya süresi dolmuş.'
                    );
                } else if (catchErr instanceof Error) {
                    setPageErrorMessage(catchErr.message);
                } else {
                    setPageErrorMessage('Token geçersiz veya süresi dolmuş.');
                }
            }
        };

        verify();
    }, [token, navigate]);

    const renderContent = () => {
        if (!token) {
            return (
                <div className={styles.successMessage}>
                    <AlertCircle size={56} strokeWidth={1.5} className="text-red-400 mb-4" />
                    <h2 className="text-2xl font-semibold text-white mb-2">Geçersiz Bağlantı</h2>
                    <p className="text-gray-400 mb-6">Token bulunamadı veya link eksik kopyalanmış olabilir.</p>
                    <Button onClick={() => navigate('/login')} className={styles.submitButton}>
                        Giriş Sayfasına Dön
                    </Button>
                </div>
            );
        }

        if (status === 'loading' || status === 'idle') {
            return (
                <div className={styles.successMessage}>
                    <Loader2 size={56} strokeWidth={1.5} className="text-primary animate-spin mb-4" />
                    <h2 className="text-2xl font-semibold text-white mb-2">Hesap Doğrulanıyor</h2>
                    <p className="text-gray-400">Lütfen bekleyin, e-posta adresiniz onaylanıyor...</p>
                </div>
            );
        }

        if (status === 'success') {
            return (
                <div className={styles.successMessage}>
                    <CheckCircle2 size={56} strokeWidth={1.5} className="text-green-400 mb-4" />
                    <h2 className="text-2xl font-semibold text-white mb-2">E-posta Doğrulandı!</h2>
                    <p className="text-gray-400 mb-6">Hesabınız başarıyla aktifleştirildi. Giriş sayfasına yönlendiriliyorsunuz...</p>
                    <Button onClick={() => navigate('/login')} className={styles.submitButton}>
                        Hemen Giriş Yap
                    </Button>
                </div>
            );
        }

        return (
            <div className={styles.successMessage}>
                <XCircle size={56} strokeWidth={1.5} className="text-red-400 mb-4" />
                <h2 className="text-2xl font-semibold text-white mb-2">Doğrulama Başarısız</h2>
                <p className="text-red-300/80 mb-6 text-sm">{pageErrorMessage}</p>
                <Button onClick={() => navigate('/login')} className={styles.submitButton}>
                    Giriş Sayfasına Dön
                </Button>
            </div>
        );
    };

    return (
        <AuthLayout>
            <div className={styles.form} style={{ border: 'none', background: 'transparent', boxShadow: 'none' }}>
                <div className={styles.header}>
                    <div className={styles.logoWrapper}>
                        <Logo size={42} />
                    </div>
                </div>
                {renderContent()}
            </div>
        </AuthLayout>
    );
};
