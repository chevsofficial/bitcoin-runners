import React from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

import { useThemeColors } from './theme/ThemeProvider';

const spacing = {
  sm: 8,
  md: 12,
  lg: 20,
};

const featureRows = [
  {
    icon: '🌟',
    title: 'Premium focus modes',
    description: 'Stay in the zone with advanced Pomodoro options.',
  },
  {
    icon: '🎨',
    title: 'Unlock every theme',
    description: 'Match your timer to any mood with pro palettes.',
  },
  {
    icon: '☁️',
    title: 'Cloud sync',
    description: 'Keep your sessions and tasks consistent across devices.',
  },
  {
    icon: '✅',
    title: 'Export all data (.CSV)',
    description: 'Back up your tasks and focus history.',
  },
];

const ProUpsellScreen = () => {
  const colors = useThemeColors();

  return (
    <ScrollView
      style={[styles.container, { backgroundColor: colors.background }]}
      contentContainerStyle={styles.content}
    >
      <View style={styles.hero}>
        <Text style={[styles.kicker, { color: colors.accent }]}>Upgrade to Pro</Text>
        <Text style={[styles.title, { color: colors.textPrimary }]}>More power. More focus.</Text>
        <Text style={[styles.subtitle, { color: colors.textSecondary }]}>Unlock premium tools to stay organized and keep momentum.</Text>
      </View>

      <View style={styles.featureList}>
        {featureRows.map((feature) => (
          <View
            key={feature.title}
            style={[styles.featureRow, { backgroundColor: colors.surface, borderColor: colors.border }]}
          >
            <View style={[styles.iconBadge, { backgroundColor: colors.muted }]}>
              <Text style={styles.icon}>{feature.icon}</Text>
            </View>
            <View style={styles.featureCopy}>
              <Text style={[styles.featureTitle, { color: colors.textPrimary }]}>{feature.title}</Text>
              <Text style={[styles.featureDescription, { color: colors.textSecondary }]}>{feature.description}</Text>
            </View>
          </View>
        ))}
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  content: {
    padding: spacing.lg,
    gap: spacing.lg,
  },
  hero: {
    gap: spacing.sm,
  },
  kicker: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  title: {
    fontSize: 28,
    fontWeight: '800',
    lineHeight: 34,
  },
  subtitle: {
    fontSize: 16,
    lineHeight: 22,
  },
  featureList: {
    gap: spacing.md,
  },
  featureRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: 12,
    borderWidth: 1,
  },
  iconBadge: {
    width: 40,
    height: 40,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  icon: {
    fontSize: 18,
  },
  featureCopy: {
    flex: 1,
    gap: 4,
  },
  featureTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  featureDescription: {
    fontSize: 14,
    lineHeight: 20,
  },
});

export default ProUpsellScreen;
