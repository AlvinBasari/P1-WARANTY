import React, { useState } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { MapPin, X, Check, ExternalLink } from 'lucide-react';
import { toast } from 'sonner';
import api from '../services/api';
import { CopyButton } from './CopyButton';

interface LocationMapModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  request: {
    id: number;
    latitude: number;
    longitude: number;
    location_label?: string;
    is_location_confirmed: boolean;
    device?: {
      model: string;
      user?: {
        name: string;
        phone: string;
        address: string;
      };
    };
  } | null;
}

export const LocationMapModal: React.FC<LocationMapModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  request,
}) => {
  const [loading, setLoading] = useState(false);

  if (!request) return null;

  const lat = Number(request.latitude) || -6.2088;
  const lng = Number(request.longitude) || 106.8456;
  const googleMapsUrl = `https://www.google.com/maps?q=${lat},${lng}`;
  const osmEmbedUrl = `https://www.openstreetmap.org/export/embed.html?bbox=${lng - 0.01}%2C${lat - 0.008}%2C${lng + 0.01}%2C${lat + 0.008}&layer=mapnik&marker=${lat}%2C${lng}`;

  const handleConfirm = async () => {
    setLoading(true);
    try {
      await api.put(`/repair-requests/${request.id}/confirm-location`);
      toast.success(`Lokasi tiket #${request.id} berhasil diverifikasi!`);
      onSuccess();
      onClose();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Gagal memverifikasi lokasi.');
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
              <Dialog.Panel className="w-full max-w-2xl transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all">
                {/* Header */}
                <div className="flex items-center justify-between px-5 py-4 border-b border-mist bg-paper">
                  <div className="flex items-center gap-2.5">
                    <div className="p-1.5 rounded-btn bg-signal/10 text-signal">
                      <MapPin className="w-4 h-4" strokeWidth={1.75} />
                    </div>
                    <div>
                      <Dialog.Title as="h3" className="text-sm font-display font-bold text-ink">
                        Verifikasi Titik Lokasi Servis On-Site (FR-15)
                      </Dialog.Title>
                      <span className="text-xs text-ink-muted">Tiket Servis #{request.id} • {request.device?.model}</span>
                    </div>
                  </div>
                  <button
                    onClick={onClose}
                    className="p-1 text-ink-muted hover:text-ink hover:bg-mist-light rounded-btn transition-colors"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>

                {/* Body */}
                <div className="p-5 space-y-4">
                  {/* Coordinates & Customer Details Card */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs bg-paper p-3.5 rounded-card border border-mist">
                    <div className="space-y-1.5">
                      <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        KOORDINAT GPS TERSIMPAN
                      </span>
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-xs font-semibold text-circuit">
                          {lat.toFixed(6)}, {lng.toFixed(6)}
                        </span>
                        <CopyButton text={`${lat},${lng}`} label="Koordinat GPS" />
                      </div>
                      <div className="text-ink-muted">
                        Label: <span className="font-medium text-ink">{request.location_label || 'Lokasi Terdaftar'}</span>
                      </div>
                    </div>

                    <div className="space-y-1.5">
                      <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        DATA PELANGGAN &amp; ALAMAT
                      </span>
                      <div className="font-medium text-ink">{request.device?.user?.name || '-'}</div>
                      <div className="text-ink-muted">{request.device?.user?.phone || '-'}</div>
                      <div className="text-ink-subtle text-[11px] truncate">{request.device?.user?.address || '-'}</div>
                    </div>
                  </div>

                  {/* Embedded OpenStreetMap Preview */}
                  <div className="border border-mist rounded-card overflow-hidden h-64 bg-mist/20 relative">
                    <iframe
                      title="Lokasi Servis On-Site"
                      width="100%"
                      height="100%"
                      src={osmEmbedUrl}
                      className="border-0"
                    />
                    <a
                      href={googleMapsUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="absolute bottom-2.5 right-2.5 inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium bg-white/90 hover:bg-white text-circuit rounded-btn shadow-card border border-mist transition-colors"
                    >
                      <span>Buka di Google Maps</span>
                      <ExternalLink className="w-3 h-3" />
                    </a>
                  </div>
                </div>

                {/* Footer */}
                <div className="flex items-center justify-between px-5 py-3.5 border-t border-mist bg-paper">
                  <span className="text-xs text-ink-muted">
                    {request.is_location_confirmed
                      ? '✓ Titik lokasi ini sudah terverifikasi sebelumnya.'
                      : 'Konfirmasi lokasi untuk menjadwalkan teknisi lapangan.'}
                  </span>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={onClose}
                      className="px-3.5 py-1.5 text-xs font-medium text-ink-muted hover:text-ink bg-white border border-mist rounded-btn hover:bg-mist-light transition-colors"
                    >
                      Batal
                    </button>
                    {!request.is_location_confirmed && (
                      <button
                        type="button"
                        disabled={loading}
                        onClick={handleConfirm}
                        className="inline-flex items-center gap-1.5 px-4 py-1.5 text-xs font-semibold text-white bg-signal hover:bg-signal-hover rounded-btn shadow-card transition-colors disabled:opacity-50"
                      >
                        <Check className="w-3.5 h-3.5" />
                        <span>{loading ? 'Menyimpan...' : 'Konfirmasi Lokasi Ini'}</span>
                      </button>
                    )}
                  </div>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};
