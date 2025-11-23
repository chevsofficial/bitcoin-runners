import { ReactNode, createContext, useContext, useMemo } from 'react';

import { useAppStore, useIsPro } from '../store/appStore';
import { AppTheme, THEMES } from './themes';

const ThemeContext = createContext<AppTheme>(THEMES.dark);

export const ThemeProvider = ({ children }: { children: ReactNode }) => {
  const { settings } = useAppStore();
  const isPro = useIsPro();

  const theme = useMemo(() => {
    const selectedThemeId = settings.themeId ?? 'dark';
    const selectedTheme = THEMES[selectedThemeId] ?? THEMES.dark;

    if (selectedTheme.isPro && !isPro) {
      return THEMES.dark;
    }

    return selectedTheme;
  }, [settings.themeId, isPro]);

  return <ThemeContext.Provider value={theme}>{children}</ThemeContext.Provider>;
};

export const useTheme = () => useContext(ThemeContext);

export const useThemeColors = () => {
  const theme = useTheme();

  return useMemo(
    () => ({
      background: theme.background,
      surface: theme.surface,
      primary: theme.primary,
      secondary: theme.secondary,
      accent: theme.accent,
      textPrimary: theme.textPrimary,
      textSecondary: theme.textSecondary,
      border: theme.border,
      muted: theme.muted,
    }),
    [theme],
  );
};
