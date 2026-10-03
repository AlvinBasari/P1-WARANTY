import React from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { 
  Wrench, 
  X, 
  User, 
  Phone, 
  Mail, 
  MapPin, 
  Cpu, 
  Calendar, 
  MonitorPlay, 
  Truck, 
  FileText, 
  ExternalLink,
  ShieldCheck,
  Clock,
  CheckCircle2,
  AlertTriangle
} from 'lucide-react';
import { CopyButton } from './CopyButton';
import { ConnectRemoteButton } from './ConnectRemoteButton';

interface TicketDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  request: any | null;
  onVerifyLocation?: (req: any) => void;
  onOpenTracking?: (req: any) => void;
  onScheduleRemote?: (req: any) => void;
  onOpenInvoice?: (req: any) => void;
}

export const TicketDetailModal: React.FC<TicketDetailModalProps> = ({
  isOpen,
  onClose,
  request,
  onVerifyLocation,
  onOpenTracking,
  onScheduleRemote,
  onOpenInvoice,
}) => {
  if (!request) return null;

  const isRemote = request.type === 'remote';
  const sessionId = request.remote_session?.rustdesk_session_id;
  const attachments = Array.isArray(request.attachments) ? request.attachments : [];
  const warranty = request.device?.warranty;

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'completed':
        return <span className="px-2.5 py-0.5 rounded-btn text-xs font-semibold bg-stable-subtle text-stable border border-stable/30 flex items-center gap-1"><CheckCircle2 className="w-3 h-3" /> Selesai</span>;
      case 'in_progress':
        return <span className="px-2.5 py-0.5 rounded-btn text-xs font-semibold bg-circuit/10 text-circuit border border-circuit/20 flex items-center gap-1"><Clock className="w-3 h-3" /> Dalam Proses</span>;
      case 'scheduled':
        return <span className="px-2.5 py-0.5 rounded-btn text-xs font-semibold bg-signal-subtle text-signal border border-signal/30 flex items-center gap-1"><Calendar className="w-3 h-3" /> Dijadwalkan</span>;
      case 'rejected':
        return <span className="px-2.5 py-0.5 rounded-btn text-xs font-semibold bg-fault-subtle text-fault border border-fault-border flex items-center gap-1"><X className="w-3 h-3" /> Ditolak</span>;
      default:
        return <span className="px-2.5 py-0.5 rounded-btn text-xs font-semibold bg-alert-subtle text-alert border border-alert-border flex items-center gap-1"><AlertTriangle className="w-3 h-3" /> Menunggu Peninjauan</span>;
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
                <div className="flex items-center justify-between px-6 py-4 border-b border-mist bg-paper">
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-btn bg-circuit text-white">
                      <Wrench className="w-5 h-5" strokeWidth={1.75} />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-sm font-bold text-circuit">
                          Tiket Servis #{request.id}
                        </span>
                        {getStatusBadge(request.status)}
                      </div>
                      <span className="text-xs text-ink-muted">
                        Dibuat pada: {request.created_at ? new Date(request.created_at).toLocaleString('id-ID', { dateStyle: 'medium', timeStyle: 'short' }) : '-'}
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={onClose}
                    className="p-1.5 text-ink-muted hover:text-ink hover:bg-mist-light rounded-btn transition-colors"
                  >
                    <X className="w-5 h-5" />
                  </button>
                </div>

                {/* Body Content */}
                <div className="p-6 space-y-5 max-h-[75vh] overflow-y-auto">
                  {/* Category & Service Type Chips */}
                  <div className="flex flex-wrap items-center gap-2 pb-2 border-b border-mist">
                    <span className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-btn text-xs font-semibold ${
                      isRemote ? 'bg-signal-subtle text-signal border border-signal/20' : 'bg-circuit/10 text-circuit border border-circuit/20'
                    }`}>
                      {isRemote ? <MonitorPlay className="w-3.5 h-3.5" /> : <MapPin className="w-3.5 h-3.5" />}
                      <span>Kategori Layanan: {isRemote ? 'Bantuan Remote (RustDesk)' : 'Kunjungan Lapangan (On-Site)'}</span>
                    </span>

                    <span className="inline-flex items-center gap-1 px-3 py-1 rounded-btn text-xs font-medium bg-paper border border-mist text-ink-muted">
                      Kerusakan: <strong className="text-ink uppercase">{request.damage_category}</strong>
                    </span>
                  </div>

                  {/* Device & Customer Details Grid */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Device Card */}
                    <div className="bg-paper p-4 rounded-card border border-mist space-y-2.5">
                      <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle flex items-center gap-1.5">
                        <Cpu className="w-3.5 h-3.5 text-circuit" />
                        <span>INFORMASI PERANGKAT</span>
                      </div>
                      <div>
                        <div className="text-sm font-bold text-ink">{request.device?.model || 'Unit Standar'}</div>
                        <div className="text-xs text-ink-muted font-mono mt-0.5">SN: {request.device?.serial_number || '-'}</div>
                      </div>
                      {request.device?.hardware_id && (
                        <div className="flex items-center justify-between text-xs bg-paper-card p-2 rounded-btn border border-mist">
                          <span className="font-mono text-[11px] text-circuit truncate max-w-[200px]" title={request.device.hardware_id}>
                            {request.device.hardware_id}
                          </span>
                          <CopyButton text={request.device.hardware_id} label="Hardware ID" />
                        </div>
                      )}
                      {warranty && (
                        <div className="text-[11px] text-ink-muted pt-1 flex items-center gap-1">
                          <ShieldCheck className="w-3.5 h-3.5 text-stable" />
                          <span>Garansi s/d: <strong className="text-ink">{warranty.warranty_end || '-'}</strong></span>
                        </div>
                      )}
                    </div>

                    {/* Customer Card */}
                    <div className="bg-paper p-4 rounded-card border border-mist space-y-2.5">
                      <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle flex items-center gap-1.5">
                        <User className="w-3.5 h-3.5 text-circuit" />
                        <span>DATA PELANGGAN</span>
                      </div>
                      <div>
                        <div className="text-sm font-bold text-ink">{request.user?.name || 'Pelanggan QR'}</div>
                        <div className="text-xs text-ink-muted flex items-center gap-1.5 mt-1">
                          <Phone className="w-3 h-3 text-ink-subtle" />
                          <span>{request.user?.phone || '-'}</span>
                        </div>
                        <div className="text-xs text-ink-muted flex items-center gap-1.5 mt-0.5">
                          <Mail className="w-3 h-3 text-ink-subtle" />
                          <span>{request.user?.email || '-'}</span>
                        </div>
                      </div>
                      {request.user?.address && (
                        <div className="text-[11px] text-ink-muted border-t border-mist/80 pt-1.5 line-clamp-2">
                          {request.user.address}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Problem Description */}
                  <div className="space-y-1.5">
                    <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle flex items-center gap-1.5">
                      <FileText className="w-3.5 h-3.5 text-circuit" />
                      <span>DESKRIPSI KENDALA / KELUHAN</span>
                    </div>
                    <div className="p-3.5 bg-paper rounded-card border border-mist text-xs text-ink whitespace-pre-wrap leading-relaxed">
                      {request.description || 'Tidak ada catatan keluhan.'}
                    </div>
                  </div>

                  {/* Attachments / Photos */}
                  {attachments.length > 0 && (
                    <div className="space-y-2">
                      <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                        FOTO BUKTI / LAMPIRAN KENDALA ({attachments.length})
                      </div>
                      <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
                        {attachments.map((url: string, idx: number) => (
                          <a
                            key={idx}
                            href={url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="group relative aspect-video bg-paper rounded-btn border border-mist overflow-hidden flex items-center justify-center hover:border-signal transition-colors"
                          >
                            <img
                              src={url}
                              alt={`Lampiran ${idx + 1}`}
                              className="w-full h-full object-cover group-hover:scale-105 transition-transform"
                            />
                            <div className="absolute inset-0 bg-ink/30 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center text-white text-xs font-semibold gap-1">
                              <span>Perbesar</span>
                              <ExternalLink className="w-3 h-3" />
                            </div>
                          </a>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Remote Session Info (if remote) */}
                  {isRemote && (
                    <div className="p-3.5 bg-signal-subtle/50 rounded-card border border-signal/20 space-y-2">
                      <div className="flex items-center justify-between">
                        <div className="text-xs font-bold text-signal flex items-center gap-1.5">
                          <MonitorPlay className="w-4 h-4" />
                          <span>Status Sesi Remote RustDesk</span>
                        </div>
                        {request.scheduled_at && (
                          <span className="text-[11px] font-mono font-semibold text-circuit bg-paper px-2 py-0.5 rounded-btn border border-mist">
                            Jadwal: {new Date(request.scheduled_at).toLocaleString('id-ID', { dateStyle: 'short', timeStyle: 'short' })}
                          </span>
                        )}
                      </div>

                      <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
                        <div className="font-mono text-xs">
                          Session ID: <strong className="text-circuit">{sessionId || 'Belum tersedia'}</strong>
                        </div>
                        {sessionId && (
                          <ConnectRemoteButton sessionId={sessionId} requestId={request.id} />
                        )}
                      </div>
                    </div>
                  )}

                  {/* On-Site Location Status (if on_site) */}
                  {!isRemote && (
                    <div className="p-3.5 bg-alert-subtle/50 rounded-card border border-alert-border space-y-2">
                      <div className="flex items-center justify-between">
                        <div className="text-xs font-bold text-ink flex items-center gap-1.5">
                          <MapPin className="w-4 h-4 text-alert" />
                          <span>Status Koordinat Lokasi Servis</span>
                        </div>
                        <span className={`text-[10px] font-semibold px-2 py-0.5 rounded-btn ${
                          request.is_location_confirmed ? 'bg-stable-subtle text-stable' : 'bg-alert text-white'
                        }`}>
                          {request.is_location_confirmed ? '✓ Lokasi Terkonfirmasi' : 'Belum Diverifikasi'}
                        </span>
                      </div>
                      <div className="text-xs text-ink-muted">
                        Koordinat: <span className="font-mono text-ink font-semibold">{request.latitude || request.location_lat || '-'}, {request.longitude || request.location_lng || '-'}</span>
                      </div>
                    </div>
                  )}

                  {/* Office Tracking Info (if needs_office_repair) */}
                  {request.needs_office_repair && (
                    <div className="p-3.5 bg-circuit/10 rounded-card border border-circuit/20 space-y-2">
                      <div className="flex items-center justify-between">
                        <div className="text-xs font-bold text-circuit flex items-center gap-1.5">
                          <Truck className="w-4 h-4" />
                          <span>Linimasa Service Center (Workshop Pusat)</span>
                        </div>
                        <span className="text-[10px] font-mono font-bold uppercase tracking-wider bg-circuit text-white px-2 py-0.5 rounded-btn">
                          {request.tracking?.current_status?.replace(/_/g, ' ') || 'dijemput'}
                        </span>
                      </div>
                    </div>
                  )}
                </div>

                {/* Footer Actions */}
                <div className="flex flex-wrap items-center justify-between gap-3 px-6 py-4 border-t border-mist bg-paper">
                  <span className="text-xs text-ink-muted">
                    Gunakan tombol di samping untuk tindak lanjut cepat.
                  </span>
                  <div className="flex items-center gap-2">
                    {/* Action for Remote: Schedule */}
                    {isRemote && onScheduleRemote && (
                      <button
                        onClick={() => onScheduleRemote(request)}
                        className="px-3.5 py-1.5 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1.5"
                      >
                        <Calendar className="w-3.5 h-3.5" />
                        <span>Atur Jadwal Remote</span>
                      </button>
                    )}

                    {/* Action for On-Site: Location Map */}
                    {!isRemote && onVerifyLocation && (
                      <button
                        onClick={() => onVerifyLocation(request)}
                        className="px-3.5 py-1.5 bg-alert hover:bg-alert/90 text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1.5"
                      >
                        <MapPin className="w-3.5 h-3.5" />
                        <span>{request.is_location_confirmed ? 'Lihat Peta' : 'Verifikasi Peta'}</span>
                      </button>
                    )}

                    {/* Action for Office Tracking */}
                    {request.needs_office_repair && onOpenTracking && (
                      <button
                        onClick={() => onOpenTracking(request)}
                        className="px-3.5 py-1.5 bg-circuit hover:bg-circuit-dark text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1.5"
                      >
                        <Truck className="w-3.5 h-3.5" />
                        <span>Update Tracking</span>
                      </button>
                    )}

                    {/* Action for Invoice & Warranty Deduction */}
                    {onOpenInvoice && (
                      <button
                        onClick={() => onOpenInvoice(request)}
                        className="px-3.5 py-1.5 bg-circuit-dark hover:bg-circuit text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1.5 border border-circuit-light/30"
                      >
                        <FileText className="w-3.5 h-3.5" />
                        <span>Faktur / Invoice</span>
                      </button>
                    )}

                    <button
                      onClick={onClose}
                      className="px-4 py-1.5 text-xs font-medium text-ink-muted hover:text-ink bg-white border border-mist rounded-btn hover:bg-mist-light transition-colors"
                    >
                      Tutup
                    </button>
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
