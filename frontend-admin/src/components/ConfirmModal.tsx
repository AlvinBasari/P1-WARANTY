import React, { Fragment } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import { AlertTriangle, AlertCircle, CheckCircle2, Info, Loader2, X } from 'lucide-react';

export type ConfirmVariant = 'danger' | 'warning' | 'success' | 'info';

export interface ConfirmModalDetail {
  label: string;
  value: React.ReactNode;
}

export interface ConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
  title: string;
  description: React.ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: ConfirmVariant;
  loading?: boolean;
  details?: ConfirmModalDetail[];
}

export const ConfirmModal: React.FC<ConfirmModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Konfirmasi',
  cancelLabel = 'Batal',
  variant = 'warning',
  loading = false,
  details = [],
}) => {
  const getVariantStyles = () => {
    switch (variant) {
      case 'danger':
        return {
          icon: <AlertTriangle className="w-5 h-5 text-fault" strokeWidth={2} />,
          iconBg: 'bg-fault-subtle ring-4 ring-fault/10',
          btnBg: 'bg-fault hover:bg-fault/90 text-white shadow-card',
          badgeText: 'Perhatian Khusus / Aksi Kritis',
          badgeClass: 'bg-fault/10 text-fault border-fault/20',
        };
      case 'warning':
        return {
          icon: <AlertCircle className="w-5 h-5 text-alert" strokeWidth={2} />,
          iconBg: 'bg-alert-subtle ring-4 ring-alert/10',
          btnBg: 'bg-alert hover:bg-alert/90 text-white shadow-card',
          badgeText: 'Peringatan Operasional',
          badgeClass: 'bg-alert/10 text-alert border-alert/20',
        };
      case 'success':
        return {
          icon: <CheckCircle2 className="w-5 h-5 text-stable" strokeWidth={2} />,
          iconBg: 'bg-stable-subtle ring-4 ring-stable/10',
          btnBg: 'bg-stable hover:bg-stable/90 text-white shadow-card',
          badgeText: 'Konfirmasi Penyelesaian',
          badgeClass: 'bg-stable/10 text-stable border-stable/20',
        };
      case 'info':
      default:
        return {
          icon: <Info className="w-5 h-5 text-signal" strokeWidth={2} />,
          iconBg: 'bg-signal-subtle ring-4 ring-signal/10',
          btnBg: 'bg-circuit hover:bg-circuit-light text-white shadow-card',
          badgeText: 'Konfirmasi Aksi',
          badgeClass: 'bg-circuit/10 text-circuit border-circuit/20',
        };
    }
  };

  const style = getVariantStyles();

  return (
    <Transition appear show={isOpen} as={Fragment}>
      <Dialog as="div" className="relative z-50 font-sans text-ink" onClose={loading ? () => {} : onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-200"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-150"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-ink/60 backdrop-blur-sm" />
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
              <Dialog.Panel className="w-full max-w-md transform overflow-hidden rounded-card bg-paper-card text-left align-middle shadow-modal border border-mist transition-all p-5">
                {/* Header Icon + Title */}
                <div className="flex items-start gap-3.5">
                  <div className={`p-2.5 rounded-full shrink-0 ${style.iconBg}`}>
                    {style.icon}
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center justify-between gap-2">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-btn text-[10px] font-mono font-semibold uppercase tracking-wider border ${style.badgeClass}`}>
                        {style.badgeText}
                      </span>
                      {!loading && (
                        <button
                          onClick={onClose}
                          className="p-1 rounded-btn text-ink-muted hover:text-ink hover:bg-mist-light transition-colors"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      )}
                    </div>
                    <Dialog.Title as="h3" className="text-base font-display font-bold text-ink mt-1.5 leading-snug">
                      {title}
                    </Dialog.Title>
                    <div className="text-xs text-ink-muted mt-1 leading-relaxed">
                      {description}
                    </div>
                  </div>
                </div>

                {/* Details Card (if provided) */}
                {details.length > 0 && (
                  <div className="mt-4 p-3 bg-paper border border-mist rounded-btn space-y-1.5 text-xs">
                    {details.map((item, idx) => (
                      <div key={idx} className="flex justify-between items-center gap-2">
                        <span className="text-ink-subtle font-medium">{item.label}</span>
                        <span className="font-mono font-semibold text-ink text-right">{item.value}</span>
                      </div>
                    ))}
                  </div>
                )}

                {/* Action Buttons */}
                <div className="mt-5 flex items-center justify-end gap-2.5 pt-3 border-t border-mist">
                  <button
                    type="button"
                    disabled={loading}
                    onClick={onClose}
                    className="px-3.5 py-1.5 rounded-btn border border-mist bg-paper hover:bg-mist-light text-ink-muted hover:text-ink text-xs font-semibold transition-colors disabled:opacity-50"
                  >
                    {cancelLabel}
                  </button>
                  <button
                    type="button"
                    disabled={loading}
                    onClick={onConfirm}
                    className={`inline-flex items-center gap-1.5 px-4 py-1.5 rounded-btn text-xs font-semibold transition-colors disabled:opacity-50 ${style.btnBg}`}
                  >
                    {loading && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
                    <span>{confirmLabel}</span>
                  </button>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};
