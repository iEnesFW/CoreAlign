export interface AddressSnapshot {
  label?: string | null;
  recipientName?: string | null;
  phone?: string | null;
  line1: string;
  line2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface CustomerSnapshot {
  code?: string | null;
  legalName: string;
  tradeName?: string | null;
  taxNumber?: string | null;
  taxOffice?: string | null;
  nationalId?: string | null;
  email?: string | null;
  phone?: string | null;
}
