import { useEffect, useRef, useState } from 'react';
import { useNetworkState } from 'expo-network';
import { useSQLiteContext } from 'expo-sqlite';
import {
  ActivityIndicator,
  Pressable,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import {
  flushOutbox,
  isNetworkReady,
  nextTripStatus,
  tripStatuses,
  tripStatusLabel,
  type DriverUpdate,
  type TripStatus,
} from './driverUpdates';
import {
  getRecentFailures,
  getTripStatus,
  listPendingUpdates,
  queueTripStatus,
  recordUpdateFailure,
  removePendingUpdate,
} from './storage/dispatchRepository';
import { createUpdateSender } from './syncClient';

const tripId = 'DEMO-0142';
const apiBaseUrl = process.env.EXPO_PUBLIC_API_URL;

const statusActions: Record<TripStatus, string> = {
  accepted: 'Accept trip',
  'en-route': 'Start route to pickup',
  arrived: 'Confirm arrival',
  'picked-up': 'Confirm passenger onboard',
  completed: 'Complete trip',
};

const statusLabels: Record<TripStatus, string> = {
  accepted: 'ACCEPTED',
  'en-route': 'EN ROUTE',
  arrived: 'AT PICKUP',
  'picked-up': 'ON BOARD',
  completed: 'COMPLETED',
};

function makeOutbox(database: ReturnType<typeof useSQLiteContext>) {
  return {
    listPending: () => listPendingUpdates(database),
    remove: (id: string) => removePendingUpdate(database, id),
    recordFailure: (id: string, message: string) => recordUpdateFailure(database, id, message),
  };
}

export function DispatchScreen() {
  const database = useSQLiteContext();
  const network = useNetworkState();
  const syncInFlight = useRef(false);
  const [status, setStatus] = useState<TripStatus | null>(null);
  const [pendingUpdates, setPendingUpdates] = useState<DriverUpdate[]>([]);
  const [failedCount, setFailedCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('Your trip updates save on this device first.');

  const online = isNetworkReady(network);
  const nextStatus = nextTripStatus(status);
  const currentStep = status === null ? -1 : tripStatuses.indexOf(status);

  async function reloadFromDevice() {
    const [savedStatus, savedUpdates, savedFailures] = await Promise.all([
      getTripStatus(database, tripId),
      listPendingUpdates(database),
      getRecentFailures(database),
    ]);
    setStatus(savedStatus);
    setPendingUpdates(savedUpdates);
    setFailedCount(savedFailures);
  }

  useEffect(() => {
    let active = true;

    void reloadFromDevice()
      .catch(() => {
        if (active) setNotice('Local storage could not be opened. Please restart the demo.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [database]);

  async function advanceTrip() {
    if (!nextStatus || busy) return;
    setBusy(true);
    try {
      await queueTripStatus(database, tripId, nextStatus);
      await reloadFromDevice();
      setNotice('Update saved to the local outbox.');
    } catch {
      setNotice('The update could not be saved. Your trip status is unchanged.');
    } finally {
      setBusy(false);
    }
  }

  async function syncUpdates() {
    const send = createUpdateSender(apiBaseUrl);
    if (!send) {
      setNotice('Set EXPO_PUBLIC_API_URL to connect a dispatch API.');
      return;
    }
    if (!online) {
      setNotice('No connection yet. Updates remain safely on this device.');
      return;
    }
    if (syncInFlight.current || pendingUpdates.length === 0) return;

    syncInFlight.current = true;
    setBusy(true);
    try {
      const result = await flushOutbox(makeOutbox(database), send);
      await reloadFromDevice();
      setNotice(
        result.failed
          ? 'Sync paused at the first failed update. The outbox is ready to retry.'
          : result.sent === 0
            ? 'There are no updates waiting to sync.'
            : `${result.sent} ${result.sent === 1 ? 'update' : 'updates'} delivered.`,
      );
    } catch {
      setNotice('Sync could not reach the service. Updates remain queued.');
    } finally {
      syncInFlight.current = false;
      setBusy(false);
    }
  }

  useEffect(() => {
    if (online && apiBaseUrl && pendingUpdates.length > 0 && !loading) {
      void syncUpdates();
    }
  }, [online, pendingUpdates.length, loading, database]);

  const connectionLabel = network.isConnected === undefined ? 'CHECKING' : online ? 'ONLINE' : 'OFFLINE';

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.topbar}>
          <View style={styles.brand}>
            <View style={styles.brandMark}><Text style={styles.brandMarkText}>RK</Text></View>
            <View>
              <Text style={styles.brandName}>FIELD / 07</Text>
              <Text style={styles.brandSub}>DRIVER COMPANION</Text>
            </View>
          </View>
          <View style={[styles.connection, online ? styles.connectionOnline : styles.connectionOffline]}>
            <View style={[styles.connectionDot, online && styles.connectionDotOnline]} />
            <Text style={styles.connectionText}>{connectionLabel}</Text>
          </View>
        </View>

        <View style={styles.headingBlock}>
          <Text style={styles.eyebrow}>TUESDAY / SHIFT 03</Text>
          <Text style={styles.heading}>Move the work{'\n'}<Text style={styles.headingAccent}>forward.</Text></Text>
          <Text style={styles.headingCopy}>One clear next step. Every update saved before it leaves the phone.</Text>
        </View>

        <View style={styles.statsRow}>
          <View style={styles.statBlock}>
            <Text style={styles.statValue}>01</Text>
            <Text style={styles.statLabel}>ACTIVE TRIP</Text>
          </View>
          <View style={styles.statDivider} />
          <View style={styles.statBlock}>
            <Text style={styles.statValue}>{String(pendingUpdates.length).padStart(2, '0')}</Text>
            <Text style={styles.statLabel}>WAITING TO SYNC</Text>
          </View>
          <View style={styles.statDivider} />
          <View style={styles.statBlock}>
            <Text style={styles.statValue}>{String(failedCount).padStart(2, '0')}</Text>
            <Text style={styles.statLabel}>RETRY COUNT</Text>
          </View>
        </View>

        <View style={styles.tripCard}>
          <View style={styles.tripCardTop}>
            <View>
              <Text style={styles.cardEyebrow}>CURRENT ASSIGNMENT</Text>
              <Text style={styles.tripId}>{tripId}</Text>
            </View>
            <Text style={styles.demoBadge}>DEMO</Text>
          </View>

          <View style={styles.routeRow}>
            <View style={styles.routeRail}>
              <View style={styles.routeOrigin} />
              <View style={styles.routeLine} />
              <View style={styles.routeDestination} />
            </View>
            <View style={styles.routeStops}>
              <View>
                <Text style={styles.stopLabel}>PICKUP</Text>
                <Text style={styles.stopName}>Central Station</Text>
              </View>
              <View>
                <Text style={styles.stopLabel}>DROP-OFF</Text>
                <Text style={styles.stopName}>Westlands Market</Text>
              </View>
            </View>
          </View>

          <View style={styles.tripMeta}>
            <View><Text style={styles.metaValue}>4.8 km</Text><Text style={styles.metaLabel}>DISTANCE</Text></View>
            <View><Text style={styles.metaValue}>~12 min</Text><Text style={styles.metaLabel}>EST. DRIVE</Text></View>
            <View><Text style={styles.metaValue}>Standard</Text><Text style={styles.metaLabel}>SERVICE</Text></View>
          </View>
        </View>

        <View style={styles.statusSection}>
          <View style={styles.sectionTitleRow}>
            <View>
              <Text style={styles.cardEyebrow}>TRIP PROGRESS</Text>
              <Text style={styles.statusTitle}>{tripStatusLabel(status)}</Text>
            </View>
            <Text style={styles.stepCount}>{String(Math.max(currentStep + 1, 0)).padStart(2, '0')} / 05</Text>
          </View>

          <View style={styles.progressTrack} accessibilityLabel={`Trip progress, step ${currentStep + 1} of 5`}>
            {tripStatuses.map((step, index) => (
              <View key={step} style={styles.progressItem}>
                <View style={[styles.progressDot, index <= currentStep && styles.progressDotComplete]} />
                {index < tripStatuses.length - 1 && <View style={[styles.progressLine, index < currentStep && styles.progressLineComplete]} />}
              </View>
            ))}
          </View>

          <View style={styles.progressLabels}>
            <Text style={styles.progressLabel}>ACCEPT</Text>
            <Text style={styles.progressLabel}>PICKUP</Text>
            <Text style={styles.progressLabel}>COMPLETE</Text>
          </View>

          {loading ? (
            <ActivityIndicator color={palette.ink} style={styles.loading} />
          ) : nextStatus ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={statusActions[nextStatus]}
              disabled={busy}
              onPress={advanceTrip}
              style={({ pressed }) => [styles.primaryButton, pressed && styles.buttonPressed, busy && styles.buttonDisabled]}
            >
              {busy ? <ActivityIndicator color={palette.ink} /> : <Text style={styles.primaryButtonText}>{statusActions[nextStatus]}</Text>}
              {!busy && <Text style={styles.buttonArrow}>&rarr;</Text>}
            </Pressable>
          ) : (
            <View style={styles.completeBanner}><Text style={styles.completeText}>Trip complete. Nice work.</Text></View>
          )}
        </View>

        <View style={styles.syncCard}>
          <View style={styles.syncHeader}>
            <View>
              <Text style={styles.cardEyebrow}>LOCAL OUTBOX</Text>
              <Text style={styles.syncTitle}>Updates stay with you.</Text>
            </View>
            <View style={styles.syncIcon}><Text style={styles.syncIconText}>&uarr;</Text></View>
          </View>
          <Text style={styles.syncCopy}>{notice}</Text>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Sync pending trip updates"
            disabled={busy || pendingUpdates.length === 0}
            onPress={syncUpdates}
            style={({ pressed }) => [styles.secondaryButton, pressed && styles.secondaryPressed, (busy || pendingUpdates.length === 0) && styles.buttonDisabled]}
          >
            <Text style={styles.secondaryButtonText}>{busy ? 'Working...' : 'Sync pending updates'}</Text>
            <Text style={styles.secondaryArrow}>&nearr;</Text>
          </Pressable>
          {pendingUpdates.length > 0 && (
            <View style={styles.queueList}>
              {pendingUpdates.slice(0, 3).map((update) => (
                <View key={update.id} style={styles.queueRow}>
                  <View style={styles.queueDot} />
                  <Text style={styles.queueStatus}>{statusLabels[update.status]}</Text>
                  <Text style={styles.queueTime}>{new Date(update.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</Text>
                  {update.attempts > 0 && <Text style={styles.retryBadge}>RETRY {update.attempts}</Text>}
                </View>
              ))}
              {pendingUpdates.length > 3 && <Text style={styles.moreUpdates}>+ {pendingUpdates.length - 3} more waiting</Text>}
            </View>
          )}
        </View>

        <View style={styles.footer}>
          <Text style={styles.footerText}>OFFLINE-FIRST REFERENCE</Text>
          <Text style={styles.footerDivider}>/</Text>
          <Text style={styles.footerText}>DEMO ROUTE</Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const palette = {
  ink: '#1C2A24',
  inkSoft: '#516057',
  muted: '#829087',
  paper: '#F3F4ED',
  white: '#FFFFFF',
  line: '#DCE1D8',
  lime: '#D7EF7C',
  blue: '#8FB2F5',
  coral: '#E98268',
};

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: palette.paper },
  content: { width: '100%', maxWidth: 560, alignSelf: 'center', paddingHorizontal: 22, paddingTop: 16, paddingBottom: 36 },
  topbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 37 },
  brand: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  brandMark: { width: 38, height: 38, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.ink },
  brandMarkText: { color: palette.lime, fontFamily: 'monospace', fontSize: 12, fontWeight: '700' },
  brandName: { color: palette.ink, fontFamily: 'monospace', fontSize: 10, fontWeight: '700' },
  brandSub: { marginTop: 3, color: palette.muted, fontFamily: 'monospace', fontSize: 8 },
  connection: { flexDirection: 'row', alignItems: 'center', gap: 7, paddingVertical: 7, paddingHorizontal: 10, borderWidth: 1 },
  connectionOnline: { borderColor: '#B9C9A1', backgroundColor: '#EEF5DC' },
  connectionOffline: { borderColor: '#DBB8AC', backgroundColor: '#F8ECE7' },
  connectionDot: { width: 6, height: 6, backgroundColor: palette.coral },
  connectionDotOnline: { backgroundColor: '#6A8B34' },
  connectionText: { color: palette.ink, fontFamily: 'monospace', fontSize: 8, fontWeight: '700' },
  headingBlock: { marginBottom: 25 },
  eyebrow: { marginBottom: 10, color: '#66766B', fontFamily: 'monospace', fontSize: 9, letterSpacing: 0 },
  heading: { color: palette.ink, fontSize: 41, fontWeight: '700', lineHeight: 44, letterSpacing: 0 },
  headingAccent: { color: '#708A39' },
  headingCopy: { maxWidth: 330, marginTop: 12, color: palette.inkSoft, fontSize: 13, lineHeight: 19 },
  statsRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingVertical: 15, borderTopWidth: 1, borderBottomWidth: 1, borderColor: palette.line },
  statBlock: { flex: 1 },
  statValue: { color: palette.ink, fontSize: 23, fontWeight: '600', lineHeight: 27 },
  statLabel: { marginTop: 3, color: palette.muted, fontFamily: 'monospace', fontSize: 7 },
  statDivider: { width: 1, height: 32, marginHorizontal: 11, backgroundColor: palette.line },
  tripCard: { marginTop: 22, padding: 17, backgroundColor: palette.white, borderWidth: 1, borderColor: palette.line },
  tripCardTop: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', paddingBottom: 14, borderBottomWidth: 1, borderBottomColor: '#E9ECE5' },
  cardEyebrow: { color: '#78857D', fontFamily: 'monospace', fontSize: 8, fontWeight: '500' },
  tripId: { marginTop: 5, color: palette.ink, fontFamily: 'monospace', fontSize: 13, fontWeight: '700' },
  demoBadge: { paddingVertical: 4, paddingHorizontal: 7, backgroundColor: '#F7E5DE', color: '#9B4F38', fontFamily: 'monospace', fontSize: 8, fontWeight: '700' },
  routeRow: { flexDirection: 'row', gap: 13, paddingTop: 16, paddingBottom: 16 },
  routeRail: { width: 12, alignItems: 'center', paddingVertical: 4 },
  routeOrigin: { width: 8, height: 8, borderRadius: 4, backgroundColor: '#799447' },
  routeLine: { width: 1, flex: 1, minHeight: 27, marginVertical: 4, backgroundColor: '#B8C6AA' },
  routeDestination: { width: 8, height: 8, backgroundColor: palette.coral },
  routeStops: { flex: 1, justifyContent: 'space-between', gap: 15 },
  stopLabel: { color: palette.muted, fontFamily: 'monospace', fontSize: 7 },
  stopName: { marginTop: 3, color: palette.ink, fontSize: 13, fontWeight: '600' },
  tripMeta: { flexDirection: 'row', justifyContent: 'space-between', paddingTop: 12, borderTopWidth: 1, borderTopColor: '#E9ECE5' },
  metaValue: { color: palette.ink, fontFamily: 'monospace', fontSize: 10, fontWeight: '600' },
  metaLabel: { marginTop: 3, color: palette.muted, fontFamily: 'monospace', fontSize: 7 },
  statusSection: { marginTop: 25 },
  sectionTitleRow: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between' },
  statusTitle: { marginTop: 4, color: palette.ink, fontSize: 17, fontWeight: '700' },
  stepCount: { color: '#75843E', fontFamily: 'monospace', fontSize: 10, fontWeight: '600' },
  progressTrack: { flexDirection: 'row', alignItems: 'center', marginTop: 19 },
  progressItem: { flex: 1, flexDirection: 'row', alignItems: 'center' },
  progressDot: { zIndex: 1, width: 11, height: 11, borderWidth: 1, borderColor: '#BCC6BD', backgroundColor: palette.paper },
  progressDotComplete: { borderColor: palette.ink, backgroundColor: palette.lime },
  progressLine: { height: 1, flex: 1, backgroundColor: '#D2D9D0' },
  progressLineComplete: { backgroundColor: '#839A4E' },
  progressLabels: { flexDirection: 'row', justifyContent: 'space-between', marginTop: 7 },
  progressLabel: { color: palette.muted, fontFamily: 'monospace', fontSize: 7 },
  primaryButton: { minHeight: 52, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 18, paddingHorizontal: 16, backgroundColor: palette.lime },
  primaryButtonText: { color: palette.ink, fontSize: 13, fontWeight: '700' },
  buttonArrow: { color: palette.ink, fontSize: 21 },
  buttonPressed: { opacity: 0.78 },
  buttonDisabled: { opacity: 0.5 },
  loading: { marginTop: 23 },
  completeBanner: { marginTop: 18, padding: 14, borderWidth: 1, borderColor: '#BDCCA6', backgroundColor: '#EEF5DE' },
  completeText: { color: '#50652C', fontSize: 12, fontWeight: '600' },
  syncCard: { marginTop: 26, padding: 16, borderWidth: 1, borderColor: '#CBD3CC', backgroundColor: '#E9EDE5' },
  syncHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  syncTitle: { marginTop: 4, color: palette.ink, fontSize: 17, fontWeight: '700' },
  syncIcon: { width: 30, height: 30, alignItems: 'center', justifyContent: 'center', backgroundColor: palette.ink },
  syncIconText: { color: palette.lime, fontSize: 16, fontWeight: '700' },
  syncCopy: { marginTop: 8, color: palette.inkSoft, fontSize: 11, lineHeight: 16 },
  secondaryButton: { minHeight: 42, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 13, paddingHorizontal: 11, borderWidth: 1, borderColor: '#A8B4A9', backgroundColor: '#F5F6F0' },
  secondaryPressed: { backgroundColor: '#DEE5D8' },
  secondaryButtonText: { color: palette.ink, fontSize: 11, fontWeight: '600' },
  secondaryArrow: { color: palette.ink, fontSize: 17 },
  queueList: { marginTop: 13, borderTopWidth: 1, borderTopColor: '#D3DAD1' },
  queueRow: { minHeight: 35, flexDirection: 'row', alignItems: 'center', gap: 8, borderBottomWidth: 1, borderBottomColor: '#D3DAD1' },
  queueDot: { width: 6, height: 6, backgroundColor: '#718C3D' },
  queueStatus: { flex: 1, color: '#4B5B50', fontFamily: 'monospace', fontSize: 8, fontWeight: '600' },
  queueTime: { color: '#748078', fontFamily: 'monospace', fontSize: 8 },
  retryBadge: { color: '#A6533C', fontFamily: 'monospace', fontSize: 7 },
  moreUpdates: { paddingTop: 8, color: '#6B786F', fontFamily: 'monospace', fontSize: 8 },
  footer: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8, marginTop: 24 },
  footerText: { color: '#89948C', fontFamily: 'monospace', fontSize: 7 },
  footerDivider: { color: '#89948C', fontFamily: 'monospace', fontSize: 9 },
});