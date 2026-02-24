import { useEffect, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useVerifyEmail } from '@/features/auth/hooks/useAuth';
import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { Loader2 } from 'lucide-react';

export const VerifyEmailPage = () => {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');
    const navigate = useNavigate();
    const mutation = useVerifyEmail();
    const { mutate: verifyEmail, isPending, isError, isSuccess, error } = mutation;
    const hasVerified = useRef(false);

    useEffect(() => {
        console.log("VerifyEmailPage mounted. Token:", token);
        if (token && !hasVerified.current) {
            console.log("Calling verifyEmail mutation...");
            hasVerified.current = true;
            verifyEmail({ token }, {
                onSuccess: (data) => {
                    console.log("Verification SUCCESS:", data);
                    setTimeout(() => navigate('/login'), 3000);
                },
                onError: (err: any) => {
                    console.error("Verification FAILED:", err);
                    console.error("Error Response Data:", err.response?.data);
                    console.error("Error Status:", err.response?.status);
                },
                onSettled: () => {
                    console.log("Verification mutation settled (finished).");
                }
            });
        }
    }, [token, verifyEmail, navigate]);

    useEffect(() => {
        console.log("Render State -> isPending:", isPending, "isSuccess:", isSuccess, "isError:", isError, "Error:", error);
    }, [isPending, isSuccess, isError, error]);

    if (!token) {
        return (
            <AuthLayout>
                <div className="flex flex-col items-center justify-center p-8 text-center">
                    <h1 className="text-2xl font-bold mb-2 text-red-500">Hata</h1>
                    <p className="text-muted-foreground mb-4">Geçersiz doğrulama bağlantısı.</p>
                    <div className="text-sm">Token bulunamadı.</div>
                    <button
                        onClick={() => navigate('/login')}
                        className="mt-6 px-4 py-2 bg-primary text-white rounded hover:bg-primary/90"
                    >
                        Giriş Yap
                    </button>
                </div>
            </AuthLayout>
        );
    }

    const errorMessage = (error as any)?.response?.data?.errors?.[0] ||
        (error as any)?.message ||
        "Token geçersiz veya süresi dolmuş.";

    return (
        <AuthLayout>
            <div className="flex flex-col items-center justify-center p-8 text-center">
                <h1 className="text-2xl font-bold mb-2">
                    {isSuccess ? "E-posta Doğrulandı" : isError ? "Doğrulama Başarısız" : "Doğrulanıyor..."}
                </h1>
                <p className="text-muted-foreground mb-8">
                    {isSuccess ?
                        "Hesabınız başarıyla doğrulandı. Giriş sayfasına yönlendiriliyorsunuz..." :
                        isError ? "E-posta doğrulama işlemi sırasında bir hata oluştu." :
                            "Lütfen bekleyin, e-posta adresiniz doğrulanıyor."}
                </p>

                {isPending && (
                    <Loader2 className="h-12 w-12 animate-spin text-primary" />
                )}

                {isSuccess && (
                    <div className="text-green-500">
                        <svg className="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                        <p className="font-medium">İşlem başarılı!</p>
                    </div>
                )}

                {isError && (
                    <div className="text-red-500">
                        <svg className="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                        <p className="mb-4 font-medium">{errorMessage}</p>
                        <button
                            onClick={() => navigate('/login')}
                            className="px-4 py-2 bg-primary text-white rounded hover:bg-primary/90"
                        >
                            Giriş Yap
                        </button>
                    </div>
                )}
            </div>
        </AuthLayout>
    );
};
