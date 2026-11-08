import { StatusBar } from 'expo-status-bar';
import { SQLiteProvider } from 'expo-sqlite';
import { DispatchScreen } from './src/DispatchScreen';
import { initializeDispatchDatabase } from './src/storage/dispatchRepository';

export default function App() {
  return (
    <SQLiteProvider databaseName="dispatch-field.db" onInit={initializeDispatchDatabase}>
      <StatusBar style="dark" />
      <DispatchScreen />
    </SQLiteProvider>
  );
}
