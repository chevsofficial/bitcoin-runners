import React from 'react';
import { StyleSheet, Text, TouchableOpacity, View } from 'react-native';

import { useThemeColors } from './theme/ThemeProvider';

const spacing = {
  sm: 8,
  md: 12,
  lg: 16,
};

export const CloudSyncCard = () => {
  const colors = useThemeColors();

  return (
    <View style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.border }]}>
      <Text style={[styles.title, { color: colors.textPrimary }]}>Cloud Sync & Backup</Text>

      <View style={styles.row}>
        <View>
          <Text style={[styles.label, { color: colors.textPrimary }]}>Cloud Sync sign-in</Text>
          <Text style={[styles.helper, { color: colors.textSecondary }]}>Keep your data in sync across devices.</Text>
        </View>
        <TouchableOpacity>
          <Text style={[styles.action, { color: colors.primary }]}>Sign in</Text>
        </TouchableOpacity>
      </View>

      <View style={[styles.row, styles.exportSection]}>
        <View>
          <Text style={[styles.label, { color: colors.textPrimary }]}>Export CSV</Text>
          <Text style={[styles.helper, { color: colors.textSecondary }]}>Download a backup of your data.</Text>
        </View>
        <TouchableOpacity>
          <Text style={[styles.action, { color: colors.primary }]}>Export</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  card: {
    padding: spacing.lg,
    borderRadius: 12,
    borderWidth: 1,
    gap: spacing.md,
  },
  title: {
    fontSize: 18,
    fontWeight: '700',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
  },
  helper: {
    marginTop: 2,
    fontSize: 14,
  },
  action: {
    fontSize: 14,
    fontWeight: '700',
  },
  exportSection: {
    marginTop: spacing.md,
  },
});

export default CloudSyncCard;
