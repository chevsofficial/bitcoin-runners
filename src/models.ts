export type ThemeId =
  | 'dark'
  | 'light'
  | 'pastel'
  | 'neon'
  | 'amoled'
  | 'nature'
  | 'fall'
  | 'winter'
  | 'holiday';

export interface PomodoroSettings {
  /** Preferred theme for the Pomodoro timer UI. */
  themeId?: ThemeId;
}
