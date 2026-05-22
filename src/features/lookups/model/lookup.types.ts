export interface Currency {
  code: string;
  name: string;
  symbol: string | null;
  isActive: boolean;
}

export interface Country {
  code: string;
  name: string;
  dialCode: string | null;
  isActive: boolean;
}

export interface Province {
  id: number;
  countryCode: string;
  name: string;
  isActive: boolean;
}

export interface District {
  id: number;
  provinceId: number;
  name: string;
  isActive: boolean;
}
