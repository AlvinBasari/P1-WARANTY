import React, { useState } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { Truck, X, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import api from '../services/api';

interface TrackingStepperModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  requestId: number | null;
  currentStatus: string;
}

const STAGES = [
  { id: 'dijemput', label: '1. Dijemput oleh Kurir/Teknisi', desc: 'Unit diambil dari alamat pelanggan' },
  { id: 'di_service_center', label: '2. Tiba di Service Center', desc: 'Diterima di workshop pusat untuk registrasi' },
  { id: 'sedang_diperbaiki', label: '3. Sedang Diperbaiki', desc: 'Pengerjaan penggantian part & re-assembly' },
  { id: 'selesai', label: '4. Selesai & Lolos QC', desc: 'Pengujian fungsional dan kelayakan selesai' },
  { id: 'dikembalikan', label: '5. Dikembalikan ke Pelanggan', desc: 'Proses pengiriman kembali ke alamat pemilik' },
];

export const TrackingStepperModal: React.FC<TrackingStepperModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  requestId,
  currentStatus,
}) => {
  const [selectedStatus, setSelectedStatus] = useState<string>(currentStatus || 'di_service_center');
  const [notes, setNotes] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!requestId) return;

    setLoading(true);
    try {
      await api.post(`/repair-requests/${requestId}/tracking/progress`, {
        status: selectedStatus,
        notes: notes || `Pembaruan tahapan status menjadi: ${selectedStatus}`,
      });
      toast.success(`Linimasa perbaikan tiket #${requestId} berhasil diperbarui!`);
      onSuccess();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Gagal memperbarui status tracking.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={onClose}>
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
              <Dialog.Panel className="w-full max-w-lg transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all">
                {/* Header */}
                <div className="flex items-center justify-between px-5 py-4 border-b border-mist bg-paper">
                  <div className="flex items-center gap-2.5">
                    <div className="p-1.5 rounded-btn bg-circuit/10 text-circuit">
                      <Truck className="w-4 h-4" strokeWidth={1.75} />
                    </div>
                    <div>
                      <Dialog.Title as="h3" className="text-sm font-display font-bold text-ink">
                        Perbarui Linimasa Service Center (FR-20)
                      </Dialog.Title>
                      <span className="text-xs text-ink-muted">Tiket Servis #{requestId}</span>
                    </div>
                  </div>
                  <button
                    onClick={onClose}
                    className="p-1 text-ink-muted hover:text-ink hover:bg-mist-light rounded-btn transition-colors"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>

                {/* Form */}
                <form onSubmit={handleSubmit} className="p-5 space-y-4">
                  <div className="space-y-2">
                    <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                      PILIH TAHAPAN PROGRES TERKINI
                    </label>
                    <div className="space-y-1.5">
                      {STAGES.map((stage) => {
                        const isSelected = selectedStatus === stage.id;
                        return (
                          <label
                            key={stage.id}
                            className={`flex items-start gap-3 p-2.5 rounded-card border text-xs cursor-pointer transition-all ${
                              isSelected
                                ? 'bg-signal-subtle border-signal/40 text-ink'
                                : 'bg-paper hover:bg-mist-light border-mist text-ink-muted'
                            }`}
                          >
                            <input
                              type="radio"
                              name="stage"
                              value={stage.id}
                              checked={isSelected}
                              onChange={() => setSelectedStatus(stage.id)}
                              className="mt-0.5 accent-signal"
                            />
                            <div>
                              <div className={`font-semibold ${isSelected ? 'text-signal-hover' : 'text-ink'}`}>
                                {stage.label}
                              </div>
                              <div className="text-[11px] text-ink-subtle mt-0.5">
                                {stage.desc}
                              </div>
                            </div>
                          </label>
                        );
                      })}
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                      CATATAN / LOG PENGERJAAN TEKNISI
                    </label>
                    <textarea
                      rows={3}
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      placeholder="Contoh: Penggantian modul keyboard dan pembersihan motherboard selesai..."
                      className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                    />
                  </div>

                  {/* Footer */}
                  <div className="flex items-center justify-end gap-2 pt-3 border-t border-mist">
                    <button
                      type="button"
                      onClick={onClose}
                      className="px-3.5 py-1.5 text-xs font-medium text-ink-muted hover:text-ink bg-white border border-mist rounded-btn hover:bg-mist-light transition-colors"
                    >
                      Batal
                    </button>
                    <button
                      type="submit"
                      disabled={loading}
                      className="inline-flex items-center gap-1.5 px-4 py-1.5 text-xs font-semibold text-white bg-signal hover:bg-signal-hover rounded-btn shadow-card transition-colors disabled:opacity-50"
                    >
                      <span>{loading ? 'Menyimpan...' : 'Simpan Progres'}</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </form>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};
