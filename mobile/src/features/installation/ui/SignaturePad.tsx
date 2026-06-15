import React, { useRef, useState } from 'react';
import { Alert, Pressable, Text, TextInput, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import SignatureCanvas, { type SignatureViewRef } from 'react-native-signature-canvas';
import { buildSignaturePayload, signatureWebViewStyle } from '@/shared/native/signature';
import type { DraftSignature } from '../store/installationStore';

interface SignaturePadProps {
  installationId: string;
  signature: DraftSignature | null;
  onCaptured: (signature: DraftSignature) => void;
  onClear: () => void;
}

export const SignaturePad: React.FC<SignaturePadProps> = ({
  installationId,
  signature,
  onCaptured,
  onClear,
}) => {
  const { t } = useTranslation();
  const ref = useRef<SignatureViewRef>(null);
  const [signerName, setSignerName] = useState(signature?.signerName ?? '');
  const [signerRole, setSignerRole] = useState(signature?.signerRole ?? '');
  const [busy, setBusy] = useState(false);

  const handleConfirm = async (raw: string) => {
    if (!signerName.trim()) {
      Alert.alert(t('installation.signerNameRequired'));
      return;
    }
    setBusy(true);
    try {
      const result = await buildSignaturePayload(raw, installationId);
      onCaptured({
        signerName: signerName.trim(),
        signerRole: signerRole.trim() || null,
        base64: result.base64,
        capturedAt: result.capturedAt,
      });
    } catch (err) {
      Alert.alert('Signature', err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleSavePress = () => {
    ref.current?.readSignature();
  };

  const handleClearPress = () => {
    ref.current?.clearSignature();
    onClear();
  };

  return (
    <View className="mb-4">
      <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
        {t('installation.captureSignature')}
      </Text>
      <TextInput
        accessibilityLabel={t('installation.signerName')}
        value={signerName}
        onChangeText={setSignerName}
        placeholder={t('installation.signerName')}
        className="min-h-touch rounded-xl bg-surface-muted dark:bg-brand-700 px-4 mb-2 text-base text-brand-900 dark:text-white"
      />
      <TextInput
        accessibilityLabel={t('installation.signerRole')}
        value={signerRole}
        onChangeText={setSignerRole}
        placeholder={t('installation.signerRole')}
        className="min-h-touch rounded-xl bg-surface-muted dark:bg-brand-700 px-4 mb-3 text-base text-brand-900 dark:text-white"
      />
      <View className="h-56 rounded-2xl overflow-hidden bg-white border border-slate-200">
        <SignatureCanvas
          ref={ref}
          onOK={(raw) => void handleConfirm(raw)}
          webStyle={signatureWebViewStyle}
          descriptionText=""
          androidHardwareAccelerationDisabled
          autoClear={false}
          imageType="image/png"
        />
      </View>
      <View className="flex-row mt-3">
        <Pressable
          accessibilityRole="button"
          onPress={handleClearPress}
          disabled={busy}
          className="flex-1 mr-2 min-h-touch-lg rounded-2xl bg-surface-muted dark:bg-brand-700 items-center justify-center"
        >
          <Text className="text-base font-semibold text-brand-900 dark:text-white">
            {'↺'} {t('common.cancel')}
          </Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={handleSavePress}
          disabled={busy}
          className={`flex-1 min-h-touch-lg rounded-2xl bg-success items-center justify-center ${busy ? 'opacity-50' : ''}`}
        >
          <Text className="text-base font-bold text-white">
            {'✓'} {t('common.save')}
          </Text>
        </Pressable>
      </View>
      {signature ? (
        <Text className="mt-2 text-sm text-success">
          {'✓'} {signature.signerName}
        </Text>
      ) : null}
    </View>
  );
};
