import { useCallback, useEffect, useMemo, useState } from 'react';

import { PomodoroSettings } from '../models';

export const defaultSettings: PomodoroSettings = {
  themeId: 'dark',
};

const usePersistentState = <T,>(key: string, initialValue: T) => {
  const [value, setValue] = useState<T>(() => {
    if (typeof window === 'undefined' || !window.localStorage) {
      return initialValue;
    }

    const storedValue = window.localStorage.getItem(key);

    if (storedValue) {
      try {
        return JSON.parse(storedValue) as T;
      } catch (error) {
        console.warn(`Failed to parse persisted value for ${key}`, error);
      }
    }

    return initialValue;
  });

  const setPersistentValue = useCallback(
    (nextValue: T | ((previousValue: T) => T)) => {
      setValue((previousValue) =>
        typeof nextValue === 'function'
          ? (nextValue as (currentValue: T) => T)(previousValue)
          : nextValue,
      );
    },
    [],
  );

  useEffect(() => {
    if (typeof window === 'undefined' || !window.localStorage) {
      return;
    }

    window.localStorage.setItem(key, JSON.stringify(value));
  }, [key, value]);

  return useMemo(() => ({ value, setValue: setPersistentValue }), [value, setPersistentValue]);
};

export const useAppStore = () => {
  const { value: settings, setValue: setSettings } = usePersistentState<PomodoroSettings>(
    'app-settings',
    defaultSettings,
  );

  return useMemo(() => ({ settings, setSettings }), [settings, setSettings]);
};

export const useIsPro = () => {
  const { value: isPro } = usePersistentState('app-is-pro', false);

  return isPro;
};

export const useProStatus = () => {
  const { value: isPro, setValue: setIsPro } = usePersistentState('app-is-pro', false);

  return useMemo(() => ({ isPro, setIsPro }), [isPro, setIsPro]);
};
