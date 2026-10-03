import React, { useEffect, useState } from 'react';
import api from '../services/api';
import { Truck, Search, History, ChevronRight, User, RefreshCw, CheckCircle2, PackageCheck, Wrench, ArrowRight, ShieldCheck, FileText } from 'lucide-react';
import { TrackingStepperModal } from '../components/TrackingStepperModal';
import { InvoiceModal } from '../components/InvoiceModal';
import { TableSkeleton } from '../components/SkeletonLoader';

const STAGES = [
  { id: 'dijemput', label: '1. Dijemput' },
  { id: 'di_service_center', label: '2. Di Workshop' },
  { id: 'sedang_diperbaiki', label: '3. Diperbaiki' },
  { id: 'selesai', label: '4. Selesai QC' },
  { id: 'dikembalikan', label: '5. Dikembalikan' },
];

export const TrackingManager: React.FC = () => {
  const [requests, setRequests] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedReq, setSelectedReq] = useState<any | null>(null);
  const [selectedInvoiceReq, setSelectedInvoiceReq] = useState<any | null>(null);
  const [search, setSearch] = useState('');

  const fetchTrackingUnits = async () => {
    setLoading(true);
    try {
      const res = await api.get('/repair-requests');
      const all = res.data.data || [];
      setRequests(all.filter((r: any) => r.needs_office_repair && r.tracking));
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTrackingUnits();
  }, []);

  const getStageIndex = (status: string) => {
    const idx = STAGES.findIndex((s) => s.id === status);
    return idx >= 0 ? idx : 0;
  };

  const filtered = requests.filter((r) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      r.device?.model?.toLowerCase().includes(q) ||
      r.user?.name?.toLowerCase().includes(q) ||
      r.id.toString().includes(q)
    );
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-3 border-b border-mist">
        <div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight flex items-center gap-2">
            <span>Manajemen Unit di Service Center (Workshop)</span>
            <span className="text-xs px-2.5 py-0.5 rounded-full font-sans font-medium bg-circuit/10 text-circuit border border-circuit/20">
              5 Tahapan Linimasa
            </span>
          </h1>
          <p className="text-xs text-ink-muted mt-0.5">
            Pantau dan perbarui 5 tahapan pengerjaan unit komputer/laptop yang dibawa teknisi ke workshop pusat.
          </p>
        </div>
        <button
          onClick={fetchTrackingUnits}
          disabled={loading}
          className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-white border border-mist rounded-btn text-xs font-medium text-ink-muted hover:text-ink hover:bg-mist-light shadow-card transition-all self-start"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-signal' : ''}`} />
          <span>Segarkan Data</span>
        </button>
      </div>

      {/* Filter & Count */}
      <div className="bg-paper-card p-3 rounded-card border border-mist shadow-card flex flex-col sm:flex-row gap-3 items-center justify-between">
        <div className="relative w-full max-w-md">
          <Search className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Cari model unit, nama pelanggan, atau nomor tiket..."
            className="w-full pl-8 pr-3 py-1.5 bg-paper border border-mist rounded-btn text-xs focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
          />
        </div>
        <div className="text-xs font-mono font-semibold text-circuit bg-circuit/10 border border-circuit/20 px-3 py-1.5 rounded-btn self-start sm:self-auto">
          {filtered.length} UNIT AKTIF DALAM TRACKING WORKSHOP
        </div>
      </div>

      {/* Grid of Units */}
      {loading && requests.length === 0 ? (
        <TableSkeleton rows={4} />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filtered.length === 0 ? (
            <div className="col-span-2 bg-paper-card p-12 rounded-card border border-mist text-center text-ink-muted text-xs">
              Belum ada unit yang ditandai untuk perbaikan di workshop service center.
            </div>
          ) : (
            filtered.map((req) => {
              const currentStatus = req.tracking?.current_status || 'dijemput';
              const histories = req.tracking?.histories || [];
              const currentStageIdx = getStageIndex(currentStatus);

              return (
                <div key={req.id} className="bg-paper-card rounded-card border border-mist p-4 shadow-card space-y-4 hover:border-signal/50 transition-colors flex flex-col justify-between">
                  <div className="space-y-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-mono text-xs font-bold text-circuit bg-circuit/10 px-2 py-0.5 rounded-btn">
                            Tiket #{req.id}
                          </span>
                          <span className="px-2.5 py-0.5 rounded-btn text-[10px] font-mono font-bold uppercase tracking-wider bg-circuit text-white">
                            {currentStatus.replace(/_/g, ' ')}
                          </span>
                        </div>
                        <h3 className="text-sm font-display font-bold text-ink mt-1.5">
                          {req.device?.model || 'Unit Servis'}
                        </h3>
                        <div className="text-[11px] text-ink-muted flex items-center gap-1.5 mt-0.5">
                          <User className="w-3 h-3 text-ink-subtle" />
                          <span>{req.user?.name} • {req.user?.phone || '-'}</span>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 flex-shrink-0">
                        <button
                          onClick={() => setSelectedInvoiceReq(req)}
                          className="px-2.5 py-1.5 bg-circuit text-white hover:bg-circuit-dark rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1"
                          title="Faktur & Garansi"
                        >
                          <FileText className="w-3.5 h-3.5" />
                          <span>Faktur</span>
                        </button>
                        <button
                          onClick={() => setSelectedReq(req)}
                          className="px-3 py-1.5 bg-signal hover:bg-signal-hover text-white rounded-btn text-xs font-semibold shadow-card transition-colors flex items-center gap-1"
                        >
                          <span>Update Progres</span>
                          <ChevronRight className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>

                    {/* 5-Step Visual Stepper Bar */}
                    <div className="bg-paper p-2.5 rounded-btn border border-mist">
                      <div className="flex items-center justify-between relative">
                        {STAGES.map((st, i) => {
                          const isDone = i <= currentStageIdx;
                          const isCurrent = i === currentStageIdx;

                          return (
                            <div key={st.id} className="flex-1 flex flex-col items-center text-center relative z-10">
                              <div className={`w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold transition-all ${
                                isCurrent
                                  ? 'bg-signal text-white ring-2 ring-signal/30'
                                  : isDone
                                  ? 'bg-stable text-white'
                                  : 'bg-mist-light text-ink-subtle border border-mist'
                              }`}>
                                {isDone ? '✓' : i + 1}
                              </div>
                              <span className={`text-[9px] mt-1 font-mono font-medium truncate max-w-[64px] ${
                                isCurrent ? 'text-signal-hover font-bold' : isDone ? 'text-ink font-semibold' : 'text-ink-subtle'
                              }`}>
                                {st.label.split('. ')[1]}
                              </span>
                            </div>
                          );
                        })}
                      </div>
                    </div>

                    {/* Tracking Timeline Log */}
                    <div className="bg-paper p-3 rounded-card border border-mist space-y-2">
                      <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-ink-subtle flex items-center gap-1">
                        <History className="w-3 h-3 text-circuit" />
                        <span>LINIMASA RIWAYAT PENGERJAAN:</span>
                      </div>

                      <div className="space-y-2 max-h-36 overflow-y-auto pr-1">
                        {histories.map((h: any, idx: number) => (
                          <div key={h.id || idx} className="text-xs flex items-start gap-2 border-l-2 border-circuit pl-2.5 py-0.5">
                            <div className="flex-1">
                              <div className="flex justify-between items-center">
                                <span className="font-mono font-semibold text-ink text-[11px] capitalize">
                                  {h.status.replace(/_/g, ' ')}
                                </span>
                                <span className="text-[10px] text-ink-subtle font-mono">
                                  {new Date(h.created_at).toLocaleString('id-ID', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                                </span>
                              </div>
                              <p className="text-ink-muted mt-0.5 text-[11px]">{h.notes}</p>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      )}

      <TrackingStepperModal
        isOpen={!!selectedReq}
        onClose={() => setSelectedReq(null)}
        requestId={selectedReq?.id || null}
        currentStatus={selectedReq?.tracking?.current_status || 'di_service_center'}
        onSuccess={fetchTrackingUnits}
      />

      <InvoiceModal
        isOpen={!!selectedInvoiceReq}
        onClose={() => setSelectedInvoiceReq(null)}
        repairRequest={selectedInvoiceReq}
        onSaved={fetchTrackingUnits}
      />
    </div>
  );
};
