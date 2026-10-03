import React, { useState } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { Calendar, X, Clock, ArrowRight, MonitorPlay } from 'lucide-react';
import { toast } from 'sonner';
import api from '../services/api';

interface ScheduleRemoteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  request: any | null;
}

export const ScheduleRemoteModal: React.FC<ScheduleRemoteModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  request,
}) => {
  const [scheduledAt, setScheduledAt] = useState<string>('');
  const [notes, setNotes] = useState<string>('Teknisi kami siap melakukan remote diagnosa pada waktu yang dijadwalkan. Mohon pastikan perangkat terhubung ke internet.');
  const [loading, setLoading] = useState(false);

  if (!request) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!scheduledAt) {
      toast.error('Pilih tanggal dan waktu sesi remote terlebih dahulu.');
      return;
    }

    setLoading(true);
    try {
      await api.put(`/repair-requests/${request.id}/schedule-remote`, {
        scheduled_at: scheduledAt,
        notes: notes.trim(),
      });
      toast.success(`Jadwal sesi remote tiket #${request.id} berhasil ditetapkan!`);
      onSuccess();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Gagal mengatur jadwal remote.');
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
              <Dialog.Panel className="w-full max-w-md transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all p-5 space-y-4">
                {/* Header */}
                <div className="flex items-center justify-between pb-3 border-b border-mist">
                  <div className="flex items-center gap-2.5">
                    <div className="p-1.5 rounded-btn bg-signal/10 text-signal">
                      <MonitorPlay className="w-4 h-4" strokeWidth={1.75} />
                    </div>
                    <div>
                      <Dialog.Title as="h3" className="text-sm font-display font-bold text-ink">
                        Tetapkan Jadwal Sesi Remote
                      </Dialog.Title>
                      <span className="text-xs text-ink-muted">Tiket #{request.id} • {request.device?.model}</span>
                    </div>
                  </div>
                  <button onClick={onClose} className="p-1 rounded-btn hover:bg-mist-light text-ink-muted">
                    <X className="w-4 h-4" />
                  </button>
                </div>

                {/* Form */}
                <form onSubmit={handleSubmit} className="space-y-3.5">
                  <div className="space-y-1">
                    <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                      WAKTU SESI REMOTE
                    </label>
                    <div className="relative">
                      <input
                        type="datetime-local"
                        required
                        value={scheduledAt}
                        onChange={(e) => setScheduledAt(e.target.value)}
                        className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                      CATATAN / PANDUAN UNTUK PELANGGAN
                    </label>
                    <textarea
                      rows={3}
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      placeholder="Instruksi khusus sebelum sesi dimulai..."
                      className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                    />
                  </div>

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
                      <span>{loading ? 'Menyimpan...' : 'Konfirmasi Jadwal'}</span>
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
