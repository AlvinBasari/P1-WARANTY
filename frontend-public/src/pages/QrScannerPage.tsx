import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Html5Qrcode } from 'html5-qrcode';
import { toast } from 'sonner';
import { 
  ShieldCheck, 
  Camera, 
  Image as ImageIcon, 
  KeyRound, 
  Laptop, 
  ArrowRight, 
  ScanLine, 
  CheckCircle2, 
  AlertCircle, 
  Sparkles,
  RefreshCw
} from 'lucide-react';
import { DiagnosticPulse } from '../components/DiagnosticPulse';

export const QrScannerPage: React.FC = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'camera' | 'gallery' | 'manual'>('camera');
  
  // Camera state
  const [cameraActive, setCameraActive] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const scannerContainerId = "interactive-qr-reader";

  // Gallery state
  const [selectedFileName, setSelectedFileName] = useState<string | null>(null);
  const [scanningImage, setScanningImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // Manual state
  const [manualToken, setManualToken] = useState('');

  const extractToken = (rawText: string): string => {
    const trimmed = rawText.trim();
    const match = trimmed.match(/\/claim\/([a-zA-Z0-9_\-]+)/);
    if (match && match[1]) {
      return match[1];
    }
    return trimmed;
  };

  const handleScanSuccess = (decodedText: string) => {
    const token = extractToken(decodedText);
    toast.success('QR Code berhasil terdeteksi! Membuka portal klaim...');
    stopCamera();
    navigate(`/claim/${token}`);
  };

  // Start live camera
  const startCamera = async () => {
    setCameraError(null);
    try {
      if (!scannerRef.current) {
        scannerRef.current = new Html5Qrcode(scannerContainerId);
      }
      
      const config = {
        fps: 10,
        qrbox: { width: 240, height: 240 },
        aspectRatio: 1.0,
      };

      await scannerRef.current.start(
        { facingMode: "environment" },
        config,
        (decodedText) => {
          handleScanSuccess(decodedText);
        },
        () => {
          // ignore frame errors while searching for QR
        }
      );
      setCameraActive(true);
    } catch (err: any) {
      console.warn("Camera start error:", err);
      setCameraError(
        err?.message || 
        "Tidak dapat mengakses kamera. Pastikan izin kamera telah diaktifkan atau coba gunakan mode Ambil dari Galeri."
      );
      setCameraActive(false);
    }
  };

  const stopCamera = async () => {
    if (scannerRef.current && cameraActive) {
      try {
        await scannerRef.current.stop();
        scannerRef.current.clear();
      } catch (e) {
        console.warn("Failed to stop scanner:", e);
      } finally {
        setCameraActive(false);
      }
    }
  };

  useEffect(() => {
    if (activeTab === 'camera') {
      startCamera();
    } else {
      stopCamera();
    }
    return () => {
      stopCamera();
    };
  }, [activeTab]);

  // Gallery File handler
  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setSelectedFileName(file.name);
    setScanningImage(true);
    setCameraError(null);

    try {
      const html5QrCode = new Html5Qrcode("hidden-file-qr-reader");
      const decodedText = await html5QrCode.scanFile(file, true);
      html5QrCode.clear();
      handleScanSuccess(decodedText);
    } catch (err: any) {
      toast.error('Tidak ditemukan kode QR yang valid pada gambar tersebut.');
      setCameraError('Gagal memindai stiker dari gambar. Pastikan kode QR tampak jelas dan tidak buram.');
    } finally {
      setScanningImage(false);
    }
  };

  const handleManualSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!manualToken.trim()) {
      toast.error('Masukkan token atau nomor seri perangkat.');
      return;
    }
    const token = extractToken(manualToken);
    navigate(`/claim/${token}`);
  };

  const demoUnits = [
    {
      name: 'Lenovo ThinkCentre M700',
      token: 'qr-demo-lenovo-002',
      sn: 'SN-LEN-112233',
      badge: 'Unit Uji Skripsi',
      status: 'GARANSI AKTIF',
    },
    {
      name: 'Asus ExpertBook B1502CBA',
      token: 'qr-demo-asus-001',
      sn: 'SN-ASUS-998877',
      badge: 'Unit Demo 01',
      status: 'GARANSI AKTIF',
    },
    {
      name: 'Acer Veriton X2665G',
      token: 'qr-demo-acer-003',
      sn: 'SN-ACER-445566',
      badge: 'Unit Demo 02',
      status: 'GARANSI AKTIF',
    }
  ];

  return (
    <div className="min-h-screen bg-paper py-8 px-4 sm:px-6 lg:px-8 font-sans text-ink">
      <div id="hidden-file-qr-reader" style={{ display: 'none' }} />

      <div className="max-w-xl mx-auto space-y-6">
        {/* Main Branding Header */}
        <div className="text-center space-y-2">
          <div className="inline-flex p-2.5 bg-circuit text-white rounded-card shadow-card">
            <ShieldCheck className="w-6 h-6 text-signal-light" strokeWidth={1.75} />
          </div>
          <h1 className="text-2xl font-display font-bold text-ink tracking-tight">
            Portal Klaim Bantuan Garansi QR
          </h1>
          <p className="text-xs text-ink-muted">
            PT Jaya Teknologi Solusi — Layanan Garansi &amp; Dukungan Servis Terpadu
          </p>
          <div className="flex items-center justify-center gap-2 text-xs text-ink-muted pt-1">
            <span>Pindai Stiker Fisik Perangkat (FR-07)</span>
            <DiagnosticPulse status="active" />
          </div>
        </div>

        {/* Tab Selection Navigation */}
        <div className="bg-paper-card p-1.5 rounded-card border border-mist shadow-card grid grid-cols-3 gap-1.5 text-xs font-semibold">
          <button
            type="button"
            onClick={() => setActiveTab('camera')}
            className={`py-2 px-3 rounded-btn flex items-center justify-center gap-2 transition-all cursor-pointer ${
              activeTab === 'camera'
                ? 'bg-circuit text-white shadow-sm'
                : 'text-ink-muted hover:text-ink hover:bg-mist-light'
            }`}
          >
            <Camera className="w-3.5 h-3.5" />
            <span>Kamera Live</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('gallery')}
            className={`py-2 px-3 rounded-btn flex items-center justify-center gap-2 transition-all cursor-pointer ${
              activeTab === 'gallery'
                ? 'bg-circuit text-white shadow-sm'
                : 'text-ink-muted hover:text-ink hover:bg-mist-light'
            }`}
          >
            <ImageIcon className="w-3.5 h-3.5" />
            <span>Pilih Galeri</span>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('manual')}
            className={`py-2 px-3 rounded-btn flex items-center justify-center gap-2 transition-all cursor-pointer ${
              activeTab === 'manual'
                ? 'bg-circuit text-white shadow-sm'
                : 'text-ink-muted hover:text-ink hover:bg-mist-light'
            }`}
          >
            <KeyRound className="w-3.5 h-3.5" />
            <span>Input Kode</span>
          </button>
        </div>

        {/* Scanner Body Card */}
        <div className="bg-paper-card rounded-card border border-mist shadow-card p-5 sm:p-6 space-y-4">
          {/* TAB 1: LIVE CAMERA SCANNER */}
          {activeTab === 'camera' && (
            <div className="space-y-4 text-center">
              <div className="flex items-center justify-between text-xs border-b border-mist pb-3">
                <span className="font-semibold text-ink flex items-center gap-1.5">
                  <ScanLine className="w-4 h-4 text-signal" />
                  Viewfinder Kamera Perangkat
                </span>
                <span className="font-mono text-[10px] text-stable font-bold uppercase bg-stable-subtle px-2 py-0.5 rounded border border-stable/30">
                  {cameraActive ? 'Kamera Aktif' : 'Menghubungkan...'}
                </span>
              </div>

              {/* Viewfinder container */}
              <div className="relative mx-auto max-w-sm rounded-card overflow-hidden bg-slate-900 border border-mist shadow-inner min-h-[260px] flex items-center justify-center">
                <div id={scannerContainerId} className="w-full h-full" />
                
                {/* Visual Scanner Overlay */}
                {cameraActive && (
                  <div className="absolute inset-0 pointer-events-none flex items-center justify-center">
                    <div className="w-48 h-48 border-2 border-signal/70 rounded-lg relative">
                      {/* Scanner corner accents */}
                      <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-signal" />
                      <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-signal" />
                      <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-signal" />
                      <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-signal" />
                      {/* Laser scanning line */}
                      <div className="absolute inset-x-0 h-0.5 bg-signal shadow-[0_0_8px_#0E7C7B] animate-pulse" style={{ top: '45%' }} />
                    </div>
                  </div>
                )}

                {/* Error message or fallback */}
                {cameraError && (
                  <div className="absolute inset-0 bg-paper-card/95 p-4 flex flex-col items-center justify-center gap-2 text-center">
                    <AlertCircle className="w-8 h-8 text-fault" />
                    <p className="text-xs text-ink-muted max-w-xs">{cameraError}</p>
                    <div className="flex gap-2 pt-2">
                      <button
                        type="button"
                        onClick={startCamera}
                        className="py-1.5 px-3 bg-circuit text-white rounded-btn text-xs font-semibold flex items-center gap-1 cursor-pointer"
                      >
                        <RefreshCw className="w-3 h-3" />
                        <span>Coba Lagi</span>
                      </button>
                      <button
                        type="button"
                        onClick={() => setActiveTab('gallery')}
                        className="py-1.5 px-3 bg-mist-light text-ink rounded-btn text-xs font-semibold border border-mist cursor-pointer"
                      >
                        <span>Gunakan Galeri</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>

              <p className="text-xs text-ink-muted leading-relaxed">
                Arahkan lensa kamera ponsel Anda tepat ke stiker kode QR fisik yang menempel pada casing unit komputer PT JTS.
              </p>
            </div>
          )}

          {/* TAB 2: GALLERY / FILE UPLOAD SCANNER */}
          {activeTab === 'gallery' && (
            <div className="space-y-4">
              <div className="text-xs border-b border-mist pb-3">
                <span className="font-semibold text-ink flex items-center gap-1.5">
                  <ImageIcon className="w-4 h-4 text-signal" />
                  Unggah Foto Stiker QR dari Galeri
                </span>
              </div>

              <input
                type="file"
                ref={fileInputRef}
                accept="image/*"
                onChange={handleFileUpload}
                className="hidden"
              />

              <div
                onClick={() => fileInputRef.current?.click()}
                className="border-2 border-dashed border-mist hover:border-signal rounded-card p-8 text-center cursor-pointer transition-colors bg-paper hover:bg-mist-light/50 space-y-3"
              >
                <div className="w-12 h-12 rounded-full bg-signal-subtle text-signal flex items-center justify-center mx-auto">
                  <ImageIcon className="w-6 h-6" strokeWidth={1.75} />
                </div>
                <div>
                  <h3 className="text-xs font-bold text-ink">
                    {selectedFileName ? selectedFileName : 'Ketuk untuk Memilih Gambar Stiker'}
                  </h3>
                  <p className="text-[11px] text-ink-subtle mt-1">
                    Mendukung format PNG, JPG, JPEG, atau WebP dari galeri ponsel maupun tangkapan layar.
                  </p>
                </div>
                <button
                  type="button"
                  disabled={scanningImage}
                  className="py-2 px-4 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card inline-flex items-center gap-2 cursor-pointer"
                >
                  {scanningImage ? (
                    <>
                      <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                      <span>Memindai Kode QR...</span>
                    </>
                  ) : (
                    <>
                      <ImageIcon className="w-3.5 h-3.5" />
                      <span>Buka Galeri Foto</span>
                    </>
                  )}
                </button>
              </div>

              {cameraError && (
                <div className="p-3 bg-fault-subtle border border-fault-border rounded-card text-xs text-fault flex items-center gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0" />
                  <span>{cameraError}</span>
                </div>
              )}
            </div>
          )}

          {/* TAB 3: MANUAL TOKEN / SERIAL INPUT */}
          {activeTab === 'manual' && (
            <form onSubmit={handleManualSubmit} className="space-y-4">
              <div className="text-xs border-b border-mist pb-3">
                <span className="font-semibold text-ink flex items-center gap-1.5">
                  <KeyRound className="w-4 h-4 text-signal" />
                  Masukkan Token QR atau Nomor Seri Manual
                </span>
              </div>

              <div className="space-y-1.5">
                <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                  TOKEN QR / SERIAL NUMBER
                </label>
                <input
                  type="text"
                  value={manualToken}
                  onChange={(e) => setManualToken(e.target.value)}
                  placeholder="Contoh: qr-demo-lenovo-002"
                  className="w-full text-xs p-3 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal font-mono text-ink placeholder:text-ink-subtle"
                />
                <span className="text-[10px] text-ink-subtle block">
                  Token QR tercetak pada baris bawah stiker fisik (format: qr-demo-xxxx-xxx).
                </span>
              </div>

              <button
                type="submit"
                className="w-full py-2.5 px-4 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card flex items-center justify-center gap-2 cursor-pointer transition-colors"
              >
                <span>Verifikasi Token &amp; Buka Formulir Klaim</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </button>
            </form>
          )}
        </div>

        {/* Demo Units Quick Access Section */}
        <div className="bg-paper-card rounded-card border border-mist shadow-card p-5 space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-alert" />
              <h2 className="text-xs font-display font-bold text-ink uppercase tracking-wider">
                Unit Perangkat Uji Coba Demo
              </h2>
            </div>
            <span className="text-[10px] font-mono text-ink-subtle">Pilih Cepat untuk Pengujian</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5 pt-1">
            {demoUnits.map((u) => (
              <button
                key={u.token}
                type="button"
                onClick={() => navigate(`/claim/${u.token}`)}
                className="p-3 bg-paper hover:bg-mist-light border border-mist hover:border-signal rounded-card text-left transition-all cursor-pointer group flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between text-[10px] mb-1">
                    <span className="font-mono text-circuit font-bold">{u.badge}</span>
                    <span className="text-stable font-bold">✓ Aktif</span>
                  </div>
                  <h3 className="text-xs font-bold text-ink group-hover:text-signal transition-colors line-clamp-1">
                    {u.name}
                  </h3>
                  <p className="text-[10px] font-mono text-ink-subtle mt-0.5">
                    {u.sn}
                  </p>
                </div>
                <div className="mt-2.5 pt-2 border-t border-mist/50 flex items-center justify-between text-[10px] font-semibold text-signal">
                  <span>Buka Klaim</span>
                  <ArrowRight className="w-3 h-3 group-hover:translate-x-0.5 transition-transform" />
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Guide / Instruction Steps */}
        <div className="bg-circuit/5 rounded-card border border-circuit/20 p-4 space-y-2 text-xs text-ink-muted">
          <h3 className="font-semibold text-circuit flex items-center gap-1.5">
            <CheckCircle2 className="w-4 h-4 text-signal" />
            Panduan Pengajuan Klaim Garansi QR:
          </h3>
          <ol className="list-decimal list-inside space-y-1 text-[11px] leading-relaxed pl-1">
            <li>Temukan stiker fisik kode QR pada penutup samping atau belakang unit komputer.</li>
            <li>Pindai stiker langsung lewat <b>Kamera</b>, pilih gambar foto dari <b>Galeri</b>, atau klik salah satu <b>Unit Demo</b> di atas.</li>
            <li>Portal otomatis memverifikasi keaslian unit, masa garansi resmi, dan membuka formulir pelaporan servis on-site.</li>
          </ol>
        </div>
      </div>
    </div>
  );
};
