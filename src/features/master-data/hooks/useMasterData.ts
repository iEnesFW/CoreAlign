import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  masterDataApi,
  type BrandInput,
  type CustomerGroupInput,
  type PaymentTermInput,
  type PriceListInput,
  type ProductCategoryInput,
  type TaxRateInput,
  type UnitOfMeasureInput,
  type WarehouseInput,
  type WarehouseUpdateInput,
} from '../api/masterDataApi';

const FIVE_MINUTES = 5 * 60 * 1000;

export const useBrandsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'brands', { isActive }] as const,
    queryFn: () => masterDataApi.brands.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCategoriesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'categories', { isActive }] as const,
    queryFn: () => masterDataApi.categories.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCustomerGroupsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'customer-groups', { isActive }] as const,
    queryFn: () => masterDataApi.customerGroups.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useUomsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'units-of-measure', { isActive }] as const,
    queryFn: () => masterDataApi.uoms.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useTaxRatesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'tax-rates', { isActive }] as const,
    queryFn: () => masterDataApi.taxRates.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const usePaymentTermsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'payment-terms', { isActive }] as const,
    queryFn: () => masterDataApi.paymentTerms.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const usePriceListsQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'price-lists', { isActive }] as const,
    queryFn: () => masterDataApi.priceLists.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useWarehousesQuery = (isActive?: boolean) =>
  useQuery({
    queryKey: ['master-data', 'warehouses', { isActive }] as const,
    queryFn: () => masterDataApi.warehouses.list(isActive),
    staleTime: FIVE_MINUTES,
  });

export const useCreatePaymentTerm = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: PaymentTermInput) => masterDataApi.paymentTerms.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'payment-terms'] }),
  });
};

export const useCreatePriceList = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: PriceListInput) => masterDataApi.priceLists.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'price-lists'] }),
  });
};

export const useCreateCustomerGroup = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CustomerGroupInput) => masterDataApi.customerGroups.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'customer-groups'] }),
  });
};

export const useCreateBrand = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: BrandInput) => masterDataApi.brands.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'brands'] }),
  });
};

export const useCreateProductCategory = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ProductCategoryInput) => masterDataApi.categories.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'categories'] }),
  });
};

export const useCreateUnitOfMeasure = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UnitOfMeasureInput) => masterDataApi.uoms.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'units-of-measure'] }),
  });
};

export const useSeedStandardUoms = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => masterDataApi.uoms.seedStandard(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'units-of-measure'] }),
  });
};

const invalidateWarehouses = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['master-data', 'warehouses'] });
  qc.invalidateQueries({ queryKey: ['inventory'] });
};

export const useCreateWarehouse = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: WarehouseInput) => masterDataApi.warehouses.create(input),
    onSuccess: () => invalidateWarehouses(qc),
  });
};

export const useUpdateWarehouse = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: WarehouseUpdateInput) => masterDataApi.warehouses.update(input),
    onSuccess: () => invalidateWarehouses(qc),
  });
};

export const useDeleteWarehouse = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => masterDataApi.warehouses.remove(id),
    onSuccess: () => invalidateWarehouses(qc),
  });
};

export const useCreateTaxRate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: TaxRateInput) => masterDataApi.taxRates.create(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-data', 'tax-rates'] }),
  });
};
