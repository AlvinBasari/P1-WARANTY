import React, { useEffect, useState } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { toast } from 'sonner';
import api from '../services/api';
import { ShieldCheck, Plus, QrCode, Cpu, Search, RefreshCw, X, ExternalLink, AlertCircle, CheckCircle2, User } from 'lucide-react';
import { CopyButton } from '../components/CopyButton';
import { TableSkeleton } from '../components/SkeletonLoader';

export const Warranties: React.FC = () => {
  const [devices, setDevices] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAddModal, setShowAddModal] = useState(false);
  const [selectedQr, setSelectedQr] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'expired' | 'unlinked'>('all');

  // Form State
  const [hardwareId, setHardwareId] = useState('');
  const [model, setModel] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [purchaseDate, setPurchaseDate] = useState(new Date().toISOString().split('T')[0]);
  const [warrantyYears, setWarrantyYears] = useState(2);
  const [formLoading, setFormLoading] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const fetchDevices = async () => {
    setLoading(true);
    try {
      const res = await api.get('/devices');
      setDevices(res.data.devices || []);
    } catch (e) {
      console.error(e);
      toast.error('Gagal memuat master data perangkat.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  const handleAddDevice = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormLoading(true);
    setFormError(null);

    try {
      await api.post('/devices', {
        hardware_id: hardwareId.trim(),
        model: model.trim(),
        serial_number: serialNumber.trim(),
        purchase_date: purchaseDate,
        warranty_years: Number(warrantyYears),
      });

      toast.success(`Perangkat ${model} berhasil didaftarkan!`);
      setShowAddModal(false);
      setHardwareId('');
      setModel('');
      setSerialNumber('');
      fetchDevices();
    } catch (err: any) {
      setFormError(err.response?.data?.message || 'Gagal menambahkan data perangkat');
      toast.error('Gagal menambahkan perangkat.');
    } finally {
      setFormLoading(false);
    }
  };

  const filteredDevices = devices.filter((dev) => {
    const warranty = dev.warranty;
    const isExpired = warranty?.status === 'expired' || (warranty?.warranty_end && new Date(warranty.warranty_end) < new Date());
    const isUnlinked = !dev.user;

    if (statusFilter === 'active' && isExpired) return false;
    if (statusFilter === 'expired' && !isExpired) return false;
    if (statusFilter === 'unlinked' && !isUnlinked) return false;

    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const matchModel = dev.model?.toLowerCase().includes(q);
      const matchSn = dev.serial_number?.toLowerCase().includes(q);
      const matchHw = dev.hardware_id?.toLowerCase().includes(q);
      const matchUser = dev.user?.name?.toLowerCase().includes(q) || dev.user?.email?.toLowerCase().includes(q);
      return matchModel || matchSn || matchHw || matchUser;
    }
    return true;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-3 border-b border-mist">
        <div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight">
            Master Data Garansi &amp; Perangkat
          </h1>
          <p className="text-xs text-ink-muted mt-0.5">
            Manajemen pemetaan BIOS/Hardware ID, masa aktif garansi, dan token stiker QR code fisik.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={fetchDevices}
            disabled={loading}
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-white border border-mist rounded-btn text-xs font-medium text-ink-muted hover:text-ink hover:bg-mist-light shadow-card transition-all"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-signal' : ''}`} />
            <span>Segarkan Data</span>
          </button>
          <button
            onClick={() => setShowAddModal(true)}
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card transition-all"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Daftarkan Perangkat</span>
          </button>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="bg-paper-card p-3 rounded-card border border-mist shadow-card flex flex-col md:flex-row gap-3 justify-between items-center">
        <div className="flex rounded-btn bg-paper border border-mist p-0.5 text-xs font-medium w-full md:w-auto">
          <button
            onClick={() => setStatusFilter('all')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'all' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Semua ({devices.length})
          </button>
          <button
            onClick={() => setStatusFilter('active')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'active' ? 'bg-stable text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Garansi Aktif
          </button>
          <button
            onClick={() => setStatusFilter('expired')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'expired' ? 'bg-fault text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Expired
          </button>
          <button
            onClick={() => setStatusFilter('unlinked')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'unlinked' ? 'bg-alert text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Belum Terhubung
          </button>
        </div>

        <div className="relative w-full md:w-80">
          <Search className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Cari Serial, BIOS, Model, Pemilik..."
            className="w-full pl-8 pr-3 py-1.5 bg-paper border border-mist rounded-btn text-xs focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-paper-card rounded-card border border-mist shadow-card overflow-hidden">
        {loading && devices.length === 0 ? (
          <TableSkeleton rows={5} />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-paper text-ink-subtle uppercase text-[10px] font-mono font-bold tracking-wider border-b border-mist">
                <tr>
                  <th className="px-5 py-3">Model &amp; Serial</th>
                  <th className="px-5 py-3">BIOS / Hardware ID</th>
                  <th className="px-5 py-3">Pemilik Terdaftar</th>
                  <th className="px-5 py-3">Masa Berlaku Garansi</th>
                  <th className="px-5 py-3">Status</th>
                  <th className="px-5 py-3 text-right">Stiker QR</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-mist">
                {filteredDevices.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-5 py-12 text-center text-ink-muted text-xs">
                      Tidak ada data perangkat yang sesuai dengan filter pencarian.
                    </td>
                  </tr>
                ) : (
                  filteredDevices.map((dev) => {
                    const warranty = dev.warranty;
                    const isExpired = warranty?.status === 'expired' || (warranty?.warranty_end && new Date(warranty.warranty_end) < new Date());

                    return (
                      <tr key={dev.id} className="hover:bg-mist-light/40 transition-colors">
                        <td className="px-5 py-3.5">
                          <div className="font-semibold text-ink">{dev.model}</div>
                          <div className="text-[11px] text-ink-subtle font-mono flex items-center gap-1 mt-0.5">
                            <span>SN: {dev.serial_number}</span>
                            <CopyButton text={dev.serial_number} label="Serial Number" />
                          </div>
                        </td>

                        <td className="px-5 py-3.5">
                          <div className="inline-flex items-center gap-1.5 px-2 py-0.5 bg-paper border border-mist rounded-btn font-mono text-[11px] text-circuit">
                            <Cpu className="w-3 h-3 text-signal" />
                            <span>{dev.hardware_id}</span>
                            <CopyButton text={dev.hardware_id} label="Hardware ID" />
                          </div>
                        </td>

                        <td className="px-5 py-3.5">
                          {dev.user ? (
                            <div>
                              <div className="font-medium text-ink flex items-center gap-1">
                                <User className="w-3.5 h-3.5 text-ink-subtle" />
                                <span>{dev.user.name}</span>
                              </div>
                              <div className="text-[11px] text-ink-muted">{dev.user.email}</div>
                            </div>
                          ) : (
                            <span className="text-[10px] text-alert bg-alert-subtle border border-alert-border px-2 py-0.5 rounded-btn font-medium font-mono">
                              BELUM TERHUBUNG
                            </span>
                          )}
                        </td>

                        <td className="px-5 py-3.5 text-[11px] font-mono">
                          <div>Mulai: <span className="text-ink font-semibold">{warranty?.warranty_start || '-'}</span></div>
                          <div className="text-ink-muted">Berakhir: <span className="text-ink font-semibold">{warranty?.warranty_end || '-'}</span></div>
                        </td>

                        <td className="px-5 py-3.5">
                          <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-semibold ${
                            isExpired ? 'bg-fault-subtle text-fault border border-fault-border' : 'bg-stable-subtle text-stable border border-stable/30'
                          }`}>
                            {isExpired ? 'EXPIRED' : 'AKTIF'}
                          </span>
                        </td>

                        <td className="px-5 py-3.5 text-right">
                          <button
                            onClick={() => setSelectedQr(dev.qr_token)}
                            className="inline-flex items-center gap-1 px-2.5 py-1 bg-paper hover:bg-mist-light text-circuit rounded-btn text-xs font-medium border border-mist transition-colors"
                          >
                            <QrCode className="w-3.5 h-3.5" />
                            <span>Stiker QR</span>
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* QR Modal via Headless UI */}
      <Transition appear show={!!selectedQr} as={Fragment}>
        <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={() => setSelectedQr(null)}>
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-200"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="ease-in duration-150"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-ink/50 backdrop-blur-sm" />
          </Transition.Child>

          <div className="fixed inset-0 overflow-y-auto">
            <div className="flex min-h-full items-center justify-center p-4 text-center">
              <Transition.Child
                as={Fragment}
                enter="ease-out duration-200"
                enterFrom="opacity-0 scale-95"
                enterTo="opacity-100 scale-100"
                leave="ease-in duration-150"
                leaveFrom="opacity-100 scale-100"
                leaveTo="opacity-0 scale-95"
              >
                <Dialog.Panel className="w-full max-w-sm transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all p-5 space-y-4 text-center">
                  <div className="flex items-center justify-between pb-3 border-b border-mist">
                    <Dialog.Title as="h3" className="text-sm font-display font-bold text-ink">
                      Stiker QR Code Fisik
                    </Dialog.Title>
                    <button onClick={() => setSelectedQr(null)} className="p-1 rounded-btn hover:bg-mist-light text-ink-muted">
                      <X className="w-4 h-4" />
                    </button>
                  </div>

                  <div className="p-4 bg-paper rounded-card border border-mist flex justify-center">
                    <img
                      src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(`http://localhost:5174/claim/${selectedQr}`)}`}
                      alt="QR Code Stiker"
                      className="w-44 h-44 rounded-btn"
                    />
                  </div>

                  <div className="bg-paper p-2 rounded-btn text-[10px] font-mono text-ink-muted border border-mist break-all flex items-center justify-between">
                    <span>Token: {selectedQr}</span>
                    {selectedQr && <CopyButton text={selectedQr} label="Token QR" />}
                  </div>

                  <div className="space-y-2 pt-2">
                    <a
                      href={`http://localhost:5174/claim/${selectedQr}`}
                      target="_blank"
                      rel="noreferrer"
                      className="inline-flex items-center justify-center gap-1.5 w-full py-2 bg-circuit hover:bg-circuit-dark text-white rounded-btn text-xs font-semibold shadow-card transition-colors"
                    >
                      <span>Buka Portal QR Publik</span>
                      <ExternalLink className="w-3.5 h-3.5" />
                    </a>
                  </div>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </Dialog>
      </Transition>

      {/* Add Device Modal via Headless UI */}
      <Transition appear show={showAddModal} as={Fragment}>
        <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={() => setShowAddModal(false)}>
          <Transition.Child
            as={Fragment}
            enter="ease-out duration-200"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="ease-in duration-150"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-ink/50 backdrop-blur-sm" />
          </Transition.Child>

          <div className="fixed inset-0 overflow-y-auto">
            <div className="flex min-h-full items-center justify-center p-4 text-center">
              <Transition.Child
                as={Fragment}
                enter="ease-out duration-200"
                enterFrom="opacity-0 scale-95"
                enterTo="opacity-100 scale-100"
                leave="ease-in duration-150"
                leaveFrom="opacity-100 scale-100"
                leaveTo="opacity-0 scale-95"
              >
                <Dialog.Panel className="w-full max-w-md transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all p-5 space-y-4">
                  <div className="flex items-center justify-between pb-3 border-b border-mist">
                    <div>
                      <Dialog.Title as="h3" className="text-sm font-display font-bold text-ink">
                        Daftarkan Perangkat &amp; Garansi Baru
                      </Dialog.Title>
                      <span className="text-xs text-ink-muted">Input data BIOS ID sebelum unit diserahkan.</span>
                    </div>
                    <button onClick={() => setShowAddModal(false)} className="p-1 rounded-btn hover:bg-mist-light text-ink-muted">
                      <X className="w-4 h-4" />
                    </button>
                  </div>

                  {formError && (
                    <div className="p-2.5 bg-fault-subtle border border-fault-border text-fault text-xs rounded-btn flex items-center gap-2">
                      <AlertCircle className="w-4 h-4 flex-shrink-0" />
                      <span>{formError}</span>
                    </div>
                  )}

                  <form onSubmit={handleAddDevice} className="space-y-3">
                    <div className="space-y-1">
                      <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        MODEL PERANGKAT
                      </label>
                      <input
                        type="text"
                        required
                        value={model}
                        onChange={(e) => setModel(e.target.value)}
                        placeholder="Contoh: Lenovo ThinkCentre M700"
                        className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                      />
                    </div>

                    <div className="space-y-1">
                      <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        SERIAL NUMBER
                      </label>
                      <input
                        type="text"
                        required
                        value={serialNumber}
                        onChange={(e) => setSerialNumber(e.target.value)}
                        placeholder="Contoh: SN-LENOVO-988231"
                        className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn font-mono focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                      />
                    </div>

                    <div className="space-y-1">
                      <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        HARDWARE / BIOS ID (UUID)
                      </label>
                      <input
                        type="text"
                        required
                        value={hardwareId}
                        onChange={(e) => setHardwareId(e.target.value)}
                        placeholder="Contoh: MACHINE-f561d272e3324caca6df3ce4046f7c25"
                        className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn font-mono focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                      />
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                          TANGGAL BELI
                        </label>
                        <input
                          type="date"
                          required
                          value={purchaseDate}
                          onChange={(e) => setPurchaseDate(e.target.value)}
                          className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn text-ink"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                          DURASI GARANSI
                        </label>
                        <select
                          value={warrantyYears}
                          onChange={(e) => setWarrantyYears(Number(e.target.value))}
                          className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn text-ink"
                        >
                          <option value={1}>1 Tahun</option>
                          <option value={2}>2 Tahun</option>
                          <option value={3}>3 Tahun</option>
                        </select>
                      </div>
                    </div>

                    <div className="flex items-center justify-end gap-2 pt-3 border-t border-mist">
                      <button
                        type="button"
                        onClick={() => setShowAddModal(false)}
                        className="px-3.5 py-1.5 text-xs font-medium text-ink-muted hover:text-ink bg-white border border-mist rounded-btn hover:bg-mist-light transition-colors"
                      >
                        Batal
                      </button>
                      <button
                        type="submit"
                        disabled={formLoading}
                        className="px-4 py-1.5 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card transition-colors disabled:opacity-50"
                      >
                        {formLoading ? 'Menyimpan...' : 'Simpan Perangkat'}
                      </button>
                    </div>
                  </form>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </Dialog>
      </Transition>
    </div>
  );
};

