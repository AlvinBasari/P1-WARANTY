import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Dialog, Transition } from '@headlessui/react';
import { Fragment } from 'react';
import { toast } from 'sonner';
import api from '../services/api';
import { 
  Wrench, 
  MapPin, 
  MonitorPlay, 
  Truck, 
  CheckCircle, 
  X,
  Search, 
  RefreshCw,
  Cpu,
  User,
  Eye,
  Calendar,
  FileText
} from 'lucide-react';
import { ConnectRemoteButton } from '../components/ConnectRemoteButton';
import { LocationMapModal } from '../components/LocationMapModal';
import { TrackingStepperModal } from '../components/TrackingStepperModal';
import { TicketDetailModal } from '../components/TicketDetailModal';
import { ScheduleRemoteModal } from '../components/ScheduleRemoteModal';
import { InvoiceModal } from '../components/InvoiceModal';
import { TableSkeleton } from '../components/SkeletonLoader';
import { CopyButton } from '../components/CopyButton';

export const RepairRequests: React.FC = () => {
  const [searchParams] = useSearchParams();
  const initialFilter = searchParams.get('filter') || 'all';

  const [requests, setRequests] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [typeFilter, setTypeFilter] = useState<string>(
    initialFilter === 'unconfirmed' ? 'on_site' : initialFilter === 'remote' ? 'remote' : 'all'
  );
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Modals state
  const [selectedDetailReq, setSelectedDetailReq] = useState<any | null>(null);
  const [selectedLocationReq, setSelectedLocationReq] = useState<any | null>(null);
  const [selectedTrackingReq, setSelectedTrackingReq] = useState<any | null>(null);
  const [selectedScheduleReq, setSelectedScheduleReq] = useState<any | null>(null);
  const [selectedInvoiceReq, setSelectedInvoiceReq] = useState<any | null>(null);
  const [markingReq, setMarkingReq] = useState<any | null>(null);
  const [officeNotes, setOfficeNotes] = useState<string>('');
  const [submittingMark, setSubmittingMark] = useState(false);

  const fetchRequests = async () => {
    setLoading(true);
    try {
      const res = await api.get('/repair-requests');
      setRequests(res.data.data || []);
    } catch (e) {
      console.error(e);
      toast.error('Gagal mengambil daftar tiket.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRequests();
  }, []);

  const handleUpdateStatus = async (id: number, status: string) => {
    try {
      await api.put(`/repair-requests/${id}/status`, { status });
      toast.success(`Status tiket #${id} diubah ke ${status}`);
      fetchRequests();
    } catch (e: any) {
      toast.error(e.response?.data?.message || 'Gagal mengubah status.');
    }
  };

  const handleMarkOfficeRepair = async (id: number, needsOffice: boolean) => {
    setSubmittingMark(true);
    try {
      await api.put(`/repair-requests/${id}/mark-office-repair`, {
        needs_office_repair: needsOffice,
        notes: officeNotes || undefined,
      });
      toast.success(needsOffice ? 'Unit dijadwalkan dibawa ke Service Center!' : 'Kunjungan on-site ditandai selesai.');
      setMarkingReq(null);
      setOfficeNotes('');
      fetchRequests();
    } catch (e: any) {
      toast.error(e.response?.data?.message || 'Gagal menyimpan hasil kunjungan.');
    } finally {
      setSubmittingMark(false);
    }
  };

  const filteredRequests = requests.filter((r) => {
    if (typeFilter === 'remote' && r.type !== 'remote') return false;
    if (typeFilter === 'on_site' && r.type !== 'on_site') return false;
    if (typeFilter === 'unconfirmed' && (r.type !== 'on_site' || r.is_location_confirmed)) return false;
    if (statusFilter !== 'all' && r.status !== statusFilter) return false;
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const matchDevice = r.device?.model?.toLowerCase().includes(q);
      const matchCustomer = r.user?.name?.toLowerCase().includes(q);
      const matchDesc = r.description?.toLowerCase().includes(q);
      const matchId = r.id.toString().includes(q);
      const matchHw = r.device?.hardware_id?.toLowerCase().includes(q);
      const matchSn = r.device?.serial_number?.toLowerCase().includes(q);
      return matchDevice || matchCustomer || matchDesc || matchId || matchHw || matchSn;
    }
    return true;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-3 border-b border-mist">
        <div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight">
            Manajemen Tiket Servis &amp; Garansi
          </h1>
          <p className="text-xs text-ink-muted mt-0.5">
            Daftar antrean servis masuk: remote assistance via RustDesk dan penjadwalan kunjungan teknisi on-site.
          </p>
        </div>
        <button
          onClick={fetchRequests}
          disabled={loading}
          className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-white border border-mist rounded-btn text-xs font-medium text-ink-muted hover:text-ink hover:bg-mist-light shadow-card transition-all self-start"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-signal' : ''}`} />
          <span>Segarkan Data</span>
        </button>
      </div>

      {/* Filter Bar */}
      <div className="bg-paper-card p-3 rounded-card border border-mist shadow-card flex flex-col md:flex-row gap-3 justify-between items-center">
        <div className="flex flex-wrap items-center gap-2 w-full md:w-auto">
          <div className="flex rounded-btn bg-paper border border-mist p-0.5 text-xs font-medium">
            <button
              onClick={() => setTypeFilter('all')}
              className={`px-3 py-1 rounded-btn transition-all ${typeFilter === 'all' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
            >
              Semua
            </button>
            <button
              onClick={() => setTypeFilter('remote')}
              className={`px-3 py-1 rounded-btn transition-all ${typeFilter === 'remote' ? 'bg-signal text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
            >
              Remote
            </button>
            <button
              onClick={() => setTypeFilter('on_site')}
              className={`px-3 py-1 rounded-btn transition-all ${typeFilter === 'on_site' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
            >
              On-Site
            </button>
            <button
              onClick={() => setTypeFilter('unconfirmed')}
              className={`px-3 py-1 rounded-btn transition-all ${typeFilter === 'unconfirmed' ? 'bg-alert text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
            >
              Perlu Lokasi
            </button>
          </div>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-1.5 bg-paper border border-mist rounded-btn text-xs font-medium text-ink focus:outline-none focus:border-signal"
          >
            <option value="all">Semua Status</option>
            <option value="pending">Pending</option>
            <option value="scheduled">Scheduled</option>
            <option value="in_progress">In Progress</option>
            <option value="completed">Completed</option>
            <option value="rejected">Rejected</option>
          </select>
        </div>

        <div className="relative w-full md:w-80">
          <Search className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Cari ID, BIOS, serial, model, pelanggan..."
            className="w-full pl-8 pr-3 py-1.5 bg-paper border border-mist rounded-btn text-xs focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
          />
        </div>
      </div>

      {/* Requests Table */}
      <div className="bg-paper-card rounded-card border border-mist shadow-card overflow-hidden">
        {loading && requests.length === 0 ? (
          <TableSkeleton rows={6} />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-paper text-ink-subtle uppercase text-[10px] font-mono font-bold tracking-wider border-b border-mist">
                <tr>
                  <th className="px-5 py-3">Tiket &amp; Perangkat</th>
                  <th className="px-5 py-3">Pelanggan</th>
                  <th className="px-5 py-3">Tipe &amp; Kategori</th>
                  <th className="px-5 py-3">Status</th>
                  <th className="px-5 py-3 text-right">Aksi Operasional</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-mist">
                {filteredRequests.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-5 py-12 text-center text-ink-muted text-xs">
                      Tidak ada tiket yang sesuai dengan filter pencarian.
                    </td>
                  </tr>
                ) : (
                  filteredRequests.map((req) => {
                    const isRemote = req.type === 'remote';
                    const sessionId = req.remote_session?.rustdesk_session_id;
                    const attachmentsCount = Array.isArray(req.attachments) ? req.attachments.length : 0;

                    return (
                      <tr 
                        key={req.id} 
                        onClick={() => setSelectedDetailReq(req)}
                        className="hover:bg-mist-light/40 transition-colors cursor-pointer group"
                      >
                        <td className="px-5 py-3.5">
                          <div className="flex items-center gap-1.5 flex-wrap">
                            <span className="font-mono text-xs font-bold text-circuit bg-circuit/10 px-1.5 py-0.5 rounded-btn">
                              #{req.id}
                            </span>
                            <span className="font-semibold text-ink group-hover:text-signal transition-colors">
                              {req.device?.model || 'Unit Standar'}
                            </span>
                            {attachmentsCount > 0 && (
                              <span className="inline-flex items-center gap-1 text-[10px] font-mono text-ink-subtle bg-paper px-1.5 py-0.5 rounded-btn border border-mist">
                                <FileText className="w-3 h-3 text-circuit" />
                                <span>{attachmentsCount} Foto</span>
                              </span>
                            )}
                          </div>

                          {req.device?.hardware_id && (
                            <div 
                              className="flex items-center gap-1 font-mono text-[10px] text-ink-subtle mt-1"
                              onClick={(e) => e.stopPropagation()}
                            >
                              <span>BIOS: {req.device.hardware_id.slice(0, 18)}...</span>
                              <CopyButton text={req.device.hardware_id} label="BIOS ID" />
                            </div>
                          )}

                          <div className="text-[11px] text-ink-muted mt-1 max-w-sm line-clamp-2">
                            {req.description}
                          </div>
                        </td>

                        <td className="px-5 py-3.5">
                          <div className="font-medium text-ink flex items-center gap-1">
                            <User className="w-3.5 h-3.5 text-ink-subtle" />
                            <span>{req.user?.name || 'Pelanggan QR'}</span>
                          </div>
                          <div className="text-[11px] text-ink-subtle font-mono mt-0.5">{req.user?.phone || '-'}</div>
                        </td>

                        <td className="px-5 py-3.5 space-y-1">
                          <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-semibold ${
                            isRemote ? 'bg-signal-subtle text-signal border border-signal/20' : 'bg-circuit/10 text-circuit border border-circuit/20'
                          }`}>
                            {isRemote ? 'Remote (RustDesk)' : 'Servis On-Site'}
                          </span>
                          <div className="text-[10px] text-ink-subtle uppercase tracking-wider font-mono">
                            Kategori: {req.damage_category}
                          </div>
                        </td>

                        <td className="px-5 py-3.5" onClick={(e) => e.stopPropagation()}>
                          <select
                            value={req.status}
                            onChange={(e) => handleUpdateStatus(req.id, e.target.value)}
                            className={`text-[11px] font-semibold px-2.5 py-1 rounded-btn border focus:outline-none focus:border-signal ${
                              req.status === 'completed'
                                ? 'bg-stable-subtle text-stable border-stable/30'
                                : req.status === 'in_progress'
                                ? 'bg-circuit/10 text-circuit border-circuit/20'
                                : req.status === 'scheduled'
                                ? 'bg-signal-subtle text-signal border-signal/30'
                                : 'bg-alert-subtle text-alert border-alert-border'
                            }`}
                          >
                            <option value="pending">Pending</option>
                            <option value="scheduled">Scheduled</option>
                            <option value="in_progress">In Progress</option>
                            <option value="completed">Completed</option>
                            <option value="rejected">Rejected</option>
                          </select>
                        </td>

                        <td className="px-5 py-3.5 text-right" onClick={(e) => e.stopPropagation()}>
                          <div className="flex items-center justify-end gap-1.5 flex-wrap">
                            {/* Detail Button */}
                            <button
                              onClick={() => setSelectedDetailReq(req)}
                              className="inline-flex items-center gap-1 px-2.5 py-1 bg-paper hover:bg-mist-light text-ink-muted hover:text-ink rounded-btn text-xs font-medium border border-mist transition-colors"
                              title="Lihat rincian lengkap tiket"
                            >
                              <Eye className="w-3.5 h-3.5" />
                              <span>Rincian</span>
                            </button>

                            {/* Remote: Schedule Button */}
                            {isRemote && (
                              <button
                                onClick={() => setSelectedScheduleReq(req)}
                                className="inline-flex items-center gap-1 px-2.5 py-1 bg-paper hover:bg-signal-subtle text-signal border border-signal/30 rounded-btn text-xs font-semibold transition-colors"
                                title="Atur jadwal sesi remote"
                              >
                                <Calendar className="w-3.5 h-3.5" />
                                <span>{req.scheduled_at ? 'Jadwal' : 'Jadwalkan'}</span>
                              </button>
                            )}

                            {/* Remote: Connect Button */}
                            {isRemote && sessionId && (
                              <ConnectRemoteButton
                                sessionId={sessionId}
                                requestId={req.id}
                              />
                            )}

                            {/* On-Site: Location Map */}
                            {!isRemote && (
                              <button
                                onClick={() => setSelectedLocationReq(req)}
                                className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-btn text-xs font-medium border ${
                                  req.is_location_confirmed
                                    ? 'bg-stable-subtle border-stable/30 text-stable'
                                    : 'bg-alert text-white hover:bg-alert/90 shadow-card'
                                }`}
                              >
                                <MapPin className="w-3 h-3" />
                                <span>{req.is_location_confirmed ? 'Peta OK' : 'Verifikasi'}</span>
                              </button>
                            )}

                            {/* On-Site: Mark Result */}
                            {!isRemote && (
                              <button
                                onClick={() => setMarkingReq(req)}
                                className="px-2.5 py-1 bg-paper hover:bg-mist-light text-ink rounded-btn text-xs font-medium border border-mist"
                                title="Tandai Selesai di Tempat / Bawa ke Workshop"
                              >
                                Hasil
                              </button>
                            )}

                            {/* Service Center Tracking */}
                            {req.needs_office_repair && (
                              <button
                                onClick={() => setSelectedTrackingReq(req)}
                                className="inline-flex items-center gap-1 px-2.5 py-1 bg-circuit/10 hover:bg-circuit/20 text-circuit rounded-btn text-xs font-medium border border-circuit/20"
                              >
                                <Truck className="w-3 h-3" />
                                <span>Tracking</span>
                              </button>
                            )}

                            {/* Invoicing / Billing Button */}
                            <button
                              onClick={() => setSelectedInvoiceReq(req)}
                              className="inline-flex items-center gap-1 px-2.5 py-1 bg-circuit text-white hover:bg-circuit-dark rounded-btn text-xs font-semibold shadow-sm"
                              title="Buat / Kelola Faktur Perbaikan & Klaim Garansi"
                            >
                              <FileText className="w-3 h-3" />
                              <span>Invoice</span>
                            </button>
                          </div>
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

      {/* Modals */}
      <TicketDetailModal
        isOpen={!!selectedDetailReq}
        onClose={() => setSelectedDetailReq(null)}
        request={selectedDetailReq}
        onVerifyLocation={(req) => {
          setSelectedDetailReq(null);
          setSelectedLocationReq(req);
        }}
        onOpenTracking={(req) => {
          setSelectedDetailReq(null);
          setSelectedTrackingReq(req);
        }}
        onScheduleRemote={(req) => {
          setSelectedDetailReq(null);
          setSelectedScheduleReq(req);
        }}
        onOpenInvoice={(req) => {
          setSelectedDetailReq(null);
          setSelectedInvoiceReq(req);
        }}
      />

      <InvoiceModal
        isOpen={!!selectedInvoiceReq}
        onClose={() => setSelectedInvoiceReq(null)}
        repairRequest={selectedInvoiceReq}
        onSaved={fetchRequests}
      />

      <LocationMapModal
        isOpen={!!selectedLocationReq}
        onClose={() => setSelectedLocationReq(null)}
        request={selectedLocationReq}
        onSuccess={fetchRequests}
      />

      <TrackingStepperModal
        isOpen={!!selectedTrackingReq}
        onClose={() => setSelectedTrackingReq(null)}
        requestId={selectedTrackingReq?.id || null}
        currentStatus={selectedTrackingReq?.tracking?.current_status || 'di_service_center'}
        onSuccess={fetchRequests}
      />

      <ScheduleRemoteModal
        isOpen={!!selectedScheduleReq}
        onClose={() => setSelectedScheduleReq(null)}
        request={selectedScheduleReq}
        onSuccess={fetchRequests}
      />

      {/* Modal: Mark Office Repair (FR-18) via Headless UI */}
      <Transition appear show={!!markingReq} as={Fragment}>
        <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={() => setMarkingReq(null)}>
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
                        Hasil Kunjungan Teknisi Lapangan (FR-18)
                      </Dialog.Title>
                      <span className="text-xs text-ink-muted">Tiket #{markingReq?.id} • {markingReq?.device?.model}</span>
                    </div>
                    <button onClick={() => setMarkingReq(null)} className="p-1 rounded-btn hover:bg-mist-light text-ink-muted">
                      <X className="w-4 h-4" />
                    </button>
                  </div>

                  <div className="space-y-1.5">
                    <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                      CATATAN PEMERIKSAAN TEKNISI
                    </label>
                    <textarea
                      rows={3}
                      value={officeNotes}
                      onChange={(e) => setOfficeNotes(e.target.value)}
                      placeholder="Diagnosa on-site atau kebutuhan sparepart khusus..."
                      className="w-full text-xs p-2.5 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-2.5 pt-2">
                    <button
                      type="button"
                      disabled={submittingMark}
                      onClick={() => handleMarkOfficeRepair(markingReq.id, false)}
                      className="p-3 rounded-card border border-stable/30 bg-stable-subtle hover:bg-stable/20 text-stable text-xs font-semibold text-center transition-colors"
                    >
                      <CheckCircle className="w-5 h-5 mx-auto mb-1 text-stable" />
                      <span>Selesai di Tempat</span>
                    </button>

                    <button
                      type="button"
                      disabled={submittingMark}
                      onClick={() => handleMarkOfficeRepair(markingReq.id, true)}
                      className="p-3 rounded-card border border-circuit/30 bg-circuit/10 hover:bg-circuit/20 text-circuit text-xs font-semibold text-center transition-colors"
                    >
                      <Truck className="w-5 h-5 mx-auto mb-1 text-circuit" />
                      <span>Bawa ke Service Center</span>
                    </button>
                  </div>
                </Dialog.Panel>
              </Transition.Child>
            </div>
          </div>
        </Dialog>
      </Transition>
    </div>
  );
};

