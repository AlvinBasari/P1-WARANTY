import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'sonner';
import { AuthProvider, useAuth } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { RepairRequests } from './pages/RepairRequests';
import { TrackingManager } from './pages/TrackingManager';
import { Warranties } from './pages/Warranties';
import { RemoteSessions } from './pages/RemoteSessions';

const ProtectedLayout: React.FC = () => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-paper">
        <div className="flex flex-col items-center gap-3">
          <div className="w-12 h-1 bg-mist overflow-hidden rounded-full">
            <div className="w-full h-full bg-signal animate-pulse" />
          </div>
          <span className="text-xs text-ink-subtle font-mono">MEMUAT SISTEM KONTROL...</span>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="min-h-screen bg-paper flex flex-col font-sans text-ink">
      <Navbar />
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/requests" element={<RepairRequests />} />
          <Route path="/tracking" element={<TrackingManager />} />
          <Route path="/warranties" element={<Warranties />} />
          <Route path="/sessions" element={<RemoteSessions />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <AuthProvider>
      <Toaster 
        position="top-right" 
        richColors 
        theme="light"
        toastOptions={{
          style: {
            fontFamily: '"IBM Plex Sans", sans-serif',
            borderRadius: '6px',
            border: '1px solid #D8DCE2',
          }
        }}
      />
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/*" element={<ProtectedLayout />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
};

export default App;
