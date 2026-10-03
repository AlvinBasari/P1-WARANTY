import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { useAuth } from '../context/AuthContext';
import api from '../services/api';
import { Cpu, Lock, Mail, ArrowRight, ShieldCheck, Wrench } from 'lucide-react';

export const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const res = await api.post('/auth/login', { email: email.trim(), password });
      login(res.data.token, res.data.user);
      toast.success(`Selamat datang, ${res.data.user?.name}!`);
      navigate('/');
    } catch (err: any) {
      if (!err.response) {
        setError('Tidak dapat terhubung ke server backend (Port 8000). Pastikan ./start-backend.sh sedang berjalan di terminal Anda.');
      } else {
        setError(err.response?.data?.message || err.response?.data?.errors?.email?.[0] || 'Login gagal. Periksa email dan password.');
      }
      toast.error('Gagal masuk ke akun.');
    } finally {
      setLoading(false);
    }
  };

  const handleQuickFill = (role: 'admin' | 'technician') => {
    if (role === 'admin') {
      setEmail('admin@warranty.com');
      setPassword('password');
    } else {
      setEmail('technician@warranty.com');
      setPassword('password');
    }
    setError(null);
  };

  return (
    <div className="min-h-screen bg-paper flex flex-col justify-center py-12 sm:px-6 lg:px-8 font-sans text-ink">
      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center space-y-2">
        <div className="inline-flex p-3 bg-circuit text-white rounded-card shadow-card">
          <Cpu className="w-6 h-6 text-signal-light" strokeWidth={1.75} />
        </div>
        <h2 className="text-2xl font-display font-bold tracking-tight text-ink">
          Control Panel Garansi
        </h2>
        <p className="text-xs text-ink-muted">
          Diagnostic, Remote Support &amp; Service Hub
        </p>
      </div>

      <div className="mt-6 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-paper-card py-8 px-6 shadow-card rounded-card border border-mist sm:px-8 space-y-6">
          {error && (
            <div className="p-3 bg-fault-subtle border border-fault-border text-fault text-xs rounded-btn">
              {error}
            </div>
          )}

          <form className="space-y-4" onSubmit={handleSubmit}>
            <div className="space-y-1">
              <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                EMAIL AKUN STAF
              </label>
              <div className="relative">
                <Mail className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="admin@warranty.com"
                  className="w-full text-xs pl-9 pr-3 py-2 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                />
              </div>
            </div>

            <div className="space-y-1">
              <label className="block text-[11px] font-mono font-bold uppercase tracking-wider text-ink-subtle">
                PASSWORD
              </label>
              <div className="relative">
                <Lock className="w-3.5 h-3.5 text-ink-subtle absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full text-xs pl-9 pr-3 py-2 bg-paper border border-mist rounded-btn focus:outline-none focus:border-signal text-ink placeholder:text-ink-subtle"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full flex items-center justify-center gap-1.5 py-2.5 px-4 bg-signal hover:bg-signal-hover text-white text-xs font-semibold rounded-btn shadow-card transition-colors disabled:opacity-50 mt-2"
            >
              <span>{loading ? 'Memverifikasi...' : 'Masuk ke Ruang Kontrol'}</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </form>

          {/* Quick Fill Demo Credentials */}
          <div className="pt-4 border-t border-mist space-y-2.5">
            <span className="block text-[10px] font-mono text-center text-ink-subtle uppercase tracking-wider">
              AKUN DEMO SEEDER (KLIK UNTUK MENGISI)
            </span>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => handleQuickFill('admin')}
                className="flex items-center justify-center gap-1.5 p-2 bg-paper hover:bg-mist-light border border-mist rounded-btn text-xs font-medium text-circuit transition-colors"
              >
                <ShieldCheck className="w-3.5 h-3.5 text-circuit" strokeWidth={1.5} />
                <span>Admin Utama</span>
              </button>

              <button
                type="button"
                onClick={() => handleQuickFill('technician')}
                className="flex items-center justify-center gap-1.5 p-2 bg-paper hover:bg-mist-light border border-mist rounded-btn text-xs font-medium text-circuit transition-colors"
              >
                <Wrench className="w-3.5 h-3.5 text-signal" strokeWidth={1.5} />
                <span>Teknisi Lapangan</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
