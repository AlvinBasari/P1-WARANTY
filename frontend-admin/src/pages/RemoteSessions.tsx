import React, { useEffect, useState } from 'react';
import api from '../services/api';
import { MonitorPlay, Clock, RefreshCw, User, Search, Play, CheckCircle2 } from 'lucide-react';
import { TableSkeleton } from '../components/SkeletonLoader';
import { CopyButton } from '../components/CopyButton';
import { ConnectRemoteButton } from '../components/ConnectRemoteButton';

export const RemoteSessions: React.FC = () => {
  const [sessions, setSessions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');

  const fetchSessions = async () => {
    setLoading(true);
    try {
      const res = await api.get('/remote-sessions');
      setSessions(res.data.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSessions();
  }, []);

  const calculateDuration = (startStr?: string, endStr?: string) => {
    if (!startStr || !endStr) return null;
    const start = new Date(startStr).getTime();
    const end = new Date(endStr).getTime();
    const diffMins = Math.round((end - start) / (1000 * 60));
    if (diffMins < 1) return '< 1 menit';
    return `${diffMins} menit`;
  };

  const filteredSessions = sessions.filter((sess) => {
    if (statusFilter !== 'all' && sess.connection_status !== statusFilter) return false;
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      const matchSessId = sess.rustdesk_session_id?.toLowerCase().includes(q);
      const matchTech = sess.technician?.name?.toLowerCase().includes(q);
      const matchDevice = sess.repair_request?.device?.model?.toLowerCase().includes(q);
      const matchNotes = sess.notes?.toLowerCase().includes(q);
      const matchReqId = sess.repair_request_id?.toString().includes(q);
      return matchSessId || matchTech || matchDevice || matchNotes || matchReqId;
    }
    return true;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-3 border-b border-mist">
        <div>
          <h1 className="text-xl font-display font-bold text-ink tracking-tight flex items-center gap-2">
            <span>Log Audit Sesi Remote Assistance</span>
            <span className="text-xs px-2.5 py-0.5 rounded-full font-sans font-medium bg-signal-subtle text-signal border border-signal/20">
              RustDesk Audit
            </span>
          </h1>
          <p className="text-xs text-ink-muted mt-0.5">
            Catatan terverifikasi aktivitas koneksi teknisi via RustDesk untuk audit reliabilitas, durasi pengerjaan, dan keamanan.
          </p>
        </div>
        <button
          onClick={fetchSessions}
          disabled={loading}
          className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-white border border-mist rounded-btn text-xs font-medium text-ink-muted hover:text-ink hover:bg-mist-light shadow-card transition-all self-start"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-signal' : ''}`} />
          <span>Segarkan Data</span>
        </button>
      </div>

      {/* Filter & Search */}
      <div className="bg-paper-card p-3 rounded-card border border-mist shadow-card flex flex-col md:flex-row gap-3 justify-between items-center">
        <div className="flex rounded-btn bg-paper border border-mist p-0.5 text-xs font-medium w-full md:w-auto">
          <button
            onClick={() => setStatusFilter('all')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'all' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Semua ({sessions.length})
          </button>
          <button
            onClick={() => setStatusFilter('connected')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'connected' ? 'bg-signal text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Connected
          </button>
          <button
            onClick={() => setStatusFilter('scheduled')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'scheduled' ? 'bg-circuit text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Dijadwalkan
          </button>
          <button
            onClick={() => setStatusFilter('completed')}
            className={`px-3 py-1 rounded-btn transition-all ${statusFilter === 'completed' ? 'bg-stable text-white font-semibold shadow-card' : 'text-ink-muted hover:text-ink'}`}
          >
            Selesai
          </button>
        </div>

        <div className="relative w-full md:w-80">
          <Search className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Cari Session ID, teknisi, unit..."
            className="w-full pl-8 pr-3 py-1.5 bg-paper border border-mist rounded-btn text-xs focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-paper-card rounded-card border border-mist shadow-card overflow-hidden">
        {loading && sessions.length === 0 ? (
          <TableSkeleton rows={5} />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-paper text-ink-subtle uppercase text-[10px] font-mono font-bold tracking-wider border-b border-mist">
                <tr>
                  <th className="px-5 py-3">Tiket &amp; Perangkat</th>
                  <th className="px-5 py-3">RustDesk Session ID</th>
                  <th className="px-5 py-3">Teknisi Bertugas</th>
                  <th className="px-5 py-3">Waktu Mulai / Selesai</th>
                  <th className="px-5 py-3">Status Sesi</th>
                  <th className="px-5 py-3">Catatan Diagnosa</th>
                  <th className="px-5 py-3 text-right">Aksi</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-mist">
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-5 py-12 text-center text-ink-muted text-xs">
                      Tidak ada log sesi remote yang sesuai dengan filter pencarian.
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((sess) => {
                    const duration = calculateDuration(sess.started_at, sess.ended_at);

                    return (
                      <tr key={sess.id} className="hover:bg-mist-light/40 transition-colors">
                        <td className="px-5 py-3.5">
                          <div className="flex items-center gap-1.5">
                            <span className="font-mono text-xs font-bold text-circuit bg-circuit/10 px-1.5 py-0.5 rounded-btn">
                              #{sess.repair_request_id}
                            </span>
                            <span className="font-semibold text-ink">
                              {sess.repair_request?.device?.model || 'Unit Standar'}
                            </span>
                          </div>
                        </td>

                        <td className="px-5 py-3.5">
                          <div className="inline-flex items-center gap-1 font-mono text-xs font-semibold text-circuit bg-paper px-2 py-1 rounded-btn border border-mist">
                            <span>{sess.rustdesk_session_id}</span>
                            <CopyButton text={sess.rustdesk_session_id} label="Session ID" />
                          </div>
                        </td>

                        <td className="px-5 py-3.5">
                          <div className="font-medium text-ink flex items-center gap-1">
                            <User className="w-3.5 h-3.5 text-ink-subtle" />
                            <span>{sess.technician?.name || 'Menunggu teknisi...'}</span>
                          </div>
                        </td>

                        <td className="px-5 py-3.5 text-[11px] font-mono text-ink-muted">
                          <div>Mulai: <span className="text-ink">{sess.started_at ? new Date(sess.started_at).toLocaleString('id-ID', { dateStyle: 'short', timeStyle: 'short' }) : '-'}</span></div>
                          <div>Selesai: <span className="text-ink">{sess.ended_at ? new Date(sess.ended_at).toLocaleString('id-ID', { dateStyle: 'short', timeStyle: 'short' }) : '-'}</span></div>
                          {duration && (
                            <div className="text-[10px] text-signal font-semibold mt-0.5">Durasi: {duration}</div>
                          )}
                        </td>

                        <td className="px-5 py-3.5">
                          <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-semibold ${
                            sess.connection_status === 'connected'
                              ? 'bg-signal-subtle text-signal border border-signal/30'
                              : sess.connection_status === 'completed'
                              ? 'bg-stable-subtle text-stable border border-stable/30'
                              : 'bg-alert-subtle text-alert border border-alert-border'
                          }`}>
                            {sess.connection_status}
                          </span>
                        </td>

                        <td className="px-5 py-3.5 text-[11px] text-ink-muted max-w-xs truncate">
                          {sess.notes || '-'}
                        </td>

                        <td className="px-5 py-3.5 text-right">
                          {sess.rustdesk_session_id && (
                            <ConnectRemoteButton
                              sessionId={sess.rustdesk_session_id}
                              requestId={sess.repair_request_id}
                            />
                          )}
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
    </div>
  );
};

