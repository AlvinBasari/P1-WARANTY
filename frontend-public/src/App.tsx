import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'sonner';
import { PublicClaimPage } from './pages/PublicClaimPage';
import { QrScannerPage } from './pages/QrScannerPage';

export const App: React.FC = () => {
  return (
    <BrowserRouter>
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
      <Routes>
        <Route path="/" element={<QrScannerPage />} />
        <Route path="/claim/:token" element={<PublicClaimPage />} />
        <Route path="*" element={<QrScannerPage />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;
