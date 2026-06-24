export const getErroredTabs = (
  errors: Record<string, unknown>,
  fieldTab: Record<string, string>,
): Set<string> => {
  const tabs = new Set<string>();
  for (const field of Object.keys(errors)) {
    const tab = fieldTab[field];
    if (tab) tabs.add(tab);
  }
  return tabs;
};

export const firstErroredTab = (
  errors: Record<string, unknown>,
  fieldTab: Record<string, string>,
  order: string[],
): string | undefined => order.find((tab) => getErroredTabs(errors, fieldTab).has(tab));
