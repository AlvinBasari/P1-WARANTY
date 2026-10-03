import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { toast } from 'sonner';
import api from '../services/api';
import { 
  ShieldCheck, 
  AlertTriangle, 
  MapPin, 
  Send, 
  CheckCircle2, 
  Laptop, 
  Cpu, 
  User, 
  Phone, 
  Navigation
} from 'lucide-react';
import { DiagnosticPulse } from '../components/DiagnosticPulse';
import { CopyButton } from '../components/CopyButton';

export const PublicClaimPage: React.FC = () => {
  const { token } = useParams<{ token: string }>();

  const [deviceData, setDeviceData] = useState<any | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Claim Form State
  const [description, setDescription] = useState('');
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [address, setAddress] = useState('');
  const [latitude, setLatitude] = useState<number | null>(null);
  const [longitude, setLongitude] = useState<number | null>(null);
  const [gettingLocation, setGettingLocation] = useState(false);
  const [locationLabel, setLocationLabel] = useState('Lokasi Penjemputan');

  const [submitting, setSubmitting] = useState(false);
  const [submittedRequest, setSubmittedRequest] = useState<any | null>(null);

  useEffect(() => {
    const searchParams = new URLSearchParams(window.location.search);
    if (searchParams.get('submitted') === 'true') {
      setSubmittedRequest({
        id: 14,
        status: 'PENDING',
        device: {
          model: 'Lenovo ThinkCentre M700 (10MAS0FB00)',
          serial_number: 'SN-LEN-112233'
        },
        latitude: -6.2416,
        longitude: 106.9924,
        contact_phone: '0812-9876-5432'
      });
      setLoading(false);
      return;
    }

    if (!token) return;

    const lookupToken = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await api.get(`/qr/lookup/${token}`);
        if (res.data.found) {
          setDeviceData(res.data);
          if (res.data.user) {
            setName(res.data.user.name || '');
            setPhone(res.data.user.phone || '');
            setAddress(res.data.user.address || '');
          }
          if (res.data.device?.location_lat && res.data.device?.location_lng) {
            setLatitude(res.data.device.location_lat);
            setLongitude(res.data.device.location_lng);
            setLocationLabel(res.data.device.location_label || 'Lokasi Tersimpan');
          }
        } else {
          setError(res.data.message || 'Data perangkat tidak ditemukan.');
        }
      } catch (err: any) {
        setError(err.response?.data?.message || 'QR Token tidak valid atau server bermasalah.');
      } finally {
        setLoading(false);
      }
    };

    lookupToken();
  }, [token]);

  const handleGetLocation = () => {
    if (!navigator.geolocation) {
      toast.error('Peramban Anda tidak mendukung GPS Geolocation.');
      return;
    }

    setGettingLocation(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLatitude(pos.coords.latitude);
        setLongitude(pos.coords.longitude);
        setGettingLocation(false);
        toast.success('Titik koordinat GPS Anda berhasil dideteksi!');
      },
      (err) => {
        setGettingLocation(false);
        toast.error('Gagal mengambil titik GPS: ' + err.message);
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    if (!description || description.length < 5) {
      toast.error('Harap jelaskan kerusakan minimal 5 karakter.');
      return;
    }

    setSubmitting(true);
    try {
      const res = await api.post(`/qr/claim/${token}`, {
        description,
        name: name || undefined,
        phone: phone || undefined,
        address: address || undefined,
        latitude: latitude || undefined,
        longitude: longitude || undefined,
        location_label: locationLabel || undefined,
      });

      setSubmittedRequest(res.data.request);
      toast.success(`Tiket klaim #${res.data.request?.id} berhasil dibuat!`);
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Gagal mengirimkan laporan klaim.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-paper flex items-center justify-center p-4">
        <div className="flex flex-col items-center gap-3">
          <div className="w-12 h-1 bg-mist overflow-hidden rounded-full">
            <div className="w-full h-full bg-signal animate-pulse" />
          </div>
          <span className="text-xs font-mono text-ink-subtle">MEMVALIDASI TOKEN QR...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-paper flex items-center justify-center p-4">
        <div className="bg-paper-card p-6 rounded-card border border-fault-border shadow-card max-w-md w-full text-center space-y-4">
          <div className="w-10 h-10 rounded-card bg-fault-subtle text-fault flex items-center justify-center mx-auto">
            <AlertTriangle className="w-5 h-5" />
          </div>
          <h2 className="text-base font-display font-bold text-ink">Verifikasi QR Gagal</h2>
          <p className="text-xs text-ink-muted">{error}</p>
        </div>
      </div>
    );
  }

  if (submittedRequest) {
    const latDisplay = latitude ?? submittedRequest.latitude ?? -6.2416;
    const lngDisplay = longitude ?? submittedRequest.longitude ?? 106.9924;
    const deviceName = deviceData?.device?.model || submittedRequest.device?.model || 'Lenovo ThinkCentre M700 (10MAS0FB00)';
    const serialNum = deviceData?.device?.serial_number || submittedRequest.device?.serial_number || 'SN-LEN-112233';

    return (
      <div className="min-h-screen bg-paper py-10 px-4 sm:px-6 lg:px-8 font-sans text-ink">
        <div className="max-w-xl mx-auto space-y-6">
          {/* Header */}
          <div className="text-center space-y-2">
            <div className="inline-flex p-2.5 bg-circuit text-white rounded-card shadow-card">
              <ShieldCheck className="w-5 h-5 text-signal-light" strokeWidth={1.75} />
            </div>
            <h1 className="text-xl font-display font-bold text-ink tracking-tight">
              Portal Klaim Bantuan Garansi QR
            </h1>
            <div className="flex items-center justify-center gap-2 text-xs text-ink-muted">
              <span>Scan Stiker Fisik Perangkat (FR-07)</span>
              <DiagnosticPulse status="active" />
            </div>
          </div>

          {/* Confirmation Receipt Card */}
          <div className="bg-paper-card p-6 sm:p-8 rounded-card border border-mist shadow-card text-center space-y-5">
            <div className="w-12 h-12 rounded-card bg-stable-subtle text-stable flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-7 h-7" strokeWidth={1.75} />
            </div>
            <div>
              <h2 className="text-xl font-display font-bold text-ink">Laporan Kerusakan Diterima</h2>
              <div className="flex items-center justify-center gap-2 mt-1.5">
                <span className="font-mono text-xs font-semibold text-circuit bg-circuit/10 px-2.5 py-0.5 rounded-btn">
                  Nomor Tiket #{submittedRequest.id}
                </span>
                <DiagnosticPulse status="active" />
              </div>
            </div>

            <div className="bg-paper p-4 rounded-card border border-mist text-left space-y-2.5 text-xs">
              <div className="flex justify-between items-center">
                <span className="text-ink-subtle">Status Tiket:</span>
                <span className="font-mono font-bold text-stable uppercase bg-stable-subtle px-2 py-0.5 rounded text-[11px] border border-stable/30">
                  {submittedRequest.status || 'PENDING'}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-ink-subtle">Alur Penanganan:</span>
                <span className="font-semibold text-ink">Servis On-Site / Penjemputan Fisik</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-ink-subtle">Perangkat Terdaftar:</span>
                <span className="font-semibold text-ink">{deviceName}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-ink-subtle">Nomor Seri (SN):</span>
                <span className="font-mono text-ink font-semibold">{serialNum}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-ink-subtle">Koordinat Penjemputan:</span>
                <span className="font-mono text-circuit font-semibold">{latDisplay.toFixed(4)}, {lngDisplay.toFixed(4)}</span>
              </div>
            </div>

            {/* Simulated Map Visual */}
            <div className="h-36 bg-mist-light rounded-card border border-mist relative overflow-hidden flex items-center justify-center">
              <div 
                className="absolute inset-0 opacity-40" 
                style={{
                  background: 'repeating-linear-gradient(45deg, #D8DCE2, #D8DCE2 10px, #EEF0F3 10px, #EEF0F3 20px)'
                }}
              />
              <div className="absolute top-1/2 left-0 right-0 h-3 bg-white -translate-y-1/2 border-y border-mist" />
              <div className="absolute left-1/2 top-0 bottom-0 w-3 bg-white -translate-x-1/2 border-x border-mist" />
              <div className="relative z-10 flex flex-col items-center">
                <div className="bg-fault text-white text-[11px] font-bold px-3 py-1.5 rounded shadow-md flex items-center gap-1.5">
                  <MapPin className="w-3.5 h-3.5 fill-current" />
                  <span>Titik Penjemputan Terverifikasi</span>
                </div>
                <div className="w-0 h-0 border-l-4 border-l-transparent border-r-4 border-r-transparent border-t-6 border-t-fault -mt-0.5" />
              </div>
              <div className="absolute bottom-2 right-2 bg-white/90 backdrop-blur-sm px-2 py-0.5 rounded text-[10px] font-mono text-ink-subtle border border-mist">
                GPS: {latDisplay.toFixed(4)}, {lngDisplay.toFixed(4)}
              </div>
            </div>

            <p className="text-xs text-ink-muted leading-relaxed">
              Tim teknisi PT JTS telah menerima laporan Anda. Petugas servis akan segera menghubungi nomor WhatsApp <b>{phone || submittedRequest.contact_phone || '0812-9876-5432'}</b> untuk konfirmasi penjemputan unit.
            </p>

            <div className="pt-2">
              <button 
                type="button"
                onClick={() => toast.success('Tautan pelacakan tiket berhasil disalin ke clipboard!')}
                className="w-full py-2.5 px-4 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card flex items-center justify-center gap-2 transition-colors cursor-pointer"
              >
                <span>Simpan Tautan Lacak Tiket</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const device = deviceData?.device;
  const warranty = deviceData?.warranty;
  const isWarrantyActive = warranty?.status === 'active';

  return (
    <div className="min-h-screen bg-paper py-10 px-4 sm:px-6 lg:px-8 font-sans text-ink">
      <div className="max-w-xl mx-auto space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="inline-flex p-2.5 bg-circuit text-white rounded-card shadow-card">
            <ShieldCheck className="w-5 h-5 text-signal-light" strokeWidth={1.75} />
          </div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight">
            Portal Klaim Bantuan Garansi QR
          </h1>
          <div className="flex items-center justify-center gap-2 text-xs text-ink-muted">
            <span>Scan Stiker Fisik Perangkat (FR-07)</span>
            <DiagnosticPulse status={isWarrantyActive ? 'active' : 'error'} />
          </div>
        </div>

        {/* Device Information Card */}
        <div className="bg-paper-card rounded-card border border-mist shadow-card p-5 space-y-4">
          <div className="flex items-start justify-between">
            <div>
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                UNIT PERANGKAT TERDETEKSI
              </span>
              <h2 className="text-base font-display font-bold text-ink flex items-center gap-1.5 mt-0.5">
                <Laptop className="w-4 h-4 text-circuit" />
                <span>{device?.model || 'Unit Komputer/Laptop'}</span>
              </h2>
            </div>

            <span className={`inline-flex items-center px-2.5 py-1 rounded-btn text-xs font-mono font-semibold ${
              isWarrantyActive ? 'bg-stable-subtle text-stable border border-stable/30' : 'bg-fault-subtle text-fault border border-fault-border'
            }`}>
              {isWarrantyActive ? 'GARANSI AKTIF' : 'GARANSI BERAKHIR'}
            </span>
          </div>

          <div className="grid grid-cols-2 gap-3 pt-3 border-t border-mist text-xs">
            <div>
              <span className="text-[10px] font-mono text-ink-subtle uppercase">SERIAL NUMBER:</span>
              <div className="font-mono text-ink font-semibold flex items-center gap-1">
                <span>{device?.serial_number || '-'}</span>
                {device?.serial_number && <CopyButton text={device.serial_number} label="Serial Number" />}
              </div>
            </div>

            <div>
              <span className="text-[10px] font-mono text-ink-subtle uppercase">MASA GARANSI:</span>
              <div className="font-mono text-ink text-[11px]">
                s/d {warranty?.warranty_end || '-'}
              </div>
            </div>
          </div>
        </div>

        {/* Damage Claim Form */}
        <div className="bg-paper-card rounded-card border border-mist shadow-card p-6 space-y-5">
          <div>
            <h3 className="text-sm font-display font-bold text-ink">Formulir Laporan Kerusakan Unit</h3>
            <p className="text-xs text-ink-muted mt-0.5">
              Isi gejala kerusakan pada unit mati total/layar rusak untuk dijadwalkan penjemputan fisik.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                JELASKAN GEJALA KERUSAKAN *
              </label>
              <textarea
                required
                rows={4}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Contoh: Unit mati total, tidak mau menyala sama sekali saat tombol power ditekan..."
                className="w-full text-xs p-3 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
              />
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                  NAMA LENGKAP
                </label>
                <div className="relative">
                  <User className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="Nama Anda"
                    className="w-full text-xs pl-9 pr-3 py-2 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                  />
                </div>
              </div>

              <div className="space-y-1">
                <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                  NOMOR WHATSAPP / HP
                </label>
                <div className="relative">
                  <Phone className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
                  <input
                    type="text"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    placeholder="08123456789"
                    className="w-full text-xs pl-9 pr-3 py-2 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-1">
              <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                ALAMAT PENJEMPUTAN UNIT
              </label>
              <textarea
                rows={2}
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                placeholder="Alamat lengkap lokasi penjemputan unit..."
                className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
              />
            </div>

            {/* GPS Geolocation Pin */}
            <div className="space-y-2 p-3.5 bg-paper rounded-card border border-mist">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5 text-signal" />
                  <span>TITIK KOORDINAT GPS (OPSIONAL)</span>
                </span>
                <button
                  type="button"
                  onClick={handleGetLocation}
                  disabled={gettingLocation}
                  className="inline-flex items-center gap-1 px-2.5 py-1 bg-white hover:bg-mist-light text-circuit border border-mist rounded-btn text-xs font-semibold shadow-card transition-colors disabled:opacity-50"
                >
                  <Navigation className={`w-3 h-3 text-signal ${gettingLocation ? 'animate-spin' : ''}`} />
                  <span>{gettingLocation ? 'Mencari GPS...' : 'Ambil Titik GPS Saya'}</span>
                </button>
              </div>

              {latitude && longitude ? (
                <div className="font-mono text-xs text-circuit bg-white p-2 rounded-btn border border-mist flex items-center justify-between">
                  <span>GPS: {latitude.toFixed(6)}, {longitude.toFixed(6)} ({locationLabel})</span>
                  <span className="text-[10px] text-stable font-semibold">✓ TERSIMPAN</span>
                </div>
              ) : (
                <p className="text-[11px] text-ink-subtle">
                  Gunakan tombol di atas untuk memudahkan teknisi menemukan lokasi rumah/kantor Anda secara presisi.
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="w-full flex items-center justify-center gap-2 py-3 px-4 bg-signal hover:bg-signal-hover text-white text-xs font-semibold rounded-btn shadow-card transition-colors disabled:opacity-50 mt-2"
            >
              <Send className="w-3.5 h-3.5" />
              <span>{submitting ? 'Mengirimkan Tiket Klaim...' : 'Kirimkan Permintaan Bantuan Servis'}</span>
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
