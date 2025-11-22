export type ThemeId = 'dark' | 'light';

export interface PomodoroSettings {
  /** Preferred theme for the Pomodoro timer UI. */
  themeId?: ThemeId;
}
