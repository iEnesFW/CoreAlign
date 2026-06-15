import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { biApi } from '../api/biApi';
import type { DashboardWidget, DashboardWidgetUpsert } from '../model/bi.types';

const DASHBOARD_KEY = ['bi', 'dashboard'] as const;

export const useDashboardQuery = (enabled = true) =>
  useQuery({
    queryKey: DASHBOARD_KEY,
    queryFn: async () => {
      const [data, error] = await biApi.getDashboard();
      if (error) {
        throw error;
      }
      return (data ?? []) as DashboardWidget[];
    },
    enabled,
    staleTime: 30 * 1000,
  });

export const useSaveDashboardLayout = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (widgets: DashboardWidgetUpsert[]) => {
      const [, error] = await biApi.saveLayout(widgets);
      if (error) {
        throw error;
      }
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DASHBOARD_KEY }),
  });
};

export const useAddDashboardWidget = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (widget: DashboardWidgetUpsert) => {
      const [data, error] = await biApi.addWidget(widget);
      if (error) {
        throw error;
      }
      return data as DashboardWidget;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DASHBOARD_KEY }),
  });
};

export const useRemoveDashboardWidget = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const [, error] = await biApi.removeWidget(id);
      if (error) {
        throw error;
      }
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: DASHBOARD_KEY }),
  });
};
