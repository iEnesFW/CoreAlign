import { useQuery } from '@tanstack/react-query';
import { biApi } from '../api/biApi';
import type { BIQueryConfig, BIResult, DashboardWidget } from '../model/bi.types';

const EMPTY_QUERY_CONFIG: BIQueryConfig = {};

const parseQueryConfig = (raw: string | null | undefined): BIQueryConfig => {
  if (!raw) {
    return EMPTY_QUERY_CONFIG;
  }
  try {
    const parsed = JSON.parse(raw) as BIQueryConfig | null;
    return parsed ?? EMPTY_QUERY_CONFIG;
  } catch {
    return EMPTY_QUERY_CONFIG;
  }
};

export const useDashboardWidgetResult = (widget: DashboardWidget) =>
  useQuery({
    queryKey: ['bi', 'widget-result', widget.id, widget.queryConfigJson] as const,
    queryFn: async () => {
      const config = parseQueryConfig(widget.queryConfigJson);
      const [data, error] = await biApi.executeAdHoc(widget.dataSource, config);
      if (error) {
        throw error;
      }
      return data as BIResult;
    },
    staleTime: 5 * 60 * 1000,
    enabled: widget.isActive,
  });
