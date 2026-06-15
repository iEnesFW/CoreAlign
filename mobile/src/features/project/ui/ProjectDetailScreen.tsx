import React from 'react';
import { Image, ScrollView, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { projectApi, type GlassRun, type ProjectPanel } from '../api/projectApi';

interface ProjectDetailScreenProps {
  projectId: string;
}

const formatMm = (value: number): string => `${Math.round(value)} mm`;
const formatArea = (value: number): string => `${(value / 1_000_000).toFixed(2)} m²`;

const STATUS_COLOR: Record<GlassRun['status'], string> = {
  Planned: 'bg-slate-400',
  Cutting: 'bg-warning',
  Ready: 'bg-brand-600',
  Installing: 'bg-brand-500',
  Installed: 'bg-success',
};

const RunRow: React.FC<{ run: GlassRun }> = ({ run }) => (
  <View className="mb-2 rounded-xl bg-white dark:bg-brand-900 p-3 flex-row items-center">
    <View className={`w-3 h-12 rounded-full mr-3 ${STATUS_COLOR[run.status]}`} />
    <View className="flex-1">
      <Text className="text-base font-bold text-brand-900 dark:text-white">{run.code}</Text>
      <Text className="text-xs text-slate-500 dark:text-slate-300 mt-1">
        {run.panelCount} {'🪟'} · {formatMm(run.totalWidthMm)} × {formatMm(run.totalHeightMm)} ·{' '}
        {formatMm(run.thicknessMm)}
      </Text>
    </View>
    <Text className="text-xs text-slate-500">{run.status}</Text>
  </View>
);

const PanelRow: React.FC<{ panel: ProjectPanel }> = ({ panel }) => (
  <View className="mb-2 rounded-xl bg-surface-muted p-3">
    <Text className="text-sm font-semibold text-brand-900">{panel.code}</Text>
    <Text className="text-xs text-slate-500 mt-1">
      {formatMm(panel.widthMm)} × {formatMm(panel.heightMm)} · {formatMm(panel.thicknessMm)}
    </Text>
    {panel.notes ? <Text className="text-xs text-slate-400 mt-1">{panel.notes}</Text> : null}
  </View>
);

export const ProjectDetailScreen: React.FC<ProjectDetailScreenProps> = ({ projectId }) => {
  const { t } = useTranslation();
  const query = useQuery({
    queryKey: ['projects', 'detail', projectId],
    queryFn: () => projectApi.getById(projectId),
    staleTime: 5 * 60_000,
  });

  if (query.isLoading) {
    return (
      <Screen>
        <Text className="text-base text-slate-500">{t('common.loading')}</Text>
      </Screen>
    );
  }

  if (!query.data) {
    return (
      <Screen>
        <Text className="text-base text-danger">{t('project.notFound')}</Text>
      </Screen>
    );
  }

  const project = query.data;
  const summary = project.dimensionSummary;

  return (
    <Screen>
      <ScrollView showsVerticalScrollIndicator={false}>
        <Text className="text-2xl font-bold text-brand-900 dark:text-white">{project.code}</Text>
        <Text className="text-base text-slate-500 dark:text-slate-300 mt-1">
          {project.customerName}
        </Text>
        <Text className="text-sm text-slate-400 mt-1">{project.siteAddress}</Text>

        {project.planImageUrl ? (
          <View className="my-4">
            <Text className="text-sm font-semibold text-brand-900 dark:text-white mb-2">
              {t('project.plan')}
            </Text>
            <Image
              source={{ uri: project.planImageUrl }}
              className="w-full h-64 rounded-2xl bg-surface-muted"
              resizeMode="contain"
            />
          </View>
        ) : null}

        <View className="my-4 rounded-2xl bg-white dark:bg-brand-900 p-4">
          <Text className="text-sm font-semibold text-brand-900 dark:text-white mb-2">
            {t('project.summary')}
          </Text>
          <View className="flex-row flex-wrap">
            <View className="w-1/2 mb-2">
              <Text className="text-xs text-slate-400">{t('project.runs')}</Text>
              <Text className="text-base font-bold text-brand-900 dark:text-white">
                {summary.totalRunCount}
              </Text>
            </View>
            <View className="w-1/2 mb-2">
              <Text className="text-xs text-slate-400">{t('project.panels')}</Text>
              <Text className="text-base font-bold text-brand-900 dark:text-white">
                {summary.totalPanelCount}
              </Text>
            </View>
            <View className="w-1/2 mb-2">
              <Text className="text-xs text-slate-400">{t('project.totalArea')}</Text>
              <Text className="text-base font-bold text-brand-900 dark:text-white">
                {formatArea(summary.totalGlassArea)}
              </Text>
            </View>
            <View className="w-1/2 mb-2">
              <Text className="text-xs text-slate-400">{t('project.largestPanel')}</Text>
              <Text className="text-base font-bold text-brand-900 dark:text-white">
                {formatMm(summary.largestPanelMm.width)} × {formatMm(summary.largestPanelMm.height)}
              </Text>
            </View>
          </View>
        </View>

        <Text className="text-base font-semibold text-brand-900 dark:text-white mt-2 mb-2">
          {t('project.runs')}
        </Text>
        {project.runs.map((run) => (
          <RunRow key={run.id} run={run} />
        ))}

        <Text className="text-base font-semibold text-brand-900 dark:text-white mt-3 mb-2">
          {t('project.panels')}
        </Text>
        {project.panels.map((panel) => (
          <PanelRow key={panel.id} panel={panel} />
        ))}
        <View className="h-12" />
      </ScrollView>
    </Screen>
  );
};
