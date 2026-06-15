import { persistSignatureBase64 } from './fileSystem';

export interface SignatureCaptureResult {
  base64: string;
  storedUri: string;
  capturedAt: number;
}

export const stripDataUrl = (raw: string): string => raw.replace(/^data:image\/\w+;base64,/, '');

export const buildSignaturePayload = async (
  rawBase64: string,
  installationId: string,
): Promise<SignatureCaptureResult> => {
  const base64 = stripDataUrl(rawBase64);
  const storedUri = await persistSignatureBase64(base64, installationId);
  return {
    base64,
    storedUri,
    capturedAt: Date.now(),
  };
};

const SIGNATURE_WEBVIEW_STYLE = `
  .m-signature-pad { box-shadow: none; border: none; }
  .m-signature-pad--body { border: 1px dashed #94a3b8; border-radius: 12px; }
  .m-signature-pad--footer { display: none; margin: 0; }
  body, html { background: transparent; }
`;

export const signatureWebViewStyle = SIGNATURE_WEBVIEW_STYLE;
