import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  LayoutDashboard, 
  Wrench, 
  ShieldCheck, 
  Truck, 
  MonitorPlay, 
  LogOut, 
  Cpu,
  Menu,
  X,
  UserCheck
} from 'lucide-react';
import { ConfirmModal } from './ConfirmModal';

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [showLogoutModal, setShowLogoutModal] = useState(false);

  const navItems = [
    { label: 'Dashboard', path: '/', icon: LayoutDashboard },
    { label: 'Tiket Perbaikan', path: '/requests', icon: Wrench },
    { label: 'Tracking Servis', path: '/tracking', icon: Truck },
    { label: 'Data Garansi & Device', path: '/warranties', icon: ShieldCheck },
    { label: 'Log Sesi Remote', path: '/sessions', icon: MonitorPlay },
  ];

  return (
    <header className="bg-circuit border-b border-circuit-dark sticky top-0 z-30 shadow-card text-white">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo & Main Navigation */}
          <div className="flex items-center space-x-6">
            <Link to="/" className="flex items-center space-x-3 group">
              <div className="w-8 h-8 rounded-btn bg-signal flex items-center justify-center text-white shadow-sm transition-transform group-hover:scale-105">
                <Cpu className="w-4 h-4" strokeWidth={2} />
              </div>
              <div className="flex flex-col">
                <span className="text-base font-display font-bold tracking-tight text-white flex items-center gap-1.5">
                  WARRANTY<span className="text-signal-light font-black">CONTROL</span>
                </span>
                <span className="text-[10px] font-mono tracking-wider text-mist-dark uppercase -mt-0.5">
                  Admin &amp; Service Hub
                </span>
              </div>
            </Link>

            {/* Desktop Navigation */}
            <nav className="hidden lg:flex items-center space-x-1">
              {navItems.map((item) => {
                const Icon = item.icon;
                const isActive = location.pathname === item.path;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    className={`inline-flex items-center px-3 py-1.5 rounded-btn text-xs font-medium transition-all ${
                      isActive
                        ? 'bg-circuit-dark text-white border border-mist/20 font-semibold shadow-inner'
                        : 'text-mist hover:text-white hover:bg-circuit-dark/50'
                    }`}
                  >
                    <Icon className={`w-3.5 h-3.5 mr-2 ${isActive ? 'text-signal-light' : 'text-mist-dark'}`} strokeWidth={1.75} />
                    {item.label}
                  </Link>
                );
              })}
            </nav>
          </div>

          {/* Right Section: User Info & Actions */}
          <div className="flex items-center space-x-3">
            {/* User Profile Pill */}
            <div className="hidden sm:flex items-center space-x-2.5 px-3 py-1.5 rounded-btn bg-circuit-dark/90 border border-mist/10">
              <div className="w-6 h-6 rounded-full bg-signal/20 text-signal-light flex items-center justify-center">
                <UserCheck className="w-3.5 h-3.5" />
              </div>
              <div className="text-left text-xs">
                <div className="font-semibold text-white leading-tight">{user?.name}</div>
                <div className="text-mist-dark uppercase tracking-widest font-mono text-[9px]">{user?.role}</div>
              </div>
            </div>

            {/* Logout Button */}
            <button
              onClick={() => setShowLogoutModal(true)}
              className="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-btn text-mist hover:text-fault hover:bg-fault/10 border border-transparent hover:border-fault/20 transition-all text-xs font-medium"
              title="Keluar dari sesi admin"
            >
              <LogOut className="w-4 h-4" strokeWidth={1.75} />
              <span className="hidden sm:inline">Keluar</span>
            </button>

            {/* Mobile Hamburger Toggle */}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="lg:hidden p-2 rounded-btn text-mist hover:text-white hover:bg-circuit-dark transition-colors"
              aria-label="Toggle navigation menu"
            >
              {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>
      </div>

      {/* Mobile Navigation Dropdown Drawer */}
      {mobileMenuOpen && (
        <div className="lg:hidden border-t border-circuit-dark bg-circuit-dark/95 backdrop-blur-sm px-4 py-3 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                onClick={() => setMobileMenuOpen(false)}
                className={`flex items-center px-3 py-2 rounded-btn text-xs font-medium transition-colors ${
                  isActive
                    ? 'bg-signal text-white font-semibold shadow-sm'
                    : 'text-mist hover:text-white hover:bg-circuit'
                }`}
              >
                <Icon className={`w-4 h-4 mr-3 ${isActive ? 'text-white' : 'text-mist-dark'}`} strokeWidth={1.75} />
                {item.label}
              </Link>
            );
          })}
          <div className="pt-2 mt-2 border-t border-mist/10 flex items-center justify-between text-xs text-mist-dark">
            <span>Masuk sebagai: <strong className="text-white">{user?.name}</strong> ({user?.role})</span>
          </div>
        </div>
      )}

      {/* Logout Confirmation Modal */}
      <ConfirmModal
        isOpen={showLogoutModal}
        onClose={() => setShowLogoutModal(false)}
        onConfirm={logout}
        variant="warning"
        title="Konfirmasi Keluar Sesi"
        description="Apakah Anda yakin ingin keluar dari portal manajemen garansi ini? Sesi autentikasi Anda akan diakhiri."
        confirmLabel="Ya, Keluar"
        cancelLabel="Batal"
        details={[
          { label: 'Pengguna', value: user?.name || '-' },
          { label: 'Peran', value: user?.role?.toUpperCase() || '-' }
        ]}
      />
    </header>
  );
};
