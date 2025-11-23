import { useMemo, useState } from 'react';

import { PomodoroSettings } from '../models';

export const defaultSettings: PomodoroSettings = {
  themeId: 'dark',
};

const usePersistentState = <T,>(initialValue: T) => {
  const [value] = useState<T>(initialValue);

  return useMemo(() => ({ value }), [value]);
};

export const useAppStore = () => {
  const { value: settings } = usePersistentState<PomodoroSettings>(defaultSettings);

  return useMemo(() => ({ settings }), [settings]);
};

export const useIsPro = () => {
  const { value: isPro } = usePersistentState(false);

  return isPro;
};
