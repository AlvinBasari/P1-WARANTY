import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';
import { 
  Wrench, 
  MonitorPlay, 
  MapPin, 
  Truck, 
  ShieldCheck, 
  ArrowRight, 
  AlertTriangle,
  RefreshCw,
  Clock,
  Eye,
  Calendar,
  CheckCircle2,
  FileText
} from 'lucide-react';
import { ConnectRemoteButton } from '../components/ConnectRemoteButton';
import { LocationMapModal } from '../components/LocationMapModal';
import { TrackingStepperModal } from '../components/TrackingStepperModal';
import { TicketDetailModal } from '../components/TicketDetailModal';
import { ScheduleRemoteModal } from '../components/ScheduleRemoteModal';
import { TableSkeleton } from '../components/SkeletonLoader';
import { CopyButton } from '../components/CopyButton';

export const Dashboard: React.FC = () => {
  const [requests, setRequests] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterTab, setFilterTab] = useState<'all' | 'remote' | 'on_site' | 'unconfirmed' | 'tracking'>('all');
  
  // Modals state
  const [selectedDetailReq, setSelectedDetailReq] = useState<any | null>(null);
  const [selectedLocationReq, setSelectedLocationReq] = useState<any | null>(null);
  const [selectedTrackingReq, setSelectedTrackingReq] = useState<any | null>(null);
  const [selectedScheduleReq, setSelectedScheduleReq] = useState<any | null>(null);

  const fetchDashboardData = async () => {
    setLoading(true);
    try {
      const res = await api.get('/repair-requests');
      setRequests(res.data.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
    const interval = setInterval(fetchDashboardData, 15000); // Polling every 15s (FR-05)
    return () => clearInterval(interval);
  }, []);

  const remoteCount = requests.filter((r) => r.type === 'remote').length;
  const onsiteCount = requests.filter((r) => r.type === 'on_site').length;
  const unconfirmedCount = requests.filter((r) => r.type === 'on_site' && !r.is_location_confirmed).length;
  const trackingCount = requests.filter((r) => r.needs_office_repair && r.tracking).length;

  const filteredRequests = requests.filter((r) => {
    if (filterTab === 'remote') return r.type === 'remote';
    if (filterTab === 'on_site') return r.type === 'on_site';
    if (filterTab === 'unconfirmed') return r.type === 'on_site' && !r.is_location_confirmed;
    if (filterTab === 'tracking') return r.needs_office_repair && r.tracking;
    return true;
  });

  return (
    <div className="space-y-6">
      {/* Top Banner & Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-3 border-b border-mist">
        <div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight flex items-center gap-2.5">
            <span>Dashboard Kontrol Operasional</span>
            <span className="text-xs px-2.5 py-0.5 rounded-full font-sans font-medium bg-signal-subtle text-signal border border-signal/20">
              Live Real-Time
            </span>
          </h1>
          <p className="text-xs text-ink-muted mt-0.5">
            Pusat kendali diagnostik remote via RustDesk, penjadwalan teknisi on-site, dan tracking workshop.
          </p>
        </div>
        <button
          onClick={fetchDashboardData}
          disabled={loading}
          className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-white border border-mist rounded-btn text-xs font-medium text-ink-muted hover:text-ink hover:bg-mist-light shadow-card transition-all self-start"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-signal' : ''}`} />
          <span>Segarkan Data</span>
        </button>
      </div>

      {/* Unconfirmed Location Alert Banner (FR-15) */}
      {unconfirmedCount > 0 && (
        <div className="p-4 bg-alert-subtle rounded-card border border-alert-border flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-card">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-alert text-white rounded-btn shadow-sm flex-shrink-0">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <div>
              <div className="text-xs font-bold text-ink">
                Perhatian: {unconfirmedCount} Permintaan Servis On-Site Membutuhkan Konfirmasi Titik Lokasi
              </div>
              <div className="text-[11px] text-ink-muted mt-0.5">
                Koordinat GPS pelanggan telah disematkan. Harap verifikasi peta sebelum mengirimkan teknisi ke alamat.
              </div>
            </div>
          </div>
          <button
            onClick={() => setFilterTab('unconfirmed')}
            className="inline-flex items-center gap-1 px-3.5 py-1.5 bg-alert hover:bg-alert/90 text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex-shrink-0 self-start sm:self-auto"
          >
            <span>Tinjau Lokasi ({unconfirmedCount})</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Remote */}
        <div 
          onClick={() => setFilterTab(filterTab === 'remote' ? 'all' : 'remote')}
          className={`bg-paper-card p-4 rounded-card border shadow-card transition-all cursor-pointer hover:border-signal ${
            filterTab === 'remote' ? 'border-signal ring-2 ring-signal/20' : 'border-mist'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
              TIKET REMOTE
            </span>
            <div className="p-2 bg-signal-subtle text-signal rounded-btn">
              <MonitorPlay className="w-4 h-4" strokeWidth={1.75} />
            </div>
          </div>
          <div className="text-2xl font-display font-bold text-ink mt-1">{remoteCount}</div>
          <div className="text-[11px] text-signal font-medium mt-0.5 flex items-center justify-between">
            <span>Diagnosa via RustDesk</span>
            <span className="text-[10px] text-ink-subtle">Klik filter</span>
          </div>
        </div>

        {/* On-Site */}
        <div 
          onClick={() => setFilterTab(filterTab === 'on_site' ? 'all' : 'on_site')}
          className={`bg-paper-card p-4 rounded-card border shadow-card transition-all cursor-pointer hover:border-alert ${
            filterTab === 'on_site' ? 'border-alert ring-2 ring-alert/20' : 'border-mist'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
              SERVIS ON-SITE
            </span>
            <div className="p-2 bg-alert-subtle text-alert rounded-btn">
              <MapPin className="w-4 h-4" strokeWidth={1.75} />
            </div>
          </div>
          <div className="text-2xl font-display font-bold text-ink mt-1">{onsiteCount}</div>
          <div className="text-[11px] text-alert font-medium mt-0.5 flex items-center justify-between">
            <span>{unconfirmedCount} perlu verifikasi</span>
            <span className="text-[10px] text-ink-subtle">Klik filter</span>
          </div>
        </div>

        {/* Tracking */}
        <div 
          onClick={() => setFilterTab(filterTab === 'tracking' ? 'all' : 'tracking')}
          className={`bg-paper-card p-4 rounded-card border shadow-card transition-all cursor-pointer hover:border-circuit ${
            filterTab === 'tracking' ? 'border-circuit ring-2 ring-circuit/20' : 'border-mist'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
              DI SERVICE CENTER
            </span>
            <div className="p-2 bg-circuit/10 text-circuit rounded-btn">
              <Truck className="w-4 h-4" strokeWidth={1.75} />
            </div>
          </div>
          <div className="text-2xl font-display font-bold text-ink mt-1">{trackingCount}</div>
          <div className="text-[11px] text-circuit font-medium mt-0.5 flex items-center justify-between">
            <span>Dalam linimasa workshop</span>
            <span className="text-[10px] text-ink-subtle">Klik filter</span>
          </div>
        </div>

        {/* Total */}
        <div 
          onClick={() => setFilterTab('all')}
          className={`bg-paper-card p-4 rounded-card border shadow-card transition-all cursor-pointer hover:border-stable ${
            filterTab === 'all' ? 'border-stable ring-2 ring-stable/20' : 'border-mist'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
              TOTAL SEMUA TIKET
            </span>
            <div className="p-2 bg-stable-subtle text-stable rounded-btn">
              <ShieldCheck className="w-4 h-4" strokeWidth={1.75} />
            </div>
          </div>
          <div className="text-2xl font-display font-bold text-ink mt-1">{requests.length}</div>
          <div className="text-[11px] text-stable font-medium mt-0.5 flex items-center justify-between">
            <span>Seluruh kanal layanan</span>
            <span className="text-[10px] text-ink-subtle">Reset filter</span>
          </div>
        </div>
      </div>

      {/* Filter Tabs & Recent Requests Table */}
      <div className="bg-paper-card rounded-card border border-mist shadow-card overflow-hidden">
        <div className="px-5 py-3.5 border-b border-mist bg-paper flex flex-col md:flex-row md:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Wrench className="w-4 h-4 text-circuit" strokeWidth={1.75} />
            <h2 className="text-xs font-display font-bold text-ink uppercase tracking-wide">
              Antrean Permintaan Masuk ({filteredRequests.length})
            </h2>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <div className="flex rounded-btn bg-paper-card border border-mist p-0.5 text-xs font-medium">
              <button
                onClick={() => setFilterTab('all')}
                className={`px-2.5 py-1 rounded-btn text-xs transition-all ${filterTab === 'all' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
              >
                Semua ({requests.length})
              </button>
              <button
                onClick={() => setFilterTab('remote')}
                className={`px-2.5 py-1 rounded-btn text-xs transition-all ${filterTab === 'remote' ? 'bg-signal text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
              >
                Remote ({remoteCount})
              </button>
              <button
                onClick={() => setFilterTab('on_site')}
                className={`px-2.5 py-1 rounded-btn text-xs transition-all ${filterTab === 'on_site' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
              >
                On-Site ({onsiteCount})
              </button>
              {unconfirmedCount > 0 && (
                <button
                  onClick={() => setFilterTab('unconfirmed')}
                  className={`px-2.5 py-1 rounded-btn text-xs transition-all ${filterTab === 'unconfirmed' ? 'bg-alert text-white font-semibold shadow-card' : 'text-alert hover:bg-alert/10'}`}
                >
                  Perlu Lokasi ({unconfirmedCount})
                </button>
              )}
            </div>

            <Link to="/requests" className="text-xs font-semibold text-signal hover:text-signal-hover flex items-center gap-1 pl-2">
              <span>Buka Semua</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        </div>

        {loading && requests.length === 0 ? (
          <TableSkeleton rows={4} />
        ) : filteredRequests.length === 0 ? (
          <div className="p-8 text-center text-ink-muted text-xs">
            Tidak ada tiket pada kategori filter ini.
          </div>
        ) : (
          <div className="divide-y divide-mist">
            {filteredRequests.slice(0, 8).map((req) => {
              const isRemote = req.type === 'remote';
              const sessionId = req.remote_session?.rustdesk_session_id;
              const attachmentsCount = Array.isArray(req.attachments) ? req.attachments.length : 0;

              return (
                <div 
                  key={req.id} 
                  onClick={() => setSelectedDetailReq(req)}
                  className="p-4 hover:bg-mist-light/50 transition-colors flex flex-col md:flex-row md:items-center md:justify-between gap-3 cursor-pointer group"
                >
                  <div className="space-y-1 max-w-xl">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-mono text-xs font-bold text-circuit bg-circuit/10 px-2 py-0.5 rounded-btn">
                        #{req.id}
                      </span>
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-semibold ${
                        isRemote ? 'bg-signal-subtle text-signal border border-signal/20' : 'bg-circuit/10 text-circuit border border-circuit/20'
                      }`}>
                        {isRemote ? 'Remote (RustDesk)' : 'Servis On-Site'}
                      </span>
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-medium ${
                        req.status === 'completed' ? 'bg-stable-subtle text-stable' : req.status === 'in_progress' ? 'bg-circuit/10 text-circuit' : req.status === 'scheduled' ? 'bg-signal-subtle text-signal' : 'bg-alert-subtle text-alert'
                      }`}>
                        {req.status}
                      </span>
                      {attachmentsCount > 0 && (
                        <span className="inline-flex items-center gap-1 text-[10px] font-mono text-ink-subtle bg-paper px-1.5 py-0.5 rounded-btn border border-mist">
                          <FileText className="w-3 h-3 text-circuit" />
                          <span>{attachmentsCount} Foto</span>
                        </span>
                      )}
                    </div>

                    <div className="text-xs font-bold text-ink flex items-center gap-2 group-hover:text-signal transition-colors">
                      <span>{req.device?.model || 'Perangkat Standar'}</span>
                      <span className="text-ink-subtle font-normal">•</span>
                      <span className="text-ink-muted font-normal">{req.user?.name}</span>
                      {req.device?.hardware_id && (
                        <div 
                          className="hidden sm:inline-flex items-center gap-1 font-mono text-[10px] text-ink-subtle bg-paper px-1.5 py-0.5 rounded-btn border border-mist"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <span>{req.device.hardware_id.slice(0, 14)}...</span>
                          <CopyButton text={req.device.hardware_id} label="Hardware ID" />
                        </div>
                      )}
                    </div>
                    <div className="text-xs text-ink-muted line-clamp-1">{req.description}</div>
                  </div>

                  {/* Actions according to type */}
                  <div className="flex items-center gap-2 flex-shrink-0" onClick={(e) => e.stopPropagation()}>
                    {/* View Detail button */}
                    <button
                      onClick={() => setSelectedDetailReq(req)}
                      className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-btn text-xs font-medium text-ink-muted hover:text-ink bg-paper hover:bg-mist-light border border-mist transition-colors"
                      title="Lihat rincian lengkap tiket"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>Rincian</span>
                    </button>

                    {/* Schedule button for remote if not scheduled */}
                    {isRemote && (
                      <button
                        onClick={() => setSelectedScheduleReq(req)}
                        className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-paper hover:bg-signal-subtle text-signal border border-signal/30 rounded-btn text-xs font-semibold transition-colors"
                        title="Atur jadwal sesi remote"
                      >
                        <Calendar className="w-3.5 h-3.5" />
                        <span>{req.scheduled_at ? 'Ubah Jadwal' : 'Jadwalkan'}</span>
                      </button>
                    )}

                    {/* Remote launcher */}
                    {isRemote && sessionId && (
                      <ConnectRemoteButton
                        sessionId={sessionId}
                        requestId={req.id}
                      />
                    )}

                    {/* On-Site Location confirmation */}
                    {!isRemote && (
                      <button
                        onClick={() => setSelectedLocationReq(req)}
                        className={`inline-flex items-center gap-1 px-2.5 py-1.5 rounded-btn text-xs font-medium border transition-all ${
                          req.is_location_confirmed
                            ? 'bg-stable-subtle border-stable/30 text-stable'
                            : 'bg-alert text-white hover:bg-alert/90 shadow-card'
                        }`}
                      >
                        <MapPin className="w-3.5 h-3.5" />
                        <span>{req.is_location_confirmed ? 'Lokasi OK' : 'Verifikasi Peta'}</span>
                      </button>
                    )}

                    {/* Service Center Tracking */}
                    {req.needs_office_repair && (
                      <button
                        onClick={() => setSelectedTrackingReq(req)}
                        className="inline-flex items-center gap-1 px-2.5 py-1.5 bg-circuit/10 hover:bg-circuit/20 text-circuit border border-circuit/20 rounded-btn text-xs font-medium transition-colors"
                      >
                        <Truck className="w-3.5 h-3.5" />
                        <span>Tracking ({req.tracking?.current_status?.replace(/_/g, ' ') || 'dijemput'})</span>
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
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
      />

      <LocationMapModal
        isOpen={!!selectedLocationReq}
        onClose={() => setSelectedLocationReq(null)}
        request={selectedLocationReq}
        onSuccess={fetchDashboardData}
      />

      <TrackingStepperModal
        isOpen={!!selectedTrackingReq}
        onClose={() => setSelectedTrackingReq(null)}
        requestId={selectedTrackingReq?.id || null}
        currentStatus={selectedTrackingReq?.tracking?.current_status || 'di_service_center'}
        onSuccess={fetchDashboardData}
      />

      <ScheduleRemoteModal
        isOpen={!!selectedScheduleReq}
        onClose={() => setSelectedScheduleReq(null)}
        request={selectedScheduleReq}
        onSuccess={fetchDashboardData}
      />
    </div>
  );
};

